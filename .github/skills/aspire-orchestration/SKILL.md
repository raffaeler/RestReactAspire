---
name: Aspire Orchestration
description: Configure and manage the .NET Aspire AppHost for local development orchestration.
globs:
  - "RestReactAspire.AppHost/**"
---

# Aspire Orchestration

## Overview
The solution uses **.NET Aspire** to orchestrate all microservices, the YARP gateway, and the frontend during local development.

## AppHost Configuration
Located in `RestReactAspire.AppHost/AppHost.cs`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// LavinMQ container (shared message broker — each service uses its own queue)
var lavinMq = builder.AddContainer("lavinmq", "cloudamqp/lavinmq")
    .WithEndpoint(name: "amqp", port: 5672, targetPort: 5672);

// Microservices (ports configured via launchSettings.json)
var patientService = builder.AddProject<Projects.RestReactAspire_PatientService>("patient-service")
    .WithHttpHealthCheck("/health")
    .WaitFor(lavinMq);
// ... same pattern for doctor, exam, statistics

// Gateway - waits for all services, references them for service discovery
var server = builder.AddProject<Projects.RestReactAspire_Server>("server")
    .WithHttpHealthCheck("/health")
    .WaitFor(patientService).WaitFor(doctorService)
    .WaitFor(examService).WaitFor(statisticsService)
    .WithReference(patientService).WithReference(doctorService)
    .WithReference(examService).WithReference(statisticsService)
    .WithExternalHttpEndpoints();
```

## Port Configuration
- **Service ports are configured in `Properties/launchSettings.json`**, not in AppHost. Each microservice uses a fixed HTTP port: PatientService=5101, DoctorService=5102, ExamService=5103, StatisticsService=5104.
- Do NOT use `WithEndpoint()` with matching `Port`+`TargetPort` on non-container resources — Aspire proxies these and throws.
- The gateway's YARP destinations resolve these ports at runtime from Aspire's service discovery environment variables (falling back to localhost:5101-5104 for standalone dev).

## Key Components
- **PatientService, DoctorService, ExamService, StatisticsService**: Four independent microservices, each with its own database, CQRS pipeline, and telemetry.
- **Server**: YARP reverse proxy gateway (`RestReactAspire.Server`). Routes `/api/patients` → PatientService, `/api/doctors` → DoctorService, etc.
- **Frontend**: React/Vite app added via `AddViteApp()` with a reference to the gateway.
- **Health checks**: `/health` and `/alive` endpoints configured in each service's `Extensions.cs`.
- **Service discovery**: Enabled by default via Aspire service defaults. The gateway discovers microservices by their Aspire service names.

## Service Defaults (`Extensions.cs`)
Shared configuration applied to all services (via `RestReactAspire.Shared`):
- OpenTelemetry (traces, metrics, logs)
- Health checks (readiness + liveness)
- Service discovery
- HTTP client resilience

## Adding New Services
1. Add the project to the solution.
2. Register it in `AppHost.cs` with `builder.AddProject<T>()`.
3. Add a reference from the gateway to the new service with `.WithReference()`.
4. Add YARP route configuration in the Server.
5. Ensure the new service calls `builder.AddServiceDefaults()` for consistent telemetry and health checks.
