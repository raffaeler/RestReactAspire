using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RestReactAspire.Shared.Telemetry;

public static class ExamTelemetry
{
    public const string SourceName = "RestReactAspire.ExamService";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> ExamsQueried = Meter.CreateCounter<long>(
        "hospital.exams.queried", description: "Number of times exams were queried");

    public static readonly Counter<long> ExamsCreated = Meter.CreateCounter<long>(
        "hospital.exams.created", description: "Number of exams created");

    public static readonly Counter<long> ExamsUpdated = Meter.CreateCounter<long>(
        "hospital.exams.updated", description: "Number of exams updated");

    public static readonly Counter<long> ExamsDeleted = Meter.CreateCounter<long>(
        "hospital.exams.deleted", description: "Number of exams deleted");
}
