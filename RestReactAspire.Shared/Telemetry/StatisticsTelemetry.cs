using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RestReactAspire.Shared.Telemetry;

public static class StatisticsTelemetry
{
    public const string SourceName = "RestReactAspire.StatisticsService";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> PatientsByAgeGroupQueried = Meter.CreateCounter<long>(
        "hospital.statistics.patients_by_age_group_queried", description: "Number of times patients by age group chart was queried");

    public static readonly Counter<long> ExamsPerDoctorQueried = Meter.CreateCounter<long>(
        "hospital.statistics.exams_per_doctor_queried", description: "Number of times exams per doctor chart was queried");

    public static readonly Counter<long> ExamsOverTimeQueried = Meter.CreateCounter<long>(
        "hospital.statistics.exams_over_time_queried", description: "Number of times exams over time chart was queried");

    public static readonly Counter<long> AvgDurationByExamTypeQueried = Meter.CreateCounter<long>(
        "hospital.statistics.avg_duration_by_exam_type_queried", description: "Number of times average duration by exam type chart was queried");
}
