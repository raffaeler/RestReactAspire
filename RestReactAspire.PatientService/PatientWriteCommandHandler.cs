using System.Text.Json;
using RestReactAspire.Infrastructure.Cqrs;
using RestReactAspire.PatientService.Data;
using RestReactAspire.PatientService.Models;
using RestReactAspire.PatientService.Stores;

namespace RestReactAspire.PatientService;

public sealed class PatientWriteCommandHandler : IWriteCommandHandler
{
    private readonly PatientStore _patientStore;

    public PatientWriteCommandHandler(PatientStore patientStore)
    {
        _patientStore = patientStore;
    }

    public WriteCommandResult Handle(WriteCommandEnvelope envelope)
    {
        return envelope.CommandType switch
        {
            nameof(CreatePatientCommand) => HandleCreatePatient(Deserialize<CreatePatientCommand>(envelope)),
            nameof(UpdatePatientCommand) => HandleUpdatePatient(Deserialize<UpdatePatientCommand>(envelope)),
            nameof(DeletePatientCommand) => HandleDeletePatient(Deserialize<DeletePatientCommand>(envelope)),
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

    private WriteCommandResult HandleSeedData()
    {
        var patients = SeedDataGenerator.GeneratePatients();
        _patientStore.InsertBulk(patients);

        return WriteCommandResult.Success(patientsAffected: patients.Count);
    }

    private WriteCommandResult HandleResetData()
    {
        var deletedPatients = _patientStore.DeleteAll();

        return WriteCommandResult.Success(patientsAffected: deletedPatients);
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
