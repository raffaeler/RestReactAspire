# Architecture & Design Patterns — RestReactAspire

> A HATEOAS-compliant REST day-hospital management system built with
> .NET 10, ASP.NET Core, .NET Aspire, React 19, TypeScript, and LiteDB.

---

## 1. Solution Overview

| Project | Role |
|---------|------|
| `RestReactAspire.AppHost` | .NET Aspire orchestrator — wires backend, frontend, and shared telemetry |
| `RestReactAspire.Server` | ASP.NET Core backend (API, data access, telemetry) |
| `RestReactAspire.Server.Tests` | xUnit integration and unit tests |
| `frontend/` | React 19 SPA (TypeScript, MUI v7, React Router v7, recharts v3, Vite 7) |

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
| Route tree | `Server\Program.cs` (lines 36–44) | `MapGroup` creates resource URIs: `/api/patients`, `/api/exams`, `/api/doctors`, `/api/admin`, `/api/statistics` |
| Patient resource | `Server\Endpoints\PatientEndpoints.cs` | `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}` |
| Exam resource | `Server\Endpoints\ExamEndpoints.cs` | CRUD + `PUT /{id}/doctor`; sub-resource `GET /patients/{patientId}/exams` |
| Doctor resource | `Server\Endpoints\DoctorEndpoints.cs` | CRUD + sub-resource `GET /doctors/{doctorId}/exams` |
| Admin operations | `Server\Endpoints\AdminEndpoints.cs` | `POST /seed`, `POST /reset`, `GET /stats` |
| Statistics (read-only) | `Server\Endpoints\StatisticsEndpoints.cs` | Four aggregation endpoints |
| API entry point | `Server\Endpoints\RootEndpoints.cs` | `GET /api` |

### 3.2 HATEOAS (Hypermedia as the Engine of Application State)

A REST maturity constraint (Richardson Maturity Level 3). Every API response embeds discoverable `Link` objects (`rel`, `href`, `method`) so clients navigate exclusively via hypermedia, never hard-coding URLs beyond the single entry point `GET /api`.

| Where | File(s) | Details |
|-------|---------|---------|
| Link model | `Server\Models\Link.cs` — `Link`, `PaginationInfo`, `SortInfo`, `PaginationLinks` | Shared HATEOAS primitives; `PaginationLinks.Build()` generates `self/first/last/prev/next` |
| Root discovery | `Server\Endpoints\RootEndpoints.cs` — `MapRootEndpoints` | `GET /api` returns `ApiRootResponse` with all top-level link relations |
| Per-resource links | `Server\Endpoints\PatientEndpoints.cs` — `ToPatientResponse` | `self`, `update`, `delete`, `exams`, `collection` |
| | `Server\Endpoints\DoctorEndpoints.cs` — `ToDoctorResponse` | Same pattern |
| | `Server\Endpoints\ExamEndpoints.cs` — `ToExamResponse` | Adds `assign-doctor`, `patient`, `patient-exams`, conditional `doctor`/`doctor-exams` |
| | `Server\Endpoints\AdminEndpoints.cs` | Seed/Reset/Stats responses carry cross-resource navigation links |
| | `Server\Endpoints\StatisticsEndpoints.cs` — `GetStatisticsLinks` | Links to sibling charts and entity collections |
| Frontend consumer | `frontend\src\api\apiClient.ts` — `discoverApi()`, `getLink()`, `findLink()` | Client discovers the root once and navigates via link relations |
| Frontend types | `frontend\src\types\hateoas.ts` | TypeScript contracts mirroring server `Link` model |

### 3.3 Layered Architecture

The backend now follows a **CQRS-oriented layered design** where reads and writes are separated.

| Layer | Files | Responsibility |
|-------|-------|----------------|
| **Presentation (Endpoints)** | `Server\Endpoints\*.cs` | HTTP mapping, response shaping, telemetry, HATEOAS link generation |
| **Command Layer (Write Side)** | `Server\Cqrs\*.cs` | Build write commands, enqueue to LavinMQ (RabbitMQ protocol), process queued commands, coordinate command results |
| **Query/Data Access Layer (Read + Persistence)** | `Server\Stores\PatientStore.cs`, `DoctorStore.cs`, `ExamStore.cs` | Queries, pagination/search/sorting, persistence operations executed by command handlers |
| **Models** | `Server\Models\*.cs` | Domain entities and DTOs (shared across layers) |

