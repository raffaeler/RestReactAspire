using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RestReactAspire.Server.Telemetry;

public static class RootTelemetry
{
    public const string SourceName = "RestReactAspire.Server";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> ApiRootQueried = Meter.CreateCounter<long>(
        "hospital.api_root.queried", description: "Number of times API root was queried");
}
