# Architecture & Design Patterns — RestReactAspire

> A HATEOAS-compliant REST day-hospital management system built with
> .NET 10, ASP.NET Core, .NET Aspire, React 19, TypeScript, LiteDB, and YARP.
> **Microservices architecture** — each domain entity lives in its own service.

---

## 1. Solution Overview

| Project | Role |
|---------|------|
| `RestReactAspire.AppHost` | .NET Aspire orchestrator — wires all services, frontend, and shared telemetry |
| `RestReactAspire.Server` | **YARP reverse proxy gateway** — routes requests to microservices; serves frontend static files; no database, no stores |
| `RestReactAspire.Shared` | Shared library — domain models, DTOs, CQRS abstractions, telemetry primitives, base store classes, LiteDB factory |
| `RestReactAspire.PatientService` | Patient microservice — own LiteDB, CQRS pipeline, telemetry |
| `RestReactAspire.DoctorService` | Doctor microservice — own LiteDB, CQRS pipeline, telemetry |
| `RestReactAspire.ExamService` | Exam microservice — own LiteDB, CQRS pipeline, telemetry |
| `RestReactAspire.StatisticsService` | Statistics microservice — own LiteDB, read-optimised aggregations, telemetry |
| `RestReactAspire.Server.Tests` | xUnit integration and unit tests |
| `frontend/` | React 19 SPA (TypeScript, MUI v7, React Router v7, recharts v3, Vite 7) |

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                       .NET Aspire AppHost                        │
│                                                                  │
│  ┌──────────┐   ┌──────────────────────────────────────────┐    │
│  │          │   │          YARP Gateway (Server)            │    │
│  │ frontend │──▶│  GET /api → Root discovery                │    │
│  │  (Vite)  │   │  /api/patients/* → PatientService         │    │
│  │          │   │  /api/doctors/*  → DoctorService          │    │
│  └──────────┘   │  /api/exams/*    → ExamService            │    │
│                 │  /api/statistics/* → StatisticsService    │    │
│                 │  /api/admin/*    → Fan-out to all         │    │
│                 └──────┬──────┬──────┬──────┬───────────────┘    │
│                        │      │      │      │                    │
│  ┌─────────────────────┤      │      │      │                    │
│  │  PatientService     │      │      │      │                    │
│  │  ┌───────────────┐  │      │      │      │                    │
│  │  │ LiteDB (own)  │  │      │      │      │                    │
│  │  │ CQRS pipeline │  │      │      │      │                    │
│  │  │ Telemetry     │  │      │      │      │                    │
│  │  └───────────────┘  │      │      │      │                    │
│  └─────────────────────┘      │      │      │                    │
│              ┌────────────────┘      │      │                    │
│              │ DoctorService         │      │                    │
│              │ ┌───────────────┐     │      │                    │
│              │ │ LiteDB (own)  │     │      │                    │
│              │ │ CQRS pipeline │     │      │                    │
│              │ │ Telemetry     │     │      │                    │
│              │ └───────────────┘     │      │                    │
│              └───────────────────────┘      │                    │
│                       ┌─────────────────────┘                    │
│                       │ ExamService                              │
│                       │ ┌───────────────┐                        │
│                       │ │ LiteDB (own)  │                        │
│                       │ │ CQRS pipeline │                        │
│                       │ │ Telemetry     │                        │
│                       │ └───────────────┘                        │
│                       └──────────────────────────────────────────┘
│                                    │                             │
│                        StatisticsService                         │
│                        ┌───────────────┐                         │
│                        │ LiteDB (own)  │                         │
│                        │ Telemetry     │                         │
│                        └───────────────┘                         │
│                                                                  │
│              RestReactAspire.Shared (all services)                │
│              ┌──────────────────────────────────┐                │
│              │ Models, DTOs, CQRS abstractions, │                │
│              │ BaseStore, LiteDbFactory,        │                │
│              │ Telemetry primitives             │                │
│              └──────────────────────────────────┘                │
└──────────────────────────────────────────────────────────────────┘
```

---

## 2. Methodologies

### 2.1 Domain-Driven Design (DDD)

DDD is a software design **methodology** focused on modelling the core business domain. This project **does not** adopt DDD. The domain models (`Patient`, `Doctor`, `Exam`) are **anemic data holders** without encapsulated behaviour, invariants, aggregates, value objects, or domain events. See §5.1 for a discussion of how DDD could be introduced.

---

## 3. Architectural Styles

### 3.1 Representational State Transfer (REST)

The API follows REST architectural constraints: stateless client-server communication, uniform resource identification via URIs, and standard HTTP verbs (`GET`, `POST`, `PUT`, `DELETE`).

| Where | File(s) | Details |
|-------|---------|---------|
| Gateway routing | `Server\Program.cs` | YARP routes: `/api/patients` → PatientService, `/api/doctors` → DoctorService, `/api/exams` → ExamService, `/api/statistics` → StatisticsService, `/api/admin` → fan-out |
| Patient resource | `PatientService\Endpoints\PatientEndpoints.cs` | `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}` |
| Exam resource | `ExamService\Endpoints\ExamEndpoints.cs` | CRUD + `PUT /{id}/doctor`; sub-resource `GET /patients/{patientId}/exams` |
| Doctor resource | `DoctorService\Endpoints\DoctorEndpoints.cs` | CRUD + sub-resource `GET /doctors/{doctorId}/exams` |
| Admin operations | Gateway fan-out endpoint | `POST /seed`, `POST /reset`, `GET /stats` — fans out to all services |
| Statistics (read-only) | `StatisticsService\Endpoints\StatisticsEndpoints.cs` | Four aggregation endpoints |
| API entry point | Gateway root endpoint | `GET /api` |

### 3.2 HATEOAS (Hypermedia as the Engine of Application State)

A REST maturity constraint (Richardson Maturity Level 3). Every API response embeds discoverable `Link` objects (`rel`, `href`, `method`) so clients navigate exclusively via hypermedia, never hard-coding URLs beyond the single entry point `GET /api`.

| Where | File(s) | Details |
|-------|---------|---------|
| Link model | `Shared\Models\Link.cs` — `Link`, `PaginationInfo`, `SortInfo`, `PaginationLinks` | Shared HATEOAS primitives; `PaginationLinks.Build()` generates `self/first/last/prev/next` |
| Root discovery | Gateway root endpoint — `MapRootEndpoints` | `GET /api` returns `ApiRootResponse` with all top-level link relations (pointing to gateway URLs) |
| Per-resource links | `PatientService\Endpoints\PatientEndpoints.cs` — `ToPatientResponse` | `self`, `update`, `delete`, `exams`, `collection`; HREFs point to gateway |
| | `DoctorService\Endpoints\DoctorEndpoints.cs` — `ToDoctorResponse` | Same pattern |
| | `ExamService\Endpoints\ExamEndpoints.cs` — `ToExamResponse` | Adds `assign-doctor`, `patient`, `patient-exams`, conditional `doctor`/`doctor-exams` |
| | Gateway fan-out — Admin endpoints | Seed/Reset/Stats responses carry cross-resource navigation links |
| | `StatisticsService\Endpoints\StatisticsEndpoints.cs` — `GetStatisticsLinks` | Links to sibling charts and entity collections (via gateway) |
| Frontend consumer | `frontend\src\api\apiClient.ts` — `discoverApi()`, `getLink()`, `findLink()` | Client discovers the root once and navigates via link relations |
| Frontend types | `frontend\src\types\hateoas.ts` | TypeScript contracts mirroring shared `Link` model |

### 3.3 Layered Architecture

Each microservice follows a **CQRS-oriented layered design** where reads and writes are separated. The Shared library provides base classes and abstractions; each service adds its own entity-specific implementations.

| Layer | Files | Responsibility |
|-------|-------|----------------|
| **Presentation (Endpoints)** | `{Service}\Endpoints\*.cs` | HTTP mapping, response shaping, telemetry, HATEOAS link generation |
| **Command Layer (Write Side)** | `{Service}\Cqrs\*.cs` | Build write commands, enqueue to LavinMQ (RabbitMQ protocol), process queued commands, coordinate command results |
| **Query/Data Access Layer** | `Shared\Stores\BaseStore.cs` + `{Service}\Stores\*.cs` | Generic CRUD/pagination/search in Shared; entity-specific queries in service stores |
| **Models** | `Shared\Models\*.cs` | Domain entities and DTOs (referenced by all services) |

### 3.4 Client-Server Architecture

The system is divided into a YARP gateway + microservice backend and a single-page application frontend, communicating exclusively via HTTP/JSON. In development, Vite proxies to the gateway; the gateway routes to internal microservices via Aspire service discovery.

| Component | File | Details |
|-----------|------|---------|
| Gateway | `Server\Program.cs` | YARP reverse proxy routing to all microservices |
| Backend services | `PatientService\Program.cs`, `DoctorService\Program.cs`, `ExamService\Program.cs`, `StatisticsService\Program.cs` | ASP.NET Core Minimal APIs |
| Frontend | `frontend\src\App.tsx` | React 19 SPA with `BrowserRouter` |
| Dev proxy | `frontend\vite.config.ts` | Vite proxies `/api` to the gateway via Aspire-injected env vars |
| Production serving | `Server\Program.cs` — `app.UseFileServer()` | SPA served as static files from `wwwroot` |

### 3.5 Service-Oriented Architecture (Aspire Orchestration)

.NET Aspire orchestrates all five services (gateway + 4 microservices) and the frontend as independently configured services with shared telemetry, health checks, and service discovery.

| Where | File | Details |
|-------|------|---------|
| AppHost | `AppHost\AppHost.cs` | `AddProject` for each microservice + gateway; `AddViteApp` (frontend); health checks; service references; container publishing |
| Service Defaults | `Shared\Extensions.cs` — `AddServiceDefaults` | Adds service discovery, HTTP resilience, OpenTelemetry, health checks — used by all services |

### 3.6 CQRS with Asynchronous Messaging

**Each microservice has its own independent CQRS pipeline.** Writes are handled as commands and queued through LavinMQ using the RabbitMQ protocol. A background processor consumes commands and applies state changes to the service's own LiteDB through its stores. Reads remain direct query operations from endpoint handlers. The CQRS abstractions (interfaces, envelope types, coordinator) live in `RestReactAspire.Shared/CqrsAbstractions/`.

| Where | File(s) | Details |
|-------|---------|---------|
| CQRS abstractions | `Shared\CqrsAbstractions\*.cs` | Shared interfaces: `IWriteCommandQueue`, `IWriteCommandHandler`, `WriteCommandEnvelope` |
| Command contracts | `{Service}\Cqrs\WriteCommands.cs` | Service-specific write command records |
| Queue abstraction | `Shared\CqrsAbstractions\IWriteCommandQueue.cs` | Endpoint write handlers depend on the shared abstraction |
| RabbitMQ producer | `{Service}\Cqrs\RabbitMqWriteCommandQueue.cs` | Enqueues persistent messages to LavinMQ queue |
| RabbitMQ consumer | `{Service}\Cqrs\RabbitMqWriteCommandProcessor.cs` | Background worker dequeues and executes commands |
| Command execution | `{Service}\Cqrs\WriteCommandHandler.cs` | Applies write operations via service stores |
| Request/response sync | `Shared\CqrsAbstractions\WriteCommandResultCoordinator.cs` | Correlates HTTP request with command completion (shared) |
| Runtime registration | `{Service}\Program.cs` | Registers CQRS services; uses in-memory queue in `Testing` environment |
| Aspire dependency | `AppHost\AppHost.cs` | Each service waits for `lavinmq` container before startup |

### 3.7 Microservices Architecture

The solution is decomposed into independent microservices, each responsible for a single business capability. Each service owns its data, its CQRS pipeline, and its telemetry.

| Service | Database | Entities | Dependencies |
|---------|----------|----------|-------------|
| `PatientService` | `patients.db` | Patient CRUD | LavinMQ (write commands) |
| `DoctorService` | `doctors.db` | Doctor CRUD | LavinMQ (write commands) |
| `ExamService` | `exams.db` | Exam CRUD, doctor assignment | LavinMQ (write commands), cross-service calls for patient/doctor lookup |
| `StatisticsService` | `statistics.db` | Read-only aggregations | Periodic data sync or direct queries to other services' DBs |

**Key characteristics:**
- **Independent deployability**: Each service can be built, tested, and deployed separately.
- **Data isolation**: No shared database — each service has its own LiteDB file.
- **Shared library**: `RestReactAspire.Shared` avoids code duplication for models, DTOs, CQRS abstractions, and base store logic.
- **Gateway routing**: The YARP gateway provides a unified API surface; clients never know about internal service topology.

---

## 4. Design Patterns

### 4.1 Data Transfer Object (DTO)

Separate immutable record types for creation requests, update requests, and responses. Decouples the API contract from internal domain entities. All DTOs live in the Shared library.

| DTO set | File |
|---------|------|
| Patient DTOs | `Shared\Models\PatientDto.cs` — `CreatePatientRequest`, `UpdatePatientRequest`, `PatientResponse`, `PatientListResponse`, `ApiRootResponse` |
| Doctor DTOs | `Shared\Models\DoctorDto.cs` — `CreateDoctorRequest`, `UpdateDoctorRequest`, `DoctorResponse`, `DoctorListResponse`, `AssignDoctorRequest` |
| Exam DTOs | `Shared\Models\ExamDto.cs` — `CreateExamRequest`, `UpdateExamRequest`, `ExamResponse`, `ExamListResponse` |
| Admin DTOs | `Shared\Models\AdminDto.cs` — `SeedResponse`, `ResetResponse`, `StatsResponse` |
| Statistics DTOs | `Shared\Models\StatisticsDto.cs` — `PatientsByAgeGroupResponse`, `ExamsPerDoctorResponse`, `ExamsOverTimeResponse`, `AvgDurationByExamTypeResponse` |
| HATEOAS primitives | `Shared\Models\Link.cs` — `Link`, `PaginationInfo`, `SortInfo` |

### 4.2 Repository Pattern

Each entity has a dedicated **Store** class that encapsulates all data access logic against its service's LiteDB collection. Store base class with generic CRUD, pagination, search, and sorting lives in Shared. Each microservice extends it for entity-specific needs.

| Store | File | Key Methods |
|-------|------|-------------|
| `PatientStore` | `PatientService\Stores\PatientStore.cs` | `GetAll`, `GetPaged`, `SearchPaged`, `GetById`, `Add`, `Update`, `Delete`, `InsertBulk`, `DeleteAll` |
| `DoctorStore` | `DoctorService\Stores\DoctorStore.cs` | Same CRUD + search/sort + bulk/reset helpers |
| `ExamStore` | `ExamService\Stores\ExamStore.cs` | Adds `GetByPatientId*`, `GetByDoctorId*`, `AssignDoctor`, bulk/reset helpers |

### 4.3 Dependency Injection (IoC Container)

All runtime dependencies are resolved through the built-in ASP.NET Core DI container using constructor injection and parameter injection. Each microservice has its own DI container with its own singleton registrations.

| Registration | File | Details |
|-------------|------|---------|
| `ILiteDatabase` singleton | `{Service}\Program.cs` | Each service creates its own LiteDB instance (e.g., `Filename=patients.db;Connection=shared`) |
| Store singletons | `{Service}\Program.cs` | Each service registers its own stores as singletons |
| CQRS services | `{Service}\Program.cs`, `{Service}\Cqrs\*.cs` | `WriteCommandHandler`, `WriteCommandResultCoordinator`, queue implementation, RabbitMQ connection manager, background processor |
| Endpoint parameter injection | `{Service}\Endpoints\*.cs` | Handler parameters resolved from DI (e.g., `PatientStore store`, `ILogger<PatientStore> logger`) |

### 4.4 Singleton Pattern

Each microservice's embedded database and its stores use the Singleton lifecycle to ensure a single shared instance within that service.

| Where | File | Details |
|-------|------|---------|
| `ILiteDatabase` | `{Service}\Program.cs` | `Connection=shared` for concurrent access |
| Stores | `{Service}\Program.cs` | Registered as singletons; hold references to the service's singleton DB |
| CQRS coordinator | `{Service}\Program.cs`, `Shared\CqrsAbstractions\WriteCommandResultCoordinator.cs` | Singleton command result correlation across request/worker boundary |
| `LiteDbFactory._configured` | `Shared\Stores\LiteDbFactory.cs` | Thread-safe one-time initialization with `lock` + boolean guard |

### 4.5 Factory Pattern

A static factory in the Shared library encapsulates LiteDB mapper configuration, including custom type serializers and entity pre-warming. Called by every microservice at startup.

| Where | File | Details |
|-------|------|---------|
| `LiteDbFactory.ConfigureMapper` | `Shared\Stores\LiteDbFactory.cs` | Registers `DateOnly`/`TimeOnly` serializers, pre-warms entity mapper cache |

### 4.6 Builder Pattern

Used pervasively through host configuration APIs and in HATEOAS link generation.

| Where | File | Details |
|-------|------|---------|
| Application builder | `{Service}\Program.cs` | `WebApplication.CreateBuilder` → `AddServiceDefaults` → `Build` → `Run` |
| Gateway builder | `Server\Program.cs` | Builds YARP reverse proxy configuration |
| Aspire orchestration | `AppHost\AppHost.cs` | `DistributedApplication.CreateBuilder` → `AddProject` (×5) → `AddViteApp` → `Build` → `Run` |
| Pagination link builder | `Shared\Models\Link.cs` — `PaginationLinks.Build()` | Fluent construction of `self/first/last/prev/next` links with query parameters |
| OpenTelemetry pipeline | `Shared\Extensions.cs` — `ConfigureOpenTelemetry` | `.WithMetrics(m => ...)` `.WithTracing(t => ...)` chain |

### 4.7 Observer Pattern

The telemetry layer implements the Observer pattern through `ActivitySource` (distributed traces) and `Meter`/`Counter` (metrics). Observers (OTLP exporters) subscribe to these sources without coupling to the endpoint logic. Each microservice has its own telemetry classes.

| Telemetry class | File | Instruments |
|----------------|------|-------------|
| `PatientTelemetry` | `PatientService\Telemetry\PatientTelemetry.cs` | `ActivitySource`, counters: `PatientsQueried`, `PatientsCreated`, `PatientsUpdated`, `PatientsDeleted` |
| `ExamTelemetry` | `ExamService\Telemetry\ExamTelemetry.cs` | Same pattern for exams |
| `DoctorTelemetry` | `DoctorService\Telemetry\DoctorTelemetry.cs` | Same pattern for doctors |
| `AdminTelemetry` | Gateway telemetry | `StatsQueried`, `DatabaseSeeded`, `DatabaseReset` |
| `RootTelemetry` | Gateway telemetry | `RootRequested` |
| `StatisticsTelemetry` | `StatisticsService\Telemetry\StatisticsTelemetry.cs` | Four chart-specific query counters |
| Observer registration | `Shared\Extensions.cs` — `ConfigureOpenTelemetry` | Registers all sources and meters; OTLP exporter subscribes as observer |

### 4.8 Strategy Pattern (Sorting)

Each store uses a strategy-like dispatch to select the sorting algorithm at runtime based on the `sortBy` parameter.

| Where | File | Details |
|-------|------|---------|
| `PatientStore.ApplySort` | `PatientService\Stores\PatientStore.cs` | `switch` expression selects `OrderBy`/`OrderByDescending` by column name |
| `DoctorStore.ApplySort` | `DoctorService\Stores\DoctorStore.cs` | Same dispatch pattern |
| `ExamStore.ApplySort` | `ExamService\Stores\ExamStore.cs` | Same dispatch pattern |

### 4.9 Adapter Pattern

Custom LiteDB type serializers adapt .NET types (`DateOnly`, `TimeOnly`) to BSON-compatible representations, bridging the incompatibility between the .NET type system and LiteDB's storage format.

| Where | File | Details |
|-------|------|---------|
| `DateOnly` adapter | `Shared\Stores\LiteDbFactory.cs` | `BsonMapper.Global.RegisterType` — ISO 8601 round-trip format |
| `TimeOnly` adapter | Same file | Same approach |

### 4.10 Proxy Pattern

In development, the Vite dev server acts as a reverse proxy, forwarding `/api` requests to the YARP gateway. The gateway then routes to the appropriate microservice. In production, the gateway serves the SPA directly.

| Where | File | Details |
|-------|------|---------|
| Vite proxy | `frontend\vite.config.ts` | Forwards `/api` to gateway via Aspire-injected `SERVER_HTTPS`/`SERVER_HTTP` |
| YARP gateway | `Server\Program.cs` | Reverse proxy routing to microservices via Aspire service discovery |
| Service reference | `AppHost\AppHost.cs` — `.WithReference(server)` | Aspire injects gateway URLs into the frontend process |

### 4.11 API Gateway Pattern (New)

The **Server** is now a YARP reverse proxy gateway implementing the **API Gateway** pattern. It provides a unified entry point for all clients, routing requests to the appropriate microservice based on URL path prefixes.

| Where | File | Details |
|-------|------|---------|
| Gateway config | `Server\Program.cs` | YARP route definitions: `/api/patients` → PatientService, `/api/doctors` → DoctorService, etc. |
| Service discovery | `AppHost\AppHost.cs` | Gateway discovers microservices by Aspire service names; no hard-coded URLs |
| Root endpoint | Gateway root handler | `GET /api` returns aggregated discovery links |
| Frontend serving | `Server\Program.cs` — `UseFileServer()` | SPA served as static files from `wwwroot` |

### 4.12 Fan-Out Pattern (New)

The gateway uses the **Fan-Out** pattern for admin operations (`/api/admin/seed`, `/api/admin/reset`, `/api/admin/stats`). A single client request fans out to all microservices in parallel; the gateway aggregates responses and returns a combined result.

| Where | File | Details |
|-------|------|---------|
| Fan-out handler | Gateway admin endpoint | Sends seed/reset/stats requests to PatientService, DoctorService, and ExamService concurrently |
| Aggregation | Gateway admin endpoint | Combines per-service results into a single `SeedResponse`/`ResetResponse`/`StatsResponse` |

### 4.13 Facade Pattern

The frontend `ApiClient` class provides a simplified, unified interface over raw `fetch` calls, HATEOAS link discovery, and HTTP method semantics. The facade hides the complexity of microservice routing behind a single gateway URL.

| Where | File | Details |
|-------|------|---------|
| `ApiClient` | `frontend\src\api\apiClient.ts` | Caches root links; exposes `get<T>`, `post<T>`, `put<T>`, `delete`; navigation via `findLink(links, rel)` |

### 4.14 Composite Pattern

The endpoint registration composes a tree of route groups where each sub-group inherits the parent's path prefix, building a hierarchical URL namespace. Each microservice builds its own route group tree.

| Where | File | Details |
|-------|------|---------|
| Root group | Each `{Service}\Program.cs` — `app.MapGroup("/api")` | Top-level prefix |
| Entity groups | Same file | `api.MapGroup("patients")`, `api.MapGroup("exams")`, etc. |
| Sub-resource groups | Same file | `api.MapGroup("patients/{patientId:guid}/exams")`, `api.MapGroup("doctors/{doctorId:guid}/exams")` |

### 4.15 Template Method Pattern

Integration test classes share a common structure via `IClassFixture<TestWebApplicationFactory>`, where the factory defines the skeleton of server setup (replace LiteDB, configure mapper from Shared) and each test class fills in specific HTTP interactions.

| Where | File | Details |
|-------|------|---------|
| Test factory | `Tests\TestWebApplicationFactory.cs` | Replaces `ILiteDatabase` with in-memory instance; calls `Shared\Stores\LiteDbFactory.ConfigureMapper()` |
| Patient tests | `Tests\PatientEndpointTests.cs` | Full HTTP round-trip: CRUD, HATEOAS link verification |
| Exam tests | `Tests\ExamEndpointTests.cs` | Create with patient dependency, assign-doctor, sub-resource queries |
| Doctor tests | `Tests\DoctorEndpointTests.cs` | CRUD + doctor-exams sub-resource |

### 4.16 Dispose Pattern

Store unit test classes implement `IDisposable` to deterministically release in-memory LiteDB instances after each test.

| Where | File | Details |
|-------|------|---------|
| `PatientStoreTests` | `Tests\UnitTest1.cs` | `IDisposable` — `_db.Dispose()` |
| `DoctorStoreTests` | `Tests\DoctorStoreTests.cs` | Same pattern |
| `ExamStoreTests` | `Tests\ExamStoreTests.cs` | Same pattern |

---

## 5. Patterns and Methodologies: Gaps & Potential Additions

### 5.1 Domain-Driven Design (DDD) — Methodology

| Aspect | Description |
|--------|-------------|
| **What** | A methodology for modelling complex business domains using rich domain models with encapsulated logic, value objects, aggregates, bounded contexts, and domain events |
| **Pros** | Enforces business invariants in one place; makes complex rules explicit; scales with growing complexity |
| **Cons** | Significant overhead for a CRUD-dominant app; requires bounded-context analysis; steeper learning curve; overkill for anemic entities |
| **Where it would apply** | `Patient`, `Doctor`, `Exam` could become aggregate roots; exam status transitions could be guarded by domain rules; `Exam.AssignDoctor()` could enforce specialty matching |

### 5.2 CQRS (Command Query Responsibility Segregation) — Architectural Pattern

| Aspect | Description |
|--------|-------------|
| **What** | Separate code paths for reads (queries) vs. writes (commands) |
| **Status in this solution** | **Implemented** using queued write commands through LavinMQ/RabbitMQ. **Each microservice has its own independent CQRS pipeline.** CQRS abstractions live in `Shared\CqrsAbstractions\*.cs`. |
| **Pros** | Isolates write concerns, supports asynchronous processing, and keeps read endpoints simple |
| **Trade-offs** | Added moving parts (queue, consumer worker, command coordination) and timeout/error handling complexity; now replicated per service |
| **Where implemented** | Write endpoints enqueue commands; each service's `RabbitMqWriteCommandProcessor` executes them via `WriteCommandHandler`; service stores persist changes |

### 5.3 Event Sourcing — Architectural Pattern

| Aspect | Description |
|--------|-------------|
| **What** | Persist every state change as an immutable event rather than overwriting current state; an Event Store is the persistence mechanism |
| **Pros** | Full audit trail; enables temporal queries and replays; natural fit for medical records where history may be legally required |
| **Cons** | Dramatically increases storage and complexity; eventual consistency; replay performance; requires snapshots for large event streams |
| **Where it would apply** | Exam lifecycle (`Scheduled → Assigned → Completed → Cancelled`) is a natural event stream; patient record changes could be audited |

### 5.4 Cache-Aside Pattern — Design Pattern

| Aspect | Description |
|--------|-------------|
| **What** | Check a cache before querying the database; populate the cache on miss; invalidate on writes (`IMemoryCache` or `IDistributedCache`) |
| **Pros** | Reduces latency for hot paths (doctor dropdown, statistics dashboards); lowers load on LiteDB |
| **Cons** | Cache invalidation complexity; memory pressure; stale data risk; LiteDB is already in-process and fast |
| **Where it would apply** | Statistics endpoints (rarely changing aggregations); doctor list (frequently used as a lookup); root API links |

### 5.5 Mediator Pattern — Design Pattern (GoF)

| Aspect | Description |
|--------|-------------|
| **What** | Decouple endpoint handlers from business logic via a mediator object that dispatches commands/queries (e.g., MediatR) |
| **Pros** | Clean separation of concerns; cross-cutting behaviours (logging, validation, caching) as pipeline behaviours; testable handlers |
| **Cons** | Indirection makes the call chain harder to follow; additional dependency; for thin CRUD the ceremony outweighs the benefit |
| **Where it would apply** | Each endpoint handler could dispatch `CreatePatientCommand`, `GetPatientByIdQuery`, etc. |

### 5.6 Result Object Pattern — Design Pattern

| Aspect | Description |
|--------|-------------|
| **What** | Return `Result<T>` or `OneOf<Success, NotFound, ValidationError>` from stores/services instead of `null` checks |
| **Pros** | Eliminates null returns; makes failure modes explicit and type-safe; cleaner endpoint code |
| **Cons** | Requires an extra library or custom type; marginal benefit when failure modes are simple |
| **Where it would apply** | Store methods currently return `null` for "not found" — a `Result<Patient, NotFound>` would be self-documenting |

### 5.7 Specification Pattern — Design Pattern

| Aspect | Description |
|--------|-------------|
| **What** | Encapsulate query criteria (filters, sorting) as reusable, composable specification objects |
| **Pros** | Eliminates duplicated `Where`/`OrderBy` logic across stores; testable query logic; clean store interfaces |
| **Cons** | Adds an abstraction layer; may be over-engineered for simple search; LiteDB's LINQ support is limited |
| **Where it would apply** | `SearchPaged` methods in all stores duplicate the same search-and-sort pattern |

### 5.8 Unit of Work Pattern — Design Pattern

| Aspect | Description |
|--------|-------------|
| **What** | Wrap multiple data operations in a single transactional scope to ensure atomicity |
| **Pros** | Ensures consistency (e.g., deleting a patient also removes their exams); atomic multi-collection operations |
| **Cons** | LiteDB has limited transaction support; adds abstraction overhead; current operations are mostly single-entity |
| **Where it would apply** | `AdminEndpoints.Reset`/`Seed` (multi-collection); cascading deletes |

### 5.9 Decorator Pattern — Design Pattern (GoF)

| Aspect | Description |
|--------|-------------|
| **What** | Wrap store or service calls with cross-cutting concerns (logging, caching, validation) without modifying the original class |
| **Pros** | Adheres to Open/Closed Principle; composable behaviours; clean separation |
| **Cons** | Requires interface abstractions (currently absent); increases number of types |
| **Where it would apply** | A `CachingPatientStore` decorating `PatientStore`; a `LoggingExamStore` wrapping `ExamStore` |

### 5.10 Chain of Responsibility Pattern — Design Pattern (GoF)

| Aspect | Description |
|--------|-------------|
| **What** | Pass a request through a chain of handlers, each deciding whether to process or pass along (e.g., ASP.NET Core middleware, MediatR pipeline behaviours) |
| **Pros** | Flexible composition of cross-cutting concerns; easy to add/remove steps |
| **Cons** | Debugging through the chain can be opaque; order-dependent |
| **Where it would apply** | Request validation → authorisation → logging → handler; the ASP.NET middleware pipeline is already a Chain of Responsibility but no custom middleware is defined |

### 5.11 Strategy Pattern (Validation) — Design Pattern (GoF)

| Aspect | Description |
|--------|-------------|
| **What** | Interchangeable validation strategies per request type (e.g., FluentValidation validators) |
| **Pros** | Rejects invalid input early; structured error responses (RFC 7807); prevents corrupt data |
| **Cons** | Additional library dependency; maintenance of validation rules; current requests have no validation |
| **Where it would apply** | `CreatePatientRequest` (email format, required fields), `CreateExamRequest` (valid status, future date) |

### 5.12 Outbox Pattern — Distributed Systems Pattern

| Aspect | Description |
|--------|-------------|
| **What** | Write domain events to an outbox table atomically with the entity change, then publish asynchronously to a message broker |
| **Pros** | Guaranteed event delivery; enables integration with external systems (notifications, audit) |
| **Cons** | Significant infrastructure overhead; requires a message broker; overkill for a single-service app |
| **Where it would apply** | Exam status changes triggering notifications; patient registration events |

### 5.13 Circuit Breaker Pattern — Resilience Pattern

| Aspect | Description |
|--------|-------------|
| **What** | Detect failures in downstream calls and stop retrying temporarily to prevent cascade failures |
| **Pros** | Already partially present via `AddStandardResilienceHandler()` in `Extensions.cs`; could be extended for external integrations |
| **Cons** | No external downstream calls currently exist; LiteDB is in-process |
| **Where it would apply** | Future integration with external APIs, notification services, or distributed databases |

### 5.14 Feature Toggle Pattern — Operational Pattern

| Aspect | Description |
|--------|-------------|
| **What** | Runtime toggles to enable/disable features without redeployment |
| **Pros** | Safe rollouts; A/B testing; disable dangerous admin endpoints in production |
| **Cons** | Adds conditional logic; requires a feature management library or configuration source |
| **Where it would apply** | Admin seed/reset (dangerous in production); statistics endpoints (beta features) |

---

## 6. Summary Matrix

| # | Name | Category | Status | Primary Location(s) |
|---|------|----------|--------|---------------------|
| 1 | REST | Architectural Style | ✅ Used | `{Service}\Program.cs`, `{Service}\Endpoints\*.cs`, `Server\Program.cs` |
| 2 | HATEOAS | Architectural Constraint (REST L3) | ✅ Used | `Shared\Models\Link.cs`, `{Service}\Endpoints\*.cs`, `frontend\api\apiClient.ts` |
| 3 | Layered Architecture | Architectural Style | ✅ Used | `{Service}\Endpoints\*.cs` → `{Service}\Cqrs\*.cs` → `Shared\Stores\*.cs` → `Shared\Models\*.cs` |
| 4 | Client-Server | Architectural Style | ✅ Used | `Server\Program.cs` (gateway), `{Service}\Program.cs`, `frontend\src\App.tsx` |
| 5 | Service-Oriented (Aspire) | Architectural Style | ✅ Used | `AppHost\AppHost.cs`, `Shared\Extensions.cs` |
| 6 | Microservices | Architectural Style | ✅ Used | PatientService, DoctorService, ExamService, StatisticsService (each with own DB) |
| 7 | CQRS | Architectural Pattern | ✅ Used | `Shared\CqrsAbstractions\*.cs`, `{Service}\Cqrs\*.cs`, write handlers in `{Service}\Endpoints\*.cs` |
| 8 | API Gateway | Design Pattern | ✅ Used | `Server\Program.cs` (YARP reverse proxy) |
| 9 | Fan-Out | Distributed Systems Pattern | ✅ Used | Gateway admin endpoint (parallel seed/reset/stats) |
| 10 | Data Transfer Object | Design Pattern | ✅ Used | `Shared\Models\*Dto.cs` |
| 11 | Repository | Design Pattern | ✅ Used | `Shared\Stores\BaseStore.cs`, `{Service}\Stores\*.cs` |
| 12 | Dependency Injection | Design Pattern | ✅ Used | `{Service}\Program.cs`, all endpoint handlers |
| 13 | Singleton | Design Pattern (GoF) | ✅ Used | `{Service}\Program.cs`, `Shared\Stores\LiteDbFactory.cs` |
| 14 | Factory | Design Pattern (GoF) | ✅ Used | `Shared\Stores\LiteDbFactory.cs` |
| 15 | Builder | Design Pattern (GoF) | ✅ Used | `{Service}\Program.cs`, `AppHost.cs`, `Shared\Models\Link.cs`, `Shared\Extensions.cs` |
| 16 | Observer | Design Pattern (GoF) | ✅ Used | `{Service}\Telemetry\*.cs`, `Shared\Extensions.cs` |
| 17 | Strategy (Sorting) | Design Pattern (GoF) | ✅ Used | `{Service}\Stores\*.cs` — `ApplySort` methods |
| 18 | Adapter | Design Pattern (GoF) | ✅ Used | `Shared\Stores\LiteDbFactory.cs` (type serializers) |
| 19 | Proxy | Design Pattern (GoF) | ✅ Used | `frontend\vite.config.ts` |
| 20 | Facade | Design Pattern (GoF) | ✅ Used | `frontend\src\api\apiClient.ts` |
| 21 | Composite | Design Pattern (GoF) | ✅ Used | `{Service}\Program.cs` (route groups) |
| 22 | Template Method | Design Pattern (GoF) | ✅ Used | `Tests\TestWebApplicationFactory.cs`, `Tests\*EndpointTests.cs` |
| 23 | Dispose | Design Pattern | ✅ Used | `Tests\*StoreTests.cs` |
| 24 | DDD | Methodology | ❌ Not used | — |
| 25 | Event Sourcing / Event Store | Architectural Pattern | ❌ Not used | — |
| 26 | Cache-Aside | Design Pattern | ❌ Not used | — |
| 27 | Mediator | Design Pattern (GoF) | ❌ Not used | — |
| 28 | Result Object | Design Pattern | ❌ Not used | — |
| 29 | Specification | Design Pattern | ❌ Not used | — |
| 30 | Unit of Work | Design Pattern | ❌ Not used | — |
| 31 | Decorator | Design Pattern (GoF) | ❌ Not used | — |
| 32 | Chain of Responsibility | Design Pattern (GoF) | ❌ Not used | — |
| 33 | Strategy (Validation) | Design Pattern (GoF) | ❌ Not used | — |
| 34 | Outbox | Distributed Systems Pattern | ❌ Not used | — |
| 35 | Circuit Breaker | Resilience Pattern | ❌ Not used | — |
| 36 | Feature Toggle | Operational Pattern | ❌ Not used | — |

---

## 7. Key Changes: Monolith-to-Microservices Migration

The solution was migrated from a monolithic architecture to a microservices architecture. Below is a summary of the key structural changes.

### Before (Monolithic)
```
RestReactAspire.Server  (single project)
  ├── Models/           (domain entities + DTOs)
  ├── Stores/           (data access for all entities)
  ├── Endpoints/        (all API endpoints)
  ├── Cqrs/             (single CQRS pipeline)
  ├── Telemetry/        (all telemetry classes)
  └── LiteDbFactory.cs  (one shared DB)
```

### After (Microservices)
```
RestReactAspire.Shared/           (shared across all services)
  ├── Models/                     (domain entities + DTOs)
  ├── Stores/BaseStore.cs         (generic CRUD base)
  ├── Stores/LiteDbFactory.cs     (serializer config)
  ├── CqrsAbstractions/           (interfaces + coordinator)
  └── Telemetry/                  (shared primitives)

RestReactAspire.Server/           (YARP gateway only)
  ├── Program.cs                  (YARP routes)
  └── Extensions.cs               (service defaults)

RestReactAspire.PatientService/   (own DB, CQRS, telemetry)
RestReactAspire.DoctorService/    (own DB, CQRS, telemetry)
RestReactAspire.ExamService/      (own DB, CQRS, telemetry)
RestReactAspire.StatisticsService/(own DB, telemetry, read-optimised)
```

### Key Migration Changes
| Aspect | Before | After |
|--------|--------|-------|
| Database | Single `hospital.db` shared by all entities | Each service owns its own LiteDB file |
| CQRS | One pipeline in Server | Independent pipeline per service; abstractions in Shared |
| Telemetry | Single set in Server | Per-service telemetry; shared primitives in Shared |
| Models/DTOs | In Server/Models | In Shared/Models — referenced by all services |
| Stores | In Server/Stores | Base class in Shared; entity stores in each service |
| API Gateway | None (direct to Server) | YARP reverse proxy in Server |
| Admin operations | Direct store calls in Server | Gateway fan-out to all services |
| DI | Single container | Independent DI per service |