### 3.4 Client-Server Architecture

The system is divided into a backend API server and a single-page application frontend, communicating exclusively via HTTP/JSON. In development, a reverse proxy bridges the two.

| Component | File | Details |
|-----------|------|---------|
| Backend | `Server\Program.cs` | ASP.NET Core API |
| Frontend | `frontend\src\App.tsx` | React 19 SPA with `BrowserRouter` |
| Dev proxy | `frontend\vite.config.ts` | Vite proxies `/api` to the backend via Aspire-injected env vars |
| Production serving | `Server\Program.cs` — `app.UseFileServer()` | SPA served as static files from `wwwroot` |

### 3.5 Service-Oriented Architecture (Aspire Orchestration)

.NET Aspire orchestrates backend and frontend as independently configured services with shared telemetry, health checks, and service discovery.

| Where | File | Details |
|-------|------|---------|
| AppHost | `AppHost\AppHost.cs` | `AddProject` (backend), `AddViteApp` (frontend), health checks, service references, container publishing |
| Service Defaults | `Server\Extensions.cs` — `AddServiceDefaults` | Adds service discovery, HTTP resilience, OpenTelemetry, health checks |

### 3.6 CQRS with Asynchronous Messaging

Writes are handled as commands and queued through LavinMQ using the RabbitMQ protocol. A background processor consumes commands and applies state changes to LiteDB through stores. Reads remain direct query operations from endpoint handlers.

| Where | File(s) | Details |
|-------|---------|---------|
| Command contracts | `Server\Cqrs\WriteCommands.cs` | Write command records + `WriteCommandEnvelope` |
| Queue abstraction | `Server\Cqrs\IWriteCommandQueue.cs` | Endpoint write handlers depend on an abstraction |
| RabbitMQ producer | `Server\Cqrs\RabbitMqWriteCommandQueue.cs` | Enqueues persistent messages to LavinMQ queue |
| RabbitMQ consumer | `Server\Cqrs\RabbitMqWriteCommandProcessor.cs` | Background worker dequeues and executes commands |
| Command execution | `Server\Cqrs\WriteCommandHandler.cs` | Applies write operations via stores |
| Request/response sync | `Server\Cqrs\WriteCommandResultCoordinator.cs` | Correlates HTTP request with command completion |
| Runtime registration | `Server\Program.cs` | Registers CQRS services; uses in-memory queue in `Testing` environment |
| Aspire dependency | `AppHost\AppHost.cs` | Backend waits for `lavinmq` container before startup |

---

## 4. Design Patterns

### 4.1 Data Transfer Object (DTO)

Separate immutable record types for creation requests, update requests, and responses. Decouples the API contract from internal domain entities.

| DTO set | File |
|---------|------|
| Patient DTOs | `Server\Models\PatientDto.cs` — `CreatePatientRequest`, `UpdatePatientRequest`, `PatientResponse`, `PatientListResponse`, `ApiRootResponse` |
| Doctor DTOs | `Server\Models\DoctorDto.cs` — `CreateDoctorRequest`, `UpdateDoctorRequest`, `DoctorResponse`, `DoctorListResponse`, `AssignDoctorRequest` |
| Exam DTOs | `Server\Models\ExamDto.cs` — `CreateExamRequest`, `UpdateExamRequest`, `ExamResponse`, `ExamListResponse` |
| Admin DTOs | `Server\Models\AdminDto.cs` — `SeedResponse`, `ResetResponse`, `StatsResponse` |
| Statistics DTOs | `Server\Models\StatisticsDto.cs` — `PatientsByAgeGroupResponse`, `ExamsPerDoctorResponse`, `ExamsOverTimeResponse`, `AvgDurationByExamTypeResponse` |
| HATEOAS primitives | `Server\Models\Link.cs` — `Link`, `PaginationInfo`, `SortInfo` |

### 4.2 Repository Pattern

Each entity has a dedicated **Store** class that encapsulates all data access logic against a LiteDB collection. Stores are the single point of contact with the database.

