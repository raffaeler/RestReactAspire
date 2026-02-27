---
name: Data Store Layer
description: Create or modify LiteDB data stores with pagination, search, and sorting support.
globs:
  - "RestReactAspire.Server/Stores/**"
---

# Data Store Layer

## Technology
- Uses **LiteDB** (embedded NoSQL database) via the `ILiteDatabase` interface.
- Connection string configured in `Program.cs` with `Connection=shared` mode.
- Custom serializers for `DateOnly` and `TimeOnly` are registered in `LiteDbFactory.ConfigureMapper()`.

## Store Pattern
Each entity has a `{Entity}Store` class in `RestReactAspire.Server/Stores/`:

```csharp
public class {Entity}Store
{
    private readonly ILiteCollection<{Entity}> _collection;

    public {Entity}Store(ILiteDatabase database)
    {
        _collection = database.GetCollection<{Entity}>("{collectionName}");
    }

    public IReadOnlyList<{Entity}> GetAll() => [.. _collection.FindAll()];

    public ({Entity}[] Items, int TotalCount) GetPaged(int page, int pageSize, string sortBy, string sortDirection)
    {
        var totalCount = _collection.Count();
        var items = ApplySort(_collection.FindAll(), sortBy, sortDirection)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, totalCount);
    }

    public ({Entity}[] Items, int TotalCount) SearchPaged(string search, int page, int pageSize, string sortBy, string sortDirection)
    {
        // Filter in-memory using case-insensitive string matching
        // Then apply sort, skip, take
    }

    private static IEnumerable<{Entity}> ApplySort(IEnumerable<{Entity}> items, string sortBy, string sortDirection) { ... }

    public {Entity}? GetById(Guid id) => _collection.FindById(id);
    public {Entity} Add(Create{Entity}Request request) { ... }
    public {Entity}? Update(Guid id, Update{Entity}Request request) { ... }
    public bool Delete(Guid id) => _collection.Delete(id);
}
```

## Registration
- Stores are registered as `AddSingleton<{Entity}Store>()` in `Program.cs`.

## Seed Data
- `SeedDataGenerator` is a static class that generates meaningful test data.
- Current counts: 100 patients, 30 doctors, 200 exams.
- Seeded via the Admin endpoint (`POST /api/admin/seed`).
- Each exam has realistic type-specific durations, results, and notes.

## LiteDbFactory
- Must call `LiteDbFactory.ConfigureMapper()` before creating any `LiteDatabase` instance.
- Pre-warms entity mapper cache to avoid concurrent lazy-init race conditions.

## Testing
- Tests use `LiteDatabase(":memory:")` via `TestWebApplicationFactory`.
