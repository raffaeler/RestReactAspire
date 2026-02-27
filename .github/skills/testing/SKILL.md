---
name: Testing
description: Write and maintain xUnit integration tests for the REST API using TestWebApplicationFactory.
globs:
  - "RestReactAspire.Server.Tests/**"
---

# Testing

## Framework
- **xUnit** for test execution.
- **Microsoft.AspNetCore.Mvc.Testing** for integration tests via `WebApplicationFactory<Program>`.

## Test Infrastructure

### TestWebApplicationFactory
Located in `RestReactAspire.Server.Tests/TestWebApplicationFactory.cs`:
- Replaces the real LiteDB with an in-memory instance (`LiteDatabase(":memory:")`).
- Calls `LiteDbFactory.ConfigureMapper()` to register custom type serializers.
- Tests share the factory via `IClassFixture<TestWebApplicationFactory>`.

```csharp
public class {Entity}EndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public {Entity}EndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
}
```

## Test Patterns

### CRUD Endpoint Tests
For each entity, test:
1. **GET list** — returns 200 with valid structure (Items, Links, Pagination).
2. **POST create** — returns 201 Created with HATEOAS links.
3. **GET by ID** — returns 200 or 404.
4. **PUT update** — returns 200 with updated data or 404.
5. **DELETE** — returns 204 or 404.
6. **Round-trip** — create then retrieve verifies data integrity.

### HATEOAS Verification
- Assert that responses contain expected link relations (`self`, `update`, `delete`, `collection`, `create`).
- Assert pagination links appear in list responses.

### Assertions
- Use `response.EnsureSuccessStatusCode()` for happy paths.
- Use `Assert.Equal(HttpStatusCode.{Code}, response.StatusCode)` for specific status checks.
- Deserialize with `ReadFromJsonAsync<T>()` and assert on properties.

## Existing Test Files
- `PatientEndpointTests.cs` — CRUD + HATEOAS tests for patients
- `ExamEndpointTests.cs` — CRUD tests for exams
- `ExamStoreTests.cs` — Unit tests for ExamStore
- `DoctorEndpointTests.cs` — CRUD tests for doctors
- `DoctorStoreTests.cs` — Unit tests for DoctorStore

## Adding Tests for New Features
1. Create `{Entity}EndpointTests.cs` in the test project.
2. Use `IClassFixture<TestWebApplicationFactory>` for the test class.
3. Test all CRUD operations and HATEOAS link presence.
4. Test pagination, search, and sorting query parameters.
5. Test error cases (404 for missing resources).
