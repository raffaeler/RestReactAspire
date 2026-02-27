---
name: Telemetry and Observability
description: Add or modify OpenTelemetry instrumentation (Traces, Metrics, Logs) for API endpoints.
globs:
  - "RestReactAspire.Server/Telemetry/**"
  - "RestReactAspire.Server/Extensions.cs"
---

# Telemetry and Observability

## Framework
- Uses **OpenTelemetry** for distributed tracing, metrics, and structured logging.
- Configured in `Extensions.cs` via `ConfigureOpenTelemetry()`.

## Telemetry Class Pattern
Each entity/feature has a static telemetry class in `RestReactAspire.Server/Telemetry/`:

```csharp
public static class {Entity}Telemetry
{
    public const string SourceName = "RestReactAspire.Server.{Entity}s";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> {Entity}sQueried = Meter.CreateCounter<long>(
        "hospital.{entity}s.queried", description: "Number of times {entity}s were queried");

    public static readonly Counter<long> {Entity}sCreated = Meter.CreateCounter<long>(
        "hospital.{entity}s.created", description: "Number of {entity}s created");

    // ... additional counters for update, delete, etc.
}
```

## Registration Requirements
When adding a new telemetry class:
1. Add `.AddMeter({Entity}Telemetry.SourceName)` to the metrics configuration in `Extensions.cs`.
2. Add `.AddSource({Entity}Telemetry.SourceName)` to the tracing configuration in `Extensions.cs`.

## Usage in Endpoints
Every endpoint method must:
1. Start an activity: `using var activity = {Entity}Telemetry.ActivitySource.StartActivity("{OperationName}");`
2. Set tags on the activity: `activity?.SetTag("{entity}.id", id.ToString());`
3. Increment metrics: `{Entity}Telemetry.{Entity}sQueried.Add(1);`
4. Log with structured parameters: `logger.LogInformation("Retrieved {Entity} {Id}", id);`
5. On errors, set status: `activity?.SetStatus(ActivityStatusCode.Error, "message");` and log warnings.

## Existing Telemetry Classes
- `PatientTelemetry` — SourceName: `RestReactAspire.Server.Patients`
- `ExamTelemetry` — SourceName: `RestReactAspire.Server.Exams`
- `DoctorTelemetry` — SourceName: `RestReactAspire.Server.Doctors`
- `AdminTelemetry` — SourceName: `RestReactAspire.Server.Admin`
- `RootTelemetry` — SourceName: `RestReactAspire.Server.Root`
- `StatisticsTelemetry` — SourceName: `RestReactAspire.Server.Statistics`
