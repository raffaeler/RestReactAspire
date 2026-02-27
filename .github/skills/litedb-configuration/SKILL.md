---
name: LiteDB Configuration
description: Configure LiteDB custom type serializers and manage database schema for the embedded NoSQL store.
globs:
  - "RestReactAspire.Server/Stores/LiteDbFactory.cs"
  - "RestReactAspire.Server/Program.cs"
---

# LiteDB Configuration

## Overview
This project uses **LiteDB** as an embedded NoSQL document database to keep the solution simple and self-contained, avoiding schema migrations.

## Connection String
Configured in `Program.cs`:
```csharp
var liteDbConnectionString = builder.Configuration.GetConnectionString("LiteDb")
    ?? "Filename=hospital.db;Connection=shared";
builder.Services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase(liteDbConnectionString));
```

## Custom Type Serializers
LiteDB does not natively support `DateOnly` and `TimeOnly`. Custom serializers are registered in `LiteDbFactory.ConfigureMapper()`:

```csharp
BsonMapper.Global.RegisterType(
    serialize: (DateOnly d) => new BsonValue(d.ToString("O", CultureInfo.InvariantCulture)),
    deserialize: (BsonValue bson) => DateOnly.ParseExact(bson.AsString, "O", CultureInfo.InvariantCulture)
);

BsonMapper.Global.RegisterType(
    serialize: (TimeOnly t) => new BsonValue(t.ToString("O", CultureInfo.InvariantCulture)),
    deserialize: (BsonValue bson) => TimeOnly.ParseExact(bson.AsString, "O", CultureInfo.InvariantCulture)
);
```

## Entity Mapper Pre-warming
To avoid concurrent lazy-init race conditions, entity mappers are pre-warmed:
```csharp
BsonMapper.Global.Entity<Patient>();
BsonMapper.Global.Entity<Doctor>();
BsonMapper.Global.Entity<Exam>();
```

When adding new entity types, add a pre-warm call here.

## Computed Properties
Use `[BsonIgnore]` for properties computed at runtime (e.g., `Exam.EndTime`).

## Testing
Tests use in-memory LiteDB: `new LiteDatabase(":memory:")`.
Always call `LiteDbFactory.ConfigureMapper()` before creating any database instance (including in tests).

## Adding New Types Requiring Custom Serialization
1. Register the serializer in `LiteDbFactory.ConfigureMapper()`.
2. Add the pre-warm call for any new entity type.
3. Ensure `ConfigureMapper()` is called before database creation in both production and test code.
