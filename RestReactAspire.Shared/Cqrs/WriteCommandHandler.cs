using System.Text.Json;
using RestReactAspire.Shared.Models;
using RestReactAspire.Shared.Stores;

namespace RestReactAspire.Shared.Cqrs;

public sealed class WriteCommandHandler
{
    private readonly PatientStore _patientStore;
    private readonly DoctorStore _doctorStore;
    private readonly ExamStore _examStore;

    public WriteCommandHandler(PatientStore patientStore, DoctorStore doctorStore, ExamStore examStore)
    {
        _patientStore = patientStore;
        _doctorStore = doctorStore;
        _examStore = examStore;
    }

    public WriteCommandResult Handle(WriteCommandEnvelope envelope)
    {
        return envelope.CommandType switch
        {
            nameof(CreatePatientCommand) => HandleCreatePatient(Deserialize<CreatePatientCommand>(envelope)),
            nameof(UpdatePatientCommand) => HandleUpdatePatient(Deserialize<UpdatePatientCommand>(envelope)),
            nameof(DeletePatientCommand) => HandleDeletePatient(Deserialize<DeletePatientCommand>(envelope)),
            nameof(CreateDoctorCommand) => HandleCreateDoctor(Deserialize<CreateDoctorCommand>(envelope)),
            nameof(UpdateDoctorCommand) => HandleUpdateDoctor(Deserialize<UpdateDoctorCommand>(envelope)),
            nameof(DeleteDoctorCommand) => HandleDeleteDoctor(Deserialize<DeleteDoctorCommand>(envelope)),
            nameof(CreateExamCommand) => HandleCreateExam(Deserialize<CreateExamCommand>(envelope)),
            nameof(UpdateExamCommand) => HandleUpdateExam(Deserialize<UpdateExamCommand>(envelope)),
            nameof(DeleteExamCommand) => HandleDeleteExam(Deserialize<DeleteExamCommand>(envelope)),
            nameof(AssignDoctorToExamCommand) => HandleAssignDoctor(Deserialize<AssignDoctorToExamCommand>(envelope)),
            nameof(SeedDataCommand) => HandleSeedData(),
            nameof(ResetDataCommand) => HandleResetData(),
            _ => WriteCommandResult.Failure("UnknownCommand", $"Unsupported command type {envelope.CommandType}"),
        };
    }

    private WriteCommandResult HandleCreatePatient(CreatePatientCommand command)
    {
        _patientStore.Add(new Patient
        {
            Id = command.PatientId,
            FirstName = command.FirstName,
            LastName = command.LastName,
            DateOfBirth = command.DateOfBirth,
            Email = command.Email,
            Phone = command.Phone,
        });

        return WriteCommandResult.Success(resourceId: command.PatientId);
    }

    private WriteCommandResult HandleUpdatePatient(UpdatePatientCommand command)
    {
        var updated = _patientStore.Update(command.PatientId, new UpdatePatientRequest(
            command.FirstName,
            command.LastName,
            command.DateOfBirth,
            command.Email,
            command.Phone));

        return updated is null
            ? WriteCommandResult.Failure("PatientNotFound", $"Patient {command.PatientId} not found")
            : WriteCommandResult.Success(resourceId: command.PatientId);
    }

    private WriteCommandResult HandleDeletePatient(DeletePatientCommand command)
    {
        return _patientStore.Delete(command.PatientId)
            ? WriteCommandResult.Success(resourceId: command.PatientId)
            : WriteCommandResult.Failure("PatientNotFound", $"Patient {command.PatientId} not found");
    }

    private WriteCommandResult HandleCreateDoctor(CreateDoctorCommand command)
    {
        _doctorStore.Add(new Doctor
        {
            Id = command.DoctorId,
            FirstName = command.FirstName,
            LastName = command.LastName,
            Specialty = command.Specialty,
            Email = command.Email,
            Phone = command.Phone,
        });

        return WriteCommandResult.Success(resourceId: command.DoctorId);
    }

    private WriteCommandResult HandleUpdateDoctor(UpdateDoctorCommand command)
    {
        var updated = _doctorStore.Update(command.DoctorId, new UpdateDoctorRequest(
            command.FirstName,
            command.LastName,
            command.Specialty,
            command.Email,
            command.Phone));

        return updated is null
            ? WriteCommandResult.Failure("DoctorNotFound", $"Doctor {command.DoctorId} not found")
            : WriteCommandResult.Success(resourceId: command.DoctorId);
    }

    private WriteCommandResult HandleDeleteDoctor(DeleteDoctorCommand command)
    {
        return _doctorStore.Delete(command.DoctorId)
            ? WriteCommandResult.Success(resourceId: command.DoctorId)
            : WriteCommandResult.Failure("DoctorNotFound", $"Doctor {command.DoctorId} not found");
    }

    private WriteCommandResult HandleCreateExam(CreateExamCommand command)
    {
        if (_patientStore.GetById(command.PatientId) is null)
        {
            return WriteCommandResult.Failure("PatientNotFound", $"Patient {command.PatientId} not found");
        }

        if (command.DoctorId.HasValue && _doctorStore.GetById(command.DoctorId.Value) is null)
        {
            return WriteCommandResult.Failure("DoctorNotFound", $"Doctor {command.DoctorId} not found");
        }

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
        if (command.DoctorId.HasValue && _doctorStore.GetById(command.DoctorId.Value) is null)
        {
            return WriteCommandResult.Failure("DoctorNotFound", $"Doctor {command.DoctorId} not found");
        }

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
        if (command.DoctorId.HasValue && _doctorStore.GetById(command.DoctorId.Value) is null)
        {
            return WriteCommandResult.Failure("DoctorNotFound", $"Doctor {command.DoctorId} not found");
        }

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
        var patients = SeedDataGenerator.GeneratePatients();
        var doctors = SeedDataGenerator.GenerateDoctors();
        var exams = SeedDataGenerator.GenerateExams(patients, doctors);

        _patientStore.InsertBulk(patients);
        _doctorStore.InsertBulk(doctors);
        _examStore.InsertBulk(exams);

        return WriteCommandResult.Success(
            patientsAffected: patients.Count,
            doctorsAffected: doctors.Count,
            examsAffected: exams.Count);
    }

    private WriteCommandResult HandleResetData()
    {
        var deletedPatients = _patientStore.DeleteAll();
        var deletedDoctors = _doctorStore.DeleteAll();
        var deletedExams = _examStore.DeleteAll();

        return WriteCommandResult.Success(
            patientsAffected: deletedPatients,
            doctorsAffected: deletedDoctors,
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
