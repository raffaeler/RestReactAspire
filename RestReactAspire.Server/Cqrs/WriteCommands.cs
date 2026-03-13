using System.Text.Json;

namespace RestReactAspire.Server.Cqrs;

public sealed record WriteCommandEnvelope(Guid CommandId, string CommandType, JsonElement Payload)
{
    public static WriteCommandEnvelope Create<TCommand>(Guid commandId, TCommand command)
        where TCommand : class =>
        new(commandId, typeof(TCommand).Name, JsonSerializer.SerializeToElement(command));
}

public sealed record CreatePatientCommand(
    Guid PatientId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Email,
    string Phone);

public sealed record UpdatePatientCommand(
    Guid PatientId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Email,
    string Phone);

public sealed record DeletePatientCommand(Guid PatientId);

public sealed record CreateDoctorCommand(
    Guid DoctorId,
    string FirstName,
    string LastName,
    string Specialty,
    string Email,
    string Phone);

public sealed record UpdateDoctorCommand(
    Guid DoctorId,
    string FirstName,
    string LastName,
    string Specialty,
    string Email,
    string Phone);

public sealed record DeleteDoctorCommand(Guid DoctorId);

public sealed record CreateExamCommand(
    Guid ExamId,
    Guid PatientId,
    Guid? DoctorId,
    string Type,
    DateOnly ScheduledDate,
    TimeOnly? ScheduledTime,
    int? DurationMinutes,
    string Status,
    string? Results,
    string? Notes);

public sealed record UpdateExamCommand(
    Guid ExamId,
    Guid? DoctorId,
    string Type,
    DateOnly ScheduledDate,
    TimeOnly? ScheduledTime,
    int? DurationMinutes,
    string Status,
    string? Results,
    string? Notes);

public sealed record AssignDoctorToExamCommand(Guid ExamId, Guid? DoctorId);

public sealed record DeleteExamCommand(Guid ExamId);

public sealed record SeedDataCommand();

public sealed record ResetDataCommand();

public sealed record WriteCommandResult(
    bool Succeeded,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    Guid? ResourceId = null,
    int PatientsAffected = 0,
    int DoctorsAffected = 0,
    int ExamsAffected = 0)
{
    public static WriteCommandResult Success(
        Guid? resourceId = null,
        int patientsAffected = 0,
        int doctorsAffected = 0,
        int examsAffected = 0) =>
        new(true, ResourceId: resourceId, PatientsAffected: patientsAffected, DoctorsAffected: doctorsAffected, ExamsAffected: examsAffected);

    public static WriteCommandResult Failure(string errorCode, string errorMessage) =>
        new(false, errorCode, errorMessage);
}
