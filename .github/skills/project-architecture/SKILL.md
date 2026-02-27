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
├── RestReactAspire.Server/         # ASP.NET Core Minimal API backend
│   ├── Program.cs                  # Application entry point and DI setup
│   ├── Extensions.cs               # Service defaults (OpenTelemetry, health, resilience)
│   ├── Models/                     # Domain entities and DTOs
│   │   ├── Patient.cs, PatientDto.cs
│   │   ├── Doctor.cs, DoctorDto.cs
│   │   ├── Exam.cs, ExamDto.cs
│   │   ├── Link.cs                 # HATEOAS types + PaginationLinks helper
│   │   ├── AdminDto.cs
│   │   └── StatisticsDto.cs
│   ├── Stores/                     # Data access layer (LiteDB)
│   │   ├── PatientStore.cs
│   │   ├── DoctorStore.cs
│   │   ├── ExamStore.cs
│   │   ├── LiteDbFactory.cs        # LiteDB custom serializer registration
│   │   └── SeedDataGenerator.cs    # Sample data generation
│   ├── Endpoints/                  # Minimal API endpoint definitions
│   │   ├── RootEndpoints.cs        # API discovery root
│   │   ├── PatientEndpoints.cs
│   │   ├── DoctorEndpoints.cs
│   │   ├── ExamEndpoints.cs
│   │   ├── AdminEndpoints.cs
│   │   └── StatisticsEndpoints.cs
│   └── Telemetry/                  # OpenTelemetry instrumentation
│       ├── PatientTelemetry.cs
│       ├── DoctorTelemetry.cs
│       ├── ExamTelemetry.cs
│       ├── AdminTelemetry.cs
│       ├── RootTelemetry.cs
│       └── StatisticsTelemetry.cs
├── RestReactAspire.Server.Tests/   # xUnit integration tests
│   ├── TestWebApplicationFactory.cs
│   ├── PatientEndpointTests.cs
│   ├── ExamEndpointTests.cs, ExamStoreTests.cs
│   └── DoctorEndpointTests.cs, DoctorStoreTests.cs
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
1. **HATEOAS-first**: All API navigation is link-driven; the frontend only hard-codes `GET /api`.
2. **LiteDB**: Embedded NoSQL DB for zero-setup persistence; no migrations needed.
3. **Minimal APIs**: No controllers; all endpoints are static extension methods on `RouteGroupBuilder`.
4. **Aspire**: Orchestrates backend + frontend with service discovery and shared telemetry.
5. **OpenTelemetry**: Full observability with traces, metrics, and structured logs on every endpoint.

## Adding a New Feature End-to-End
1. **Model**: Add domain class + DTO records in `Models/`.
2. **Store**: Add LiteDB store class in `Stores/` with CRUD + pagination.
3. **Telemetry**: Add telemetry class in `Telemetry/`, register in `Extensions.cs`.
4. **Endpoints**: Add endpoint class in `Endpoints/`, register in `Program.cs`.
5. **Root links**: Add discovery links in `RootEndpoints.cs`.
6. **Frontend types**: Add TypeScript interfaces in `frontend/src/types/`.
7. **Frontend pages**: Add page components in `frontend/src/pages/`.
8. **Routes**: Register routes in `App.tsx`, add nav in `Layout.tsx`.
9. **Tests**: Add integration tests in the test project.
