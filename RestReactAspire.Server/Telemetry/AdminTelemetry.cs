using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RestReactAspire.Server.Telemetry;

public static class AdminTelemetry
{
    public const string SourceName = "RestReactAspire.Server.Admin";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> StatsQueried = Meter.CreateCounter<long>(
        "hospital.admin.stats_queried", description: "Number of times database stats were queried");

    public static readonly Counter<long> DatabaseSeeded = Meter.CreateCounter<long>(
        "hospital.admin.database_seeded", description: "Number of times the database was seeded");

    public static readonly Counter<long> DatabaseReset = Meter.CreateCounter<long>(
        "hospital.admin.database_reset", description: "Number of times the database was reset");
}
