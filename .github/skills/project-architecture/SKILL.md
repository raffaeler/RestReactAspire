---
name: Project Architecture Overview
description: Understand the overall solution structure, project layout, and how all components connect.
globs:
  - "**/*.csproj"
  - "**/*.sln"
  - "frontend/package.json"
  - "frontend/tsconfig.json"
---

# Project Architecture Overview

## Solution Structure
```
RestReactAspire/
├── RestReactAspire.AppHost/        # .NET Aspire orchestrator
│   └── AppHost.cs
├── RestReactAspire.Server/         # YARP reverse proxy gateway
│   ├── Program.cs                  # Gateway entry point, YARP config
│   └── Extensions.cs               # Service defaults (OpenTelemetry, health, resilience)
├── RestReactAspire.Infrastructure.Cqrs/ # CQRS abstractions NuGet package
│   ├── IWriteCommandQueue.cs        # Queue abstraction
│   ├── InMemoryWriteCommandQueue.cs
│   ├── RabbitMqConnectionManager.cs
│   ├── RabbitMqOptions.cs
│   ├── RabbitMqWriteCommandQueue.cs
│   ├── RabbitMqWriteCommandProcessorBase.cs # Abstract base processor
│   ├── WriteCommandResultCoordinator.cs    # Async result correlation
│   ├── WriteCommandEnvelope.cs
│   ├── WriteCommandResult.cs
│   └── WriteCommands.cs             # All write command record types
├── RestReactAspire.PatientService/ # Patient microservice
│   ├── Program.cs                  # Service entry point, DI, own LiteDB, CQRS wiring
│   ├── Models/                     # Own domain models + DTOs
│   ├── Stores/                     # Own store classes + LiteDbFactory
│   ├── Telemetry/                  # Own telemetry classes
│   ├── Data/                       # Seed data generator
├── RestReactAspire.DoctorService/  # Doctor microservice
│   ├── Program.cs
│   ├── DoctorEndpoints.cs
│   ├── DoctorWriteCommandHandler.cs
│   ├── DoctorInMemoryWriteCommandQueue.cs
│   ├── DoctorRabbitMqWriteCommandProcessor.cs
│   ├── Extensions.cs
│   └── Properties/launchSettings.json # Port config (http://localhost:5102)
├── RestReactAspire.ExamService/    # Exam microservice
│   ├── Program.cs
│   ├── ExamEndpoints.cs
│   ├── ExamWriteCommandHandler.cs
│   ├── ExamInMemoryWriteCommandQueue.cs
│   ├── ExamRabbitMqWriteCommandProcessor.cs
│   ├── Extensions.cs
│   └── Properties/launchSettings.json # Port config (http://localhost:5103)
├── RestReactAspire.StatisticsService/ # Statistics microservice
│   ├── Program.cs
│   ├── StatisticsEndpoints.cs
│   ├── StatisticsWriteCommandHandler.cs
│   ├── StatisticsInMemoryWriteCommandQueue.cs
│   ├── StatisticsRabbitMqWriteCommandProcessor.cs
│   ├── Extensions.cs
│   └── Properties/launchSettings.json # Port config (http://localhost:5104)
├── RestReactAspire.Server.Tests/   # xUnit integration tests
│   ├── TestWebApplicationFactory.cs # Generic factory with marker class pattern
│   ├── PatientServiceEndpointTests.cs (20 tests)
│   ├── DoctorServiceEndpointTests.cs (15 tests)
│   ├── ExamServiceEndpointTests.cs (21 tests)
│   ├── StatisticsServiceEndpointTests.cs (8 tests)
│   └── GatewayEndpointTests.cs (3 tests)
└── frontend/                       # React + TypeScript SPA
    ├── package.json
    ├── src/
    │   ├── main.tsx                # App entry point
    │   ├── App.tsx                 # Routes definition
    │   ├── components/Layout.tsx   # Navigation shell
    │   ├── api/apiClient.ts        # HATEOAS API client
    │   ├── types/                  # TypeScript interfaces
    │   │   ├── hateoas.ts, patient.ts, doctor.ts, exam.ts, statistics.ts
    │   └── pages/                  # Page components
    │       ├── PatientListPage, PatientDetailPage, PatientFormPage
    │       ├── DoctorListPage, DoctorDetailPage, DoctorFormPage, DoctorExamListPage
    │       ├── ExamListPage, ExamDetailPage, ExamFormPage
    │       ├── AdminPage.tsx
    │       └── StatisticsPage.tsx
```

## Key Design Decisions
1. **Microservices**: Each domain entity (Patient, Doctor, Exam, Statistics) lives in its own service with independent database, CQRS pipeline, and telemetry.
2. **HATEOAS-first**: All API navigation is link-driven; the frontend only hard-codes `GET /api`. Links point to gateway URLs.
3. **YARP Gateway**: The Server is a reverse proxy that routes `/api/patients` → PatientService, `/api/doctors` → DoctorService, etc.
4. **CQRS Infrastructure**: CQRS abstractions (interfaces, write commands, RabbitMQ, result coordinator) in `RestReactAspire.Infrastructure.Cqrs` NuGet package.
5. **LiteDB**: Each service has its own embedded NoSQL DB for zero-setup persistence; no migrations needed.
6. **Minimal APIs**: No controllers; all endpoints are static extension methods on `RouteGroupBuilder`.
7. **Aspire**: Orchestrates all services + frontend with service discovery and shared telemetry.
8. **OpenTelemetry**: Full observability with traces, metrics, and structured logs on every endpoint, per service.

## Adding a New Feature End-to-End
1. **Models + DTOs**: Add domain class + DTO records in the service's `Models/` directory.
2. **Store**: Add or extend store class in the service's `Stores/` directory.
3. **New Service**: Create new microservice project; reference `RestReactAspire.Infrastructure.Cqrs` NuGet package.
4. **Endpoints**: Add endpoint class in the service's `Endpoints/` directory.
5. **CQRS**: Implement command/query pipeline in the service's `Cqrs/` directory.
6. **Telemetry**: Add telemetry class in the service's `Telemetry/` directory.
7. **Gateway routes**: Add YARP route configuration in the Server.
8. **AppHost**: Register the new service in `AppHost.cs`.
9. **Frontend types**: Add TypeScript interfaces in `frontend/src/types/`.
10. **Frontend pages**: Add page components in `frontend/src/pages/`.
11. **Routes**: Register routes in `App.tsx`, add nav in `Layout.tsx`.
12. **Tests**: Add integration tests in the test project.
