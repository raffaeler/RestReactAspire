using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RestReactAspire.Server.Telemetry;

public static class DoctorTelemetry
{
    public const string SourceName = "RestReactAspire.Server.Doctors";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> DoctorsCreated = Meter.CreateCounter<long>(
        "hospital.doctors.created", description: "Number of doctors created");

    public static readonly Counter<long> DoctorsUpdated = Meter.CreateCounter<long>(
        "hospital.doctors.updated", description: "Number of doctors updated");

    public static readonly Counter<long> DoctorsDeleted = Meter.CreateCounter<long>(
        "hospital.doctors.deleted", description: "Number of doctors deleted");
}
