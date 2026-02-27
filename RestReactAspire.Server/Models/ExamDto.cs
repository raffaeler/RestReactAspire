namespace RestReactAspire.Server.Models;

public record CreateExamRequest(
    Guid PatientId,
    string Type,
    DateOnly ScheduledDate,
    string Status,
    string? Results,
    string? Notes);

public record UpdateExamRequest(
    string Type,
    DateOnly ScheduledDate,
    string Status,
    string? Results,
    string? Notes);

public record ExamResponse(
    Guid Id,
    Guid PatientId,
    string Type,
    DateOnly ScheduledDate,
    string Status,
    string? Results,
    string? Notes,
    IReadOnlyList<Link> Links);

public record ExamListResponse(
    IReadOnlyList<ExamResponse> Items,
    IReadOnlyList<Link> Links);
