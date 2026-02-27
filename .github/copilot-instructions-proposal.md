This solution is a HATEOAS-compliant REST tutorial implementing a fictitious day-hospital management system.

## Scenario
A day-hospital system for managing patients, doctors, and medical exams. Features include CRUD operations, server-side pagination/search/sorting, statistics dashboards, seed data management, and full OpenTelemetry observability.

Refer to the Copilot skills in `.github/skills/` for detailed implementation guidance. Each skill is a subdirectory containing a `SKILL.md` file:

| Skill Directory | Purpose |
|-----------------|---------|
| `project-architecture/` | Solution structure, project layout, and end-to-end feature checklist |
| `backend-api-endpoints/` | Minimal API endpoint patterns with HATEOAS, telemetry, and error handling |
| `data-models-dtos/` | Domain models, request/response DTOs, and shared HATEOAS types |
| `data-store-layer/` | LiteDB store pattern with CRUD, pagination, search, and sorting |
| `litedb-configuration/` | Custom type serializers, entity pre-warming, and database setup |
| `hateoas-rest-design/` | HATEOAS principles, link relations, HTTP methods, and status codes |
| `telemetry-observability/` | OpenTelemetry traces, metrics, and structured logging patterns |
| `pagination-search-sorting/` | Server-side pagination, search, and sortable columns (backend + frontend) |
| `frontend-pages/` | React pages with MUI, React Router, recharts, and HATEOAS API client |
| `statistics-charts/` | Statistics endpoints and recharts visualizations |
| `admin-seed-data/` | Database seeding, reset operations, and admin interface |
| `testing/` | xUnit integration tests with TestWebApplicationFactory |
| `aspire-orchestration/` | .NET Aspire AppHost configuration and service defaults |

## Technology Stack
- **Backend**: .NET 10, ASP.NET Core Minimal APIs, Aspire, LiteDB, xUnit
- **Frontend**: React 19, TypeScript, MUI v7, React Router v7, recharts v3, Vite
- **Observability**: OpenTelemetry (Traces, Metrics, Logs)

## Key Design Principles
1. **HATEOAS-first**: Clients discover API actions via link relations. Only `GET /api` is hard-coded.
2. **Minimal APIs**: No controllers — all endpoints are `RouteGroupBuilder` extensions.
3. **Full observability**: Every endpoint has Activities, metric counters, and structured logs.
4. **LiteDB**: Embedded NoSQL for zero-setup persistence without schema migrations.
5. **Aspire orchestration**: Backend and frontend are orchestrated with service discovery and shared telemetry.
