namespace RestReactAspire.Server.Models;

public record SeedResponse(
    int PatientsCreated,
    int DoctorsCreated,
    int ExamsCreated,
    IReadOnlyList<Link> Links);

public record ResetResponse(
    int PatientsDeleted,
    int DoctorsDeleted,
    int ExamsDeleted,
    IReadOnlyList<Link> Links);

public record StatsResponse(
    int PatientCount,
    int DoctorCount,
    int ExamCount,
    IReadOnlyList<Link> Links);
