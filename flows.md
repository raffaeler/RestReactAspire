# Command Flows — RestReactAspire Microservices Architecture

## Architecture Overview

```
┌──────────────┐     YARP Proxy      ┌─────────────────┐
│   Browser     │ ────► :5000 ──────► │ PatientService  │ :5101  queue: hospital.patient.write.commands
│               │                     │ DoctorService   │ :5102  queue: hospital.doctor.write.commands
└──────────────┘                     │ ExamService     │ :5103  queue: hospital.exam.write.commands
                                     │ StatisticsServ. │ :5104  queue: hospital.statistics.write.commands
                                     └─────────────────┘
                                           │
                              RabbitMQ Exchange: hospital.write.commands (Direct)
                              RabbitMQ Exchange: hospital.admin.reset    (Fanout)
```

---

## Core Types

```
WriteCommandEnvelope { CommandId:Guid, CommandType:string, Payload:JsonElement }
   └── Created via WriteCommandEnvelope.Create<T>(commandId, command)

WriteCommandResult { Succeeded:bool, ErrorCode, ErrorMessage, ResourceId, PatientsAffected... }

WriteCommandResultCoordinator
   ├── Prepare(commandId)   → creates TaskCompletionSource<WriteCommandResult> in ConcurrentDictionary
   ├── Complete(commandId, result) → resolves the TCS
   └── WaitAsync(commandId, ct) → awaits TCS (10s timeout), then removes from dict

IWriteCommandQueue
   └── EnqueueAsync(WriteCommandEnvelope, ct)
         ├── [InMemory] InMemoryWriteCommandQueue → handler.Handle() → coordinator.Complete() (SYNC)
         └── [RabbitMQ] RabbitMqWriteCommandQueue → BasicPublish to exchange (ASYNC)

RabbitMqWriteCommandProcessorBase : BackgroundService
   └── polls BasicGetAsync() → deserializes → handler.Handle() → coordinator.Complete()
```

---

## Seed Command Flow

### Phase 1: Gateway Fan-Out

```
Browser: POST http://localhost:5000/api/admin/seed
   │
   ▼
Server (Gateway) Program.cs line 143-172:
   │
   ├─ CreateHttpClient("patients") → POST http://patient-service:5101/api/admin/seed  ─┐
   ├─ CreateHttpClient("doctors")  → POST http://doctor-service:5102/api/admin/seed   ─┤ parallel (Task.WhenAll)
   │                                                                                    │
   ├─ await both above complete ◄───────────────────────────────────────────────────────┘
   │
   ├─ CreateHttpClient("exams")   → POST http://exam-service:5103/api/admin/seed       (sequential — needs patients + doctors)
   │
   └─ CreateHttpClient("stats")   → POST http://stats-service:5104/api/admin/seed      (sequential — needs all data)
```

### Phase 2: Inside Each Microservice (e.g., PatientService Seed)

```
PatientService receives POST /api/admin/seed
   │
   ▼
PatientEndpoints.Seed()  [line 214]
   │
   ├─ 1. commandId = Guid.NewGuid()
   ├─ 2. resultCoordinator.Prepare(commandId)           // create TCS in dictionary
   ├─ 3. writeQueue.EnqueueAsync(                       // dispatch command
   │       WriteCommandEnvelope.Create(commandId, new SeedDataCommand()))
   │
   │      ┌───[InMemory Mode]──────────────────────────────────────┐
   │      │  InMemoryWriteCommandQueue.EnqueueAsync()              │
   │      │    → handler.Handle(envelope)                          │
   │      │      → PatientWriteCommandHandler.HandleSeedData()     │
   │      │        → SeedDataGenerator.GeneratePatients()          │
   │      │        → _patientStore.DeleteAll()                     │
   │      │        → _patientStore.InsertBulk(patients)            │
   │      │        → return WriteCommandResult.Success(patients: N)│
   │      │    → coordinator.Complete(commandId, result)           │
   │      └────────────────────────────────────────────────────────┘
   │
   │      ┌───[RabbitMQ Mode]──────────────────────────────────────┐
   │      │  RabbitMqWriteCommandQueue.EnqueueAsync()              │
   │      │    → JsonSerializer.Serialize(envelope) → byte[] body  │
   │      │    → channel.BasicPublishAsync(                        │
   │      │        exchange: "hospital.write.commands",            │
   │      │        routingKey: "hospital.patient.write.commands",  │
   │      │        body: body, Persistent: true)                   │
   │      │                                                        │
   │      │  ╔═══════════ RABBITMQ TRANSPORT ═══════════════════╗  │
   │      │  ║  Exchange: hospital.write.commands (Direct)      ║  │
   │      │  ║  Routing Key: hospital.patient.write.commands    ║  │
   │      │  ║      ↓ matches bound queue                       ║  │
   │      │  ║  Queue: hospital.patient.write.commands          ║  │
   │      │  ╚══════════════════════════════════════════════════╝  │
   │      │                                                        │
   │      │  PatientRabbitMqWriteCommandProcessor.ExecuteAsync()  │
   │      │    → channel.BasicGetAsync("hospital.patient.write...")│
   │      │    → Utf8.GetString(body) → deserialize to envelope   │
   │      │    → handler.Handle(envelope)                          │
   │      │      → (same HandleSeedData as above)                  │
   │      │    → coordinator.Complete(commandId, result)           │
   │      └────────────────────────────────────────────────────────┘
   │
   ├─ 4. result = await resultCoordinator.WaitAsync(commandId, ct)  // blocks until Complete()
   ├─ 5. If !result.Succeeded → 503
   └─ 6. Return 200 { patientsCreated: N, links: [...] }
```

