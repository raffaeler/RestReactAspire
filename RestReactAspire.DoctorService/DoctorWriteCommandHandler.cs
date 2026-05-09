using System.Text.Json;
using RestReactAspire.Infrastructure.Cqrs;
using RestReactAspire.DoctorService.Data;
using RestReactAspire.DoctorService.Models;
using RestReactAspire.DoctorService.Stores;

namespace RestReactAspire.DoctorService;

public sealed class DoctorWriteCommandHandler : IWriteCommandHandler
{
    private readonly DoctorStore _doctorStore;

    public DoctorWriteCommandHandler(DoctorStore doctorStore)
    {
        _doctorStore = doctorStore;
    }

    public WriteCommandResult Handle(WriteCommandEnvelope envelope)
    {
        return envelope.CommandType switch
        {
            nameof(CreateDoctorCommand) => HandleCreateDoctor(Deserialize<CreateDoctorCommand>(envelope)),
            nameof(UpdateDoctorCommand) => HandleUpdateDoctor(Deserialize<UpdateDoctorCommand>(envelope)),
            nameof(DeleteDoctorCommand) => HandleDeleteDoctor(Deserialize<DeleteDoctorCommand>(envelope)),
            nameof(SeedDataCommand) => HandleSeedData(),
            nameof(ResetDataCommand) => HandleResetData(),
            _ => WriteCommandResult.Failure("UnknownCommand", $"Unsupported command type {envelope.CommandType}"),
        };
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

    private WriteCommandResult HandleSeedData()
    {
        var doctors = SeedDataGenerator.GenerateDoctors();
        _doctorStore.InsertBulk(doctors);

        return WriteCommandResult.Success(doctorsAffected: doctors.Count);
    }

    private WriteCommandResult HandleResetData()
    {
        var deletedDoctors = _doctorStore.DeleteAll();

        return WriteCommandResult.Success(doctorsAffected: deletedDoctors);
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
