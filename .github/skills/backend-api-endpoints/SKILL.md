---
name: Backend API Endpoints
description: Create or modify Minimal API endpoints following the project's HATEOAS REST patterns with pagination, search, and sorting.
globs:
  - "RestReactAspire.Server/Endpoints/**"
  - "RestReactAspire.Server/Program.cs"
---

# Backend API Endpoints

## Architecture
- Uses **ASP.NET Core Minimal APIs** with `RouteGroupBuilder` extension methods.
- All endpoints are registered in `Program.cs` under the `/api` route group.
- Each entity has its own static class in `RestReactAspire.Server/Endpoints/` (e.g., `PatientEndpoints.cs`).

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
- `RootEndpoints.cs` exposes `GET /api` returning all discoverable link relations.
- When adding new endpoint groups, register their discovery links in the root response.