### Phase 3: Gateway Aggregates Results

```
Gateway receives all 4 responses:
   ├─ patient-service → { patientsCreated: 20 }
   ├─ doctor-service  → { doctorsCreated: 15 }
   ├─ exam-service    → { examsCreated: 50 }
   └─ stats-service   → { ... }
   ▼
Returns 200 { patientsCreated: 20, doctorsCreated: 15, examsCreated: 50, links: [...] }
```

**Key insight:** Seed propagates via **direct HTTP calls** (Gateway→service), but the command inside each service still goes through the CQRS pipeline (Enqueue/Dequeue/Process). The Gateway does NOT use RabbitMQ fanout for seed — it calls each service's HTTP endpoint sequentially (patients+doctors in parallel, then exams, then stats).

---

## Add Patient Command Flow

```
Browser: POST http://localhost:5000/api/patients
   Body: { "firstName": "John", "lastName": "Doe", ... }
   │
   ▼
YARP Reverse Proxy (Server Program.cs line 55-58):
   Route: /api/patients/{**catch-all} → patient-cluster → http://patient-service:5101
   │
   ▼
PatientService receives POST /api/patients
   │
   ▼
PatientEndpoints.Create()  [line 74]
   │
   ├─ 1. patientId = Guid.NewGuid()
   ├─ 2. commandId = Guid.NewGuid()
   ├─ 3. command = new CreatePatientCommand(patientId, firstName, lastName, ...)
   │
   ├─ 4. resultCoordinator.Prepare(commandId)
   │
   ├─ 5. await writeQueue.EnqueueAsync(
   │       WriteCommandEnvelope.Create(commandId, command), ct)
   │
   │       ╔═════ ENQUEUE (RabbitMQ path) ═════╗
   │       ║ RabbitMqWriteCommandQueue           ║
   │       ║   → Serialize envelope to JSON      ║
   │       ║   → BasicPublishAsync:              ║
   │       ║       Exchange: hospital.write...   ║
   │       ║       RoutingKey: hospital.patient...║
   │       ║       Persistent: true              ║
   │       ╚══════════════════════════════════════╝
   │
   ├─ 6. result = await resultCoordinator.WaitAsync(commandId, ct)  ← BLOCKS HERE
   │       │                                                        (up to 10s timeout)
   │       │       ╔═════ DEQUEUE + PROCESS (async, in background) ═╗
   │       │       ║ PatientRabbitMqWriteCommandProcessor            ║
   │       │       ║   BasicGetAsync("hospital.patient.write...")    ║
   │       │       ║   → deserialize to WriteCommandEnvelope         ║
   │       │       ║   → handler.Handle(envelope)                    ║
   │       │       ║       CommandType == "CreatePatientCommand"     ║
   │       │       ║       → HandleCreatePatient(command)            ║
   │       │       ║         → _patientStore.Add(new Patient{...})   ║
   │       │       ║         → return Success(resourceId: patientId) ║
   │       │       ║   → coordinator.Complete(commandId, result) ◄──┘ unblocks WaitAsync
   │       │       ╚════════════════════════════════════════════════╝
   │
   ├─ 7. if !result.Succeeded → 503
   ├─ 8. patient = store.GetById(patientId)  ← reads back from LiteDB
   │      if null → 503 ("did not complete in time")
   │
   └─ 9. Return 201 Created { Location: /api/patients/{id}, body: PatientResponse + HATEOAS links }
```

---

## Reset Command Flow (Fanout)

```
Browser: POST http://localhost:5000/api/admin/reset
   │
   ▼
Server (Gateway) Program.cs line 175-259:
   │
   ├─ Snapshot pre-reset counts via HTTP GET /api/admin/stats on patients, doctors, exams
   │
   ├─ Publish ResetDataCommand to fanout exchange:
   │     envelope = WriteCommandEnvelope.Create(Guid.NewGuid(), new ResetDataCommand())
   │     channel.BasicPublishAsync(
   │         exchange: "hospital.admin.reset",   ← FANOUT
   │         routingKey: ""                       ← empty → all bound queues
   │     )
   │
   │       ╔════════ FANOUT DELIVERY ══════════════════════════════╗
   │       ║ Exchange: hospital.admin.reset (Fanout, durable)       ║
   │       ║   ├── Queue: hospital.patient.write.commands   ──► PS  ║
   │       ║   ├── Queue: hospital.doctor.write.commands    ──► DS  ║
   │       ║   ├── Queue: hospital.exam.write.commands      ──► ES  ║
   │       ║   └── Queue: hospital.statistics.write.commands──► SS  ║
   │       ╚════════════════════════════════════════════════════════╝
   │
   │  Each service's BackgroundService picks up the message:
   │    → handler.Handle(envelope)
   │      → HandleResetData()
   │        → _store.DeleteAll()
   │        → return WriteCommandResult.Success(patients: N)
   │    → coordinator.Complete(commandId, result)
   │       (Note: no WaitAsync listener — Gateway doesn't wait for TCS)
   │
   ├─ Poll /api/admin/stats every 500ms (up to 6 attempts) until all services report 0
   │
   └─ Return 200 { PatientsDeleted: N, DoctorsDeleted: N, ExamsDeleted: N, links: [...] }
```

