using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RestReactAspire.Server.Telemetry;

public static class RootTelemetry
{
    public const string SourceName = "RestReactAspire.Server.Root";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> RootRequested = Meter.CreateCounter<long>(
        "hospital.root.requested", description: "Number of times the API root was requested");
}
