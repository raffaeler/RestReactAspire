---
name: Backend API Endpoints
description: Create or modify Minimal API endpoints following the project's HATEOAS REST patterns with pagination, search, and sorting.
globs:
  - "**/Endpoints/**"
  - "RestReactAspire.Server/Program.cs"
---

# Backend API Endpoints

## Architecture
- Uses **ASP.NET Core Minimal APIs** with `RouteGroupBuilder` extension methods.
- Endpoints now live in individual microservices (**PatientService**, **DoctorService**, **ExamService**, **StatisticsService**), not in the Server.
- Each service registers its own endpoints in its `Program.cs` under the appropriate route group (e.g., `/api/patients`).
- The **Server** is a YARP reverse proxy gateway that routes requests to the correct microservice.
- The API root discovery endpoint (`GET /api`) is served by the gateway.

## CQRS Pipeline per Service
- Each microservice has its **own CQRS pipeline**: its own `WriteCommandHandler`, `InMemoryWriteCommandQueue`, and `RabbitMqWriteCommandProcessor`.
- **RabbitMQ queues must be unique per service** (e.g., `hospital.patient.write.commands`, `hospital.doctor.write.commands`). Configure via `appsettings.json` → `RabbitMq:QueueName`. Shared queue names cause cross-service message consumption and `TaskCanceledException`.
- The shared library provides abstractions (`IWriteCommandQueue`, `WriteCommandResultCoordinator`, `WriteCommandEnvelope`) but each service implements its own concrete handler and processor.

## Endpoint Registration Pattern
```csharp
public static class {Entity}Endpoints
{
    public static RouteGroupBuilder Map{Entity}Endpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById).WithName("Get{Entity}ById");
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);
        return group;
    }
}
```

Register in `Program.cs`:
```csharp
api.MapGroup("{route}").Map{Entity}Endpoints();
```

## HATEOAS Links
- Every response MUST include an `IReadOnlyList<Link>` with navigational links.
- Single-item responses include: `self`, `update`, `delete`, `collection`, and related resource links.
- List responses include pagination links built via `PaginationLinks.Build(...)`.

## Pagination, Search & Sorting
- List endpoints accept query parameters: `page`, `pageSize`, `search`, `sortBy`, `sortDirection`.
- Delegate filtering/sorting to the Store layer.
- Return `{Entity}ListResponse` containing `Items`, `Pagination`, `Sort`, and `Links`.

## Telemetry
- Every endpoint method MUST start an Activity via the entity's telemetry class.
- Log key information using `ILogger<T>`.
- Increment the appropriate metric counter.
- Set activity tags for traceability.

## Error Handling
- Return `Results.NotFound()` for missing resources.
- Set `ActivityStatusCode.Error` and log warnings on failures.

## Root Endpoint
- The gateway serves `GET /api` returning all discoverable link relations, pointing to gateway URLs.
- When adding new endpoint groups in a microservice, register their discovery links in the gateway's root response.