| Store | File | Key Methods |
|-------|------|-------------|
| `PatientStore` | `Server\Stores\PatientStore.cs` | `GetAll`, `GetPaged`, `SearchPaged`, `GetById`, `Add`, `Update`, `Delete`, `InsertBulk`, `DeleteAll` |
| `DoctorStore` | `Server\Stores\DoctorStore.cs` | Same CRUD + search/sort + bulk/reset helpers |
| `ExamStore` | `Server\Stores\ExamStore.cs` | Adds `GetByPatientId*`, `GetByDoctorId*`, `AssignDoctor`, bulk/reset helpers |

### 4.3 Dependency Injection (IoC Container)

All runtime dependencies are resolved through the built-in ASP.NET Core DI container using constructor injection and parameter injection.

| Registration | File | Details |
|-------------|------|---------|
| `ILiteDatabase` singleton | `Server\Program.cs` (line 19) | Single shared LiteDB instance |
| Store singletons | `Server\Program.cs` (lines 22–24) | `PatientStore`, `ExamStore`, `DoctorStore` |
| CQRS services | `Server\Program.cs`, `Server\Cqrs\*.cs` | `WriteCommandHandler`, `WriteCommandResultCoordinator`, queue implementation, RabbitMQ connection manager, background processor |
| Endpoint parameter injection | `Server\Endpoints\*.cs` | Handler parameters resolved from DI (e.g., `PatientStore store`, `ILogger<PatientStore> logger`) |

### 4.4 Singleton Pattern

The embedded database and its stores use the Singleton lifecycle to ensure a single shared instance across the application.

| Where | File | Details |
|-------|------|---------|
| `ILiteDatabase` | `Server\Program.cs` (line 19) | `Connection=shared` for concurrent access |
| Stores | `Server\Program.cs` (lines 22–24) | Registered as singletons; hold references to the singleton DB |
| CQRS coordinator | `Server\Program.cs`, `Server\Cqrs\WriteCommandResultCoordinator.cs` | Singleton command result correlation across request/worker boundary |
| `LiteDbFactory._configured` | `Server\Stores\LiteDbFactory.cs` | Thread-safe one-time initialization with `lock` + boolean guard |

### 4.5 Factory Pattern

A static factory encapsulates LiteDB mapper configuration, including custom type serializers and entity pre-warming.

| Where | File | Details |
|-------|------|---------|
| `LiteDbFactory.ConfigureMapper` | `Server\Stores\LiteDbFactory.cs` | Registers `DateOnly`/`TimeOnly` serializers, pre-warms entity mapper cache |

### 4.6 Builder Pattern

Used pervasively through host configuration APIs and in HATEOAS link generation.

| Where | File | Details |
|-------|------|---------|
| Application builder | `Server\Program.cs` | `WebApplication.CreateBuilder` → `AddServiceDefaults` → `Build` → `Run` |
| Aspire orchestration | `AppHost\AppHost.cs` | `DistributedApplication.CreateBuilder` → `AddProject` → `AddViteApp` → `Build` → `Run` |
| Pagination link builder | `Server\Models\Link.cs` — `PaginationLinks.Build()` | Fluent construction of `self/first/last/prev/next` links with query parameters |
| OpenTelemetry pipeline | `Server\Extensions.cs` — `ConfigureOpenTelemetry` | `.WithMetrics(m => ...)` `.WithTracing(t => ...)` chain |

### 4.7 Observer Pattern

The telemetry layer implements the Observer pattern through `ActivitySource` (distributed traces) and `Meter`/`Counter` (metrics). Observers (OTLP exporters) subscribe to these sources without coupling to the endpoint logic.

| Telemetry class | File | Instruments |
|----------------|------|-------------|
| `PatientTelemetry` | `Server\Telemetry\PatientTelemetry.cs` | `ActivitySource`, counters: `PatientsQueried`, `PatientsCreated`, `PatientsUpdated`, `PatientsDeleted` |
| `ExamTelemetry` | `Server\Telemetry\ExamTelemetry.cs` | Same pattern for exams |
| `DoctorTelemetry` | `Server\Telemetry\DoctorTelemetry.cs` | Same pattern for doctors |
| `AdminTelemetry` | `Server\Telemetry\AdminTelemetry.cs` | `StatsQueried`, `DatabaseSeeded`, `DatabaseReset` |
| `RootTelemetry` | `Server\Telemetry\RootTelemetry.cs` | `RootRequested` |
| `StatisticsTelemetry` | `Server\Telemetry\StatisticsTelemetry.cs` | Four chart-specific query counters |
| Observer registration | `Server\Extensions.cs` — `ConfigureOpenTelemetry` | Registers all sources and meters; OTLP exporter subscribes as observer |

