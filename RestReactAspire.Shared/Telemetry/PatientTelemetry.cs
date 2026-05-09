using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RestReactAspire.Shared.Telemetry;

public static class PatientTelemetry
{
    public const string SourceName = "RestReactAspire.PatientService";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> PatientsQueried = Meter.CreateCounter<long>(
        "hospital.patients.queried", description: "Number of times patients were queried");

    public static readonly Counter<long> PatientsCreated = Meter.CreateCounter<long>(
        "hospital.patients.created", description: "Number of patients created");

    public static readonly Counter<long> PatientsUpdated = Meter.CreateCounter<long>(
        "hospital.patients.updated", description: "Number of patients updated");

    public static readonly Counter<long> PatientsDeleted = Meter.CreateCounter<long>(
        "hospital.patients.deleted", description: "Number of patients deleted");
}
