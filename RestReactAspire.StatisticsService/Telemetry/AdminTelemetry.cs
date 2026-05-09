using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RestReactAspire.StatisticsService.Telemetry;

public static class AdminTelemetry
{
    public const string SourceName = "RestReactAspire.Server.Admin";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> SeedExecuted = Meter.CreateCounter<long>(
        "hospital.admin.seed_executed", description: "Number of times seed data was executed");

    public static readonly Counter<long> ResetExecuted = Meter.CreateCounter<long>(
        "hospital.admin.reset_executed", description: "Number of times data reset was executed");

    public static readonly Counter<long> StatsQueried = Meter.CreateCounter<long>(
        "hospital.admin.stats_queried", description: "Number of times stats were queried");
}