### 4.8 Strategy Pattern (Sorting)

Each store uses a strategy-like dispatch to select the sorting algorithm at runtime based on the `sortBy` parameter.

| Where | File | Details |
|-------|------|---------|
| `PatientStore.ApplySort` | `Server\Stores\PatientStore.cs` | `switch` expression selects `OrderBy`/`OrderByDescending` by column name |
| `DoctorStore.ApplySort` | `Server\Stores\DoctorStore.cs` | Same dispatch pattern |
| `ExamStore.ApplySort` | `Server\Stores\ExamStore.cs` | Same dispatch pattern |

### 4.9 Adapter Pattern

Custom LiteDB type serializers adapt .NET types (`DateOnly`, `TimeOnly`) to BSON-compatible representations, bridging the incompatibility between the .NET type system and LiteDB's storage format.

| Where | File | Details |
|-------|------|---------|
| `DateOnly` adapter | `Server\Stores\LiteDbFactory.cs` | `BsonMapper.Global.RegisterType` — ISO 8601 round-trip format |
| `TimeOnly` adapter | Same file | Same approach |

### 4.10 Proxy Pattern

In development, the Vite dev server acts as a reverse proxy, forwarding `/api` requests to the backend. This decouples the frontend development server from the backend origin.

| Where | File | Details |
|-------|------|---------|
| Vite proxy | `frontend\vite.config.ts` | Forwards `/api` to backend via Aspire-injected `SERVER_HTTPS`/`SERVER_HTTP` |
| Service reference | `AppHost\AppHost.cs` — `.WithReference(server)` | Aspire injects backend URLs into the frontend process |

### 4.11 Facade Pattern

The frontend `ApiClient` class provides a simplified, unified interface over raw `fetch` calls, HATEOAS link discovery, and HTTP method semantics.

| Where | File | Details |
|-------|------|---------|
| `ApiClient` | `frontend\src\api\apiClient.ts` | Caches root links; exposes `get<T>`, `post<T>`, `put<T>`, `delete`; navigation via `findLink(links, rel)` |

### 4.12 Composite Pattern

The endpoint registration composes a tree of route groups where each sub-group inherits the parent's path prefix, building a hierarchical URL namespace.

| Where | File | Details |
|-------|------|---------|
| Root group | `Server\Program.cs` — `app.MapGroup("/api")` | Top-level prefix |
| Entity groups | Same file | `api.MapGroup("patients")`, `api.MapGroup("exams")`, etc. |
| Sub-resource groups | Same file | `api.MapGroup("patients/{patientId:guid}/exams")`, `api.MapGroup("doctors/{doctorId:guid}/exams")` |

### 4.13 Template Method Pattern

Integration test classes share a common structure via `IClassFixture<TestWebApplicationFactory>`, where the factory defines the skeleton of server setup (replace LiteDB, configure mapper) and each test class fills in specific HTTP interactions.

| Where | File | Details |
|-------|------|---------|
| Test factory | `Tests\TestWebApplicationFactory.cs` | Replaces `ILiteDatabase` with in-memory instance; calls `LiteDbFactory.ConfigureMapper()` |
| Patient tests | `Tests\PatientEndpointTests.cs` | Full HTTP round-trip: CRUD, HATEOAS link verification |
| Exam tests | `Tests\ExamEndpointTests.cs` | Create with patient dependency, assign-doctor, sub-resource queries |
| Doctor tests | `Tests\DoctorEndpointTests.cs` | CRUD + doctor-exams sub-resource |

