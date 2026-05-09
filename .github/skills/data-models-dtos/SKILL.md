---
name: Data Models and DTOs
description: Create or modify domain models, request/response DTOs, and HATEOAS link structures.
globs:
  - "**/Models/**"
---

# Data Models and DTOs

## Domain Models
- Each service owns its domain models in `{Service}.Models/` (e.g., `PatientService.Models`, `DoctorService.Models`).
- Each entity is a plain C# class with `Guid Id` as the primary key.
- Properties use `required` modifier where appropriate.
- LiteDB-specific attributes (e.g., `[BsonIgnore]`) for computed properties.

### Existing Models
- `Patient`: Id, FirstName, LastName, DateOfBirth, Email, Phone
- `Doctor`: Id, FirstName, LastName, Specialty, Email, Phone
- `Exam`: Id, PatientId, DoctorId?, Type, ScheduledDate, ScheduledTime?, DurationMinutes?, Status, Results?, Notes?, EndTime (computed)

## DTO Pattern
Each entity has a separate `{Entity}Dto.cs` file containing:
- `Create{Entity}Request` record — for POST bodies
- `Update{Entity}Request` record — for PUT bodies
- `{Entity}Response` record — includes all fields plus `IReadOnlyList<Link> Links`
- `{Entity}ListResponse` record — includes `Items`, `PaginationInfo`, `SortInfo`, and `Links`

## Per-Service HATEOAS Types
Each service owns its own `Link.cs` model (e.g., `PatientService.Models.Link`, `DoctorService.Models.Link`):
- `Link(string Rel, string Href, string Method)` — single navigational link
- `PaginationInfo(int Page, int PageSize, int TotalCount, int TotalPages)`
- `SortInfo(string SortBy, string SortDirection)`
- `PaginationLinks.Build(...)` — static helper that generates self/first/last/prev/next links with search and sort parameters encoded in the query string.

## Conventions
- All DTOs are `record` types for immutability.
- Response records always end with `IReadOnlyList<Link> Links`.
- Use `DateOnly` for dates and `TimeOnly` for times (with custom LiteDB serializers in each service's `Stores/LiteDbFactory`).
- All models and DTOs live in each service's own `Models/` directory. Only CQRS abstractions are shared via `RestReactAspire.Infrastructure.Cqrs`.

## Frontend TypeScript Types
- Mirror types are in `frontend/src/types/` (e.g., `patient.ts`, `exam.ts`, `doctor.ts`, `hateoas.ts`, `statistics.ts`).
- Keep backend DTOs and frontend types in sync when modifying models.
