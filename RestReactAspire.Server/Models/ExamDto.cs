namespace RestReactAspire.Server.Models;

public record CreateExamRequest(
    Guid PatientId,
    Guid? DoctorId,
    string Type,
    DateOnly ScheduledDate,
    string Status,
    string? Results,
    string? Notes);

public record UpdateExamRequest(
    Guid? DoctorId,
    string Type,
    DateOnly ScheduledDate,
    string Status,
    string? Results,
    string? Notes);

public record ExamResponse(
    Guid Id,
    Guid PatientId,
    Guid? DoctorId,
    string Type,
    DateOnly ScheduledDate,
    string Status,
    string? Results,
    string? Notes,
    IReadOnlyList<Link> Links);

public record ExamListResponse(
    IReadOnlyList<ExamResponse> Items,
    PaginationInfo Pagination,
    IReadOnlyList<Link> Links);