**Key difference from Seed:** Reset uses **RabbitMQ fanout** (single publish → all services receive simultaneously), while Seed uses **sequential HTTP calls**. The Gateway does not use the coordinator pattern for reset — it publishes and polls stats to verify completion.

---

## RabbitMQ Topology Summary

```
Exchange: hospital.write.commands (direct, durable)
   │
   ├── Queue: hospital.patient.write.commands    ← PatientRabbitMqWriteCommandProcessor
   ├── Queue: hospital.doctor.write.commands     ← DoctorRabbitMqWriteCommandProcessor
   ├── Queue: hospital.exam.write.commands       ← ExamRabbitMqWriteCommandProcessor
   └── Queue: hospital.statistics.write.commands ← StatisticsRabbitMqWriteCommandProcessor

Exchange: hospital.admin.reset (fanout, durable)
   │
   ├── Queue: hospital.patient.write.commands    ← also bound here (reset broadcasts)
   ├── Queue: hospital.doctor.write.commands     ←   "     "     "
   ├── Queue: hospital.exam.write.commands       ←   "     "     "
   └── Queue: hospital.statistics.write.commands ←   "     "     "
```

Each service binds to **both** exchanges on the same queue. The direct exchange carries normal CRUD commands; the fanout exchange carries `ResetDataCommand` broadcasts from the Gateway.

### Per-Service Queue Configuration

| Service | Queue Name | Port |
|---------|-----------|------|
| PatientService | `hospital.patient.write.commands` | 5101 |
| DoctorService | `hospital.doctor.write.commands` | 5102 |
| ExamService | `hospital.exam.write.commands` | 5103 |
| StatisticsService | `hospital.statistics.write.commands` | 5104 |

---

## Coordinator Pattern (Request-Reply over Async Messaging)

```
HTTP Request Thread                    Background Service Thread
─────────────────────                  ──────────────────────────
1. Prepare(commandId)
   → TCS stored in ConcurrentDictionary
2. EnqueueAsync(envelope)
   → BasicPublish ─────────────────────► 3. BasicGetAsync
                                         4. handler.Handle(envelope)
                                         5. coordinator.Complete(id, result)
   ◄────────────────────────────────────   TCS.TrySetResult(result)
6. WaitAsync(commandId)
   → awaits TCS.Task (10s timeout)
   ← result returned
7. Return HTTP response (201/200/503)
```

### Important: InMemory vs RabbitMQ

The `Cqrs:UseInMemoryQueue` setting (default `true` in Testing environment) toggles the queue implementation:

- **InMemory mode:** `InMemoryWriteCommandQueue.EnqueueAsync()` calls `handler.Handle()` **synchronously** and completes the coordinator immediately — the whole flow runs on the HTTP request thread.
- **RabbitMQ mode:** `RabbitMqWriteCommandQueue.EnqueueAsync()` publishes to RabbitMQ and returns immediately. A separate `BackgroundService` thread picks up the message via `BasicGetAsync`, processes it, and completes the coordinator — unblocking the waiting HTTP thread.

---

## YARP Reverse Proxy Routes

```
Browser → Server (:5000)
   │
   ├── /api/patients/**     → patient-cluster    → http://patient-service:5101
   ├── /api/doctors/**      → doctor-cluster     → http://doctor-service:5102
   ├── /api/exams/**        → exam-cluster       → http://exam-service:5103
   └── /api/statistics/**   → statistics-cluster → http://statistics-service:5104

Direct (not proxied):
   ├── /api                 → API root discovery (HATEOAS entry point)
   ├── /api/admin/seed      → Gateway fan-out handler
   ├── /api/admin/reset     → RabbitMQ fanout publish
   └── /api/admin/stats     → Gateway aggregates stats
```

---

## AppHost Orchestration

```
LavinMQ Container (cloudamqp/lavinmq)
   ├── AMQP on :5672
   └── Management UI on :15672
         │
         ├── patient-service    (:5101) waits for LavinMQ
         ├── doctor-service     (:5102) waits for LavinMQ
         ├── exam-service       (:5103) waits for LavinMQ
         ├── statistics-service (:5104) waits for LavinMQ
         │
         └── server (:5000) waits for LavinMQ + all 4 services
               │
               └── webfrontend (Vite) waits for server
```
