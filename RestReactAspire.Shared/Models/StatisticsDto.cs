namespace RestReactAspire.Shared.Models;

public record PatientsByAgeGroupResponse(
    IReadOnlyList<AgeGroupItem> Items,
    IReadOnlyList<Link> Links);

public record AgeGroupItem(string AgeGroup, int Count);

public record ExamsPerDoctorResponse(
    IReadOnlyList<ExamsPerDoctorItem> Items,
    IReadOnlyList<Link> Links);

public record ExamsPerDoctorItem(string DoctorName, string Specialty, int ExamCount);

public record ExamsOverTimeResponse(
    IReadOnlyList<ExamsOverTimeItem> Items,
    IReadOnlyList<Link> Links);

public record ExamsOverTimeItem(string Month, int ExamCount);

public record AvgDurationByExamTypeResponse(
    IReadOnlyList<AvgDurationByExamTypeItem> Items,
    IReadOnlyList<Link> Links);

public record AvgDurationByExamTypeItem(string Month, string ExamType, double AvgDurationMinutes);
