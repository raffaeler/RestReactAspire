using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RestReactAspire.Server.Telemetry;

public static class PatientTelemetry
{
    public const string SourceName = "RestReactAspire.Server.Patients";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> PatientsCreated = Meter.CreateCounter<long>(
        "hospital.patients.created", description: "Number of patients created");

    public static readonly Counter<long> PatientsUpdated = Meter.CreateCounter<long>(
        "hospital.patients.updated", description: "Number of patients updated");

    public static readonly Counter<long> PatientsDeleted = Meter.CreateCounter<long>(
        "hospital.patients.deleted", description: "Number of patients deleted");
}
