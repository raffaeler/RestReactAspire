using System.Text.Json;
using RestReactAspire.ExamService.Data;
using RestReactAspire.ExamService.Models;
using RestReactAspire.ExamService.Stores;
using RestReactAspire.Infrastructure.Cqrs;

namespace RestReactAspire.ExamService;

public sealed class ExamWriteCommandHandler : IWriteCommandHandler
{
    private readonly ExamStore _examStore;

    public ExamWriteCommandHandler(ExamStore examStore)
    {
        _examStore = examStore;
    }

    public WriteCommandResult Handle(WriteCommandEnvelope envelope)
    {
        return envelope.CommandType switch
        {
            nameof(CreateExamCommand) => HandleCreateExam(Deserialize<CreateExamCommand>(envelope)),
            nameof(UpdateExamCommand) => HandleUpdateExam(Deserialize<UpdateExamCommand>(envelope)),
            nameof(DeleteExamCommand) => HandleDeleteExam(Deserialize<DeleteExamCommand>(envelope)),
            nameof(AssignDoctorToExamCommand) => HandleAssignDoctor(Deserialize<AssignDoctorToExamCommand>(envelope)),
            nameof(SeedDataCommand) => HandleSeedData(),
            nameof(ResetDataCommand) => HandleResetData(),
            _ => WriteCommandResult.Failure("UnknownCommand", $"Unsupported command type {envelope.CommandType}"),
        };
    }

    private WriteCommandResult HandleCreateExam(CreateExamCommand command)
    {
        // No patient/doctor validation — the gateway handles cross-service coordination.
        _examStore.Add(new Exam
        {
            Id = command.ExamId,
            PatientId = command.PatientId,
            DoctorId = command.DoctorId,
            Type = command.Type,
            ScheduledDate = command.ScheduledDate,
            ScheduledTime = command.ScheduledTime,
            DurationMinutes = command.DurationMinutes,
            Status = command.Status,
            Results = command.Results,
            Notes = command.Notes,
        });

        return WriteCommandResult.Success(resourceId: command.ExamId);
    }

    private WriteCommandResult HandleUpdateExam(UpdateExamCommand command)
    {
        var updated = _examStore.Update(command.ExamId, new UpdateExamRequest(
            command.DoctorId,
            command.Type,
            command.ScheduledDate,
            command.ScheduledTime,
            command.DurationMinutes,
            command.Status,
            command.Results,
            command.Notes));

        return updated is null
            ? WriteCommandResult.Failure("ExamNotFound", $"Exam {command.ExamId} not found")
            : WriteCommandResult.Success(resourceId: command.ExamId);
    }

    private WriteCommandResult HandleAssignDoctor(AssignDoctorToExamCommand command)
    {
        var updated = _examStore.AssignDoctor(command.ExamId, command.DoctorId);
        return updated is null
            ? WriteCommandResult.Failure("ExamNotFound", $"Exam {command.ExamId} not found")
            : WriteCommandResult.Success(resourceId: command.ExamId);
    }

    private WriteCommandResult HandleDeleteExam(DeleteExamCommand command)
    {
        return _examStore.Delete(command.ExamId)
            ? WriteCommandResult.Success(resourceId: command.ExamId)
            : WriteCommandResult.Failure("ExamNotFound", $"Exam {command.ExamId} not found");
    }

    private WriteCommandResult HandleSeedData()
    {
        // Use SeedDataGenerator for deterministic IDs matching PatientService/DoctorService seeding
        var patientIds = SeedDataGenerator.GeneratePatientIds();
        var doctorIds = SeedDataGenerator.GenerateDoctorIds();
        var exams = SeedDataGenerator.GenerateExams(patientIds, doctorIds);
        _examStore.InsertBulk(exams);

        return WriteCommandResult.Success(
            patientsAffected: 0,
            doctorsAffected: 0,
            examsAffected: exams.Count);
    }

    private WriteCommandResult HandleResetData()
    {
        var deletedExams = _examStore.DeleteAll();
        return WriteCommandResult.Success(
            patientsAffected: 0,
            doctorsAffected: 0,
            examsAffected: deletedExams);
    }

    private static TCommand Deserialize<TCommand>(WriteCommandEnvelope envelope)
    {
        var command = JsonSerializer.Deserialize<TCommand>(envelope.Payload.GetRawText());
        if (command is null)
        {
            throw new InvalidOperationException($"Unable to deserialize command payload for {typeof(TCommand).Name}");
        }

        return command;
    }
}
