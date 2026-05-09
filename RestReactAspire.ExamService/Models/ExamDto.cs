namespace RestReactAspire.ExamService.Models;

public record CreateExamRequest(
    Guid PatientId,
    Guid? DoctorId,
    string Type,
    DateOnly ScheduledDate,
    TimeOnly? ScheduledTime,
    int? DurationMinutes,
    string Status,
    string? Results,
    string? Notes);

public record UpdateExamRequest(
    Guid? DoctorId,
    string Type,
    DateOnly ScheduledDate,
    TimeOnly? ScheduledTime,
    int? DurationMinutes,
    string Status,
    string? Results,
    string? Notes);

public record ExamResponse(
    Guid Id,
    Guid PatientId,
    Guid? DoctorId,
    string Type,
    DateOnly ScheduledDate,
    TimeOnly? ScheduledTime,
    int? DurationMinutes,
    TimeOnly? EndTime,
    string Status,
    string? Results,
    string? Notes,
    IReadOnlyList<Link> Links);

public record ExamListResponse(
    IReadOnlyList<ExamResponse> Items,
    PaginationInfo Pagination,
    SortInfo Sort,
    IReadOnlyList<Link> Links);

public record AssignDoctorRequest(Guid? DoctorId);

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