### 4.14 Dispose Pattern

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
| **Status in this solution** | **Implemented** using queued write commands through LavinMQ/RabbitMQ in `Server\Cqrs\*.cs` |
| **Pros** | Isolates write concerns, supports asynchronous processing, and keeps read endpoints simple |
| **Trade-offs** | Added moving parts (queue, consumer worker, command coordination) and timeout/error handling complexity |
| **Where implemented** | Write endpoints enqueue commands; `RabbitMqWriteCommandProcessor` executes them via `WriteCommandHandler`; stores persist changes |

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
| 1 | REST | Architectural Style | ✅ Used | `Program.cs`, `Endpoints\*.cs` |
| 2 | HATEOAS | Architectural Constraint (REST L3) | ✅ Used | `Endpoints\*.cs`, `Models\Link.cs`, `frontend\api\apiClient.ts` |
| 3 | Layered Architecture | Architectural Style | ✅ Used | `Endpoints\*.cs` → `Stores\*.cs` → `Models\*.cs` |
| 4 | Client-Server | Architectural Style | ✅ Used | `Server\Program.cs`, `frontend\src\App.tsx` |
| 5 | Service-Oriented (Aspire) | Architectural Style | ✅ Used | `AppHost\AppHost.cs`, `Server\Extensions.cs` |
| 6 | Data Transfer Object | Design Pattern | ✅ Used | `Models\*Dto.cs` |
| 7 | Repository | Design Pattern | ✅ Used | `Stores\PatientStore.cs`, `DoctorStore.cs`, `ExamStore.cs` |
| 8 | Dependency Injection | Design Pattern | ✅ Used | `Program.cs`, all endpoint handlers |
| 9 | Singleton | Design Pattern (GoF) | ✅ Used | `Program.cs`, `Stores\LiteDbFactory.cs` |
| 10 | Factory | Design Pattern (GoF) | ✅ Used | `Stores\LiteDbFactory.cs` |
| 11 | Builder | Design Pattern (GoF) | ✅ Used | `Program.cs`, `AppHost.cs`, `Models\Link.cs`, `Extensions.cs` |
| 12 | Observer | Design Pattern (GoF) | ✅ Used | `Telemetry\*.cs`, `Extensions.cs` |
| 13 | Strategy (Sorting) | Design Pattern (GoF) | ✅ Used | `Stores\*.cs` — `ApplySort` methods |
| 14 | Adapter | Design Pattern (GoF) | ✅ Used | `Stores\LiteDbFactory.cs` (type serializers) |
| 15 | Proxy | Design Pattern (GoF) | ✅ Used | `frontend\vite.config.ts` |
| 16 | Facade | Design Pattern (GoF) | ✅ Used | `frontend\src\api\apiClient.ts` |
| 17 | Composite | Design Pattern (GoF) | ✅ Used | `Program.cs` (route groups) |
| 18 | Template Method | Design Pattern (GoF) | ✅ Used | `Tests\TestWebApplicationFactory.cs`, `Tests\*EndpointTests.cs` |
| 19 | Dispose | Design Pattern | ✅ Used | `Tests\*StoreTests.cs` |
| 20 | DDD | Methodology | ❌ Not used | — |
| 21 | CQRS | Architectural Pattern | ✅ Used | `Server\Cqrs\*.cs`, `Server\Program.cs`, write handlers in `Endpoints\*.cs` |
| 22 | Event Sourcing / Event Store | Architectural Pattern | ❌ Not used | — |
| 23 | Cache-Aside | Design Pattern | ❌ Not used | — |
| 24 | Mediator | Design Pattern (GoF) | ❌ Not used | — |
| 25 | Result Object | Design Pattern | ❌ Not used | — |
| 26 | Specification | Design Pattern | ❌ Not used | — |
| 27 | Unit of Work | Design Pattern | ❌ Not used | — |
| 28 | Decorator | Design Pattern (GoF) | ❌ Not used | — |
| 29 | Chain of Responsibility | Design Pattern (GoF) | ❌ Not used | — |
| 30 | Strategy (Validation) | Design Pattern (GoF) | ❌ Not used | — |
| 31 | Outbox | Distributed Systems Pattern | ❌ Not used | — |
| 32 | Circuit Breaker | Resilience Pattern | ❌ Not used | — |
| 33 | Feature Toggle | Operational Pattern | ❌ Not used | — |
