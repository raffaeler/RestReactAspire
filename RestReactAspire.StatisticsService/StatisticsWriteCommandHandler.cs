using System.Text.Json;
using RestReactAspire.Shared.Cqrs;
using RestReactAspire.Shared.Models;
using RestReactAspire.Shared.Stores;

namespace RestReactAspire.StatisticsService;

public sealed class StatisticsWriteCommandHandler
{
    private readonly PatientStore _patientStore;
    private readonly DoctorStore _doctorStore;
    private readonly ExamStore _examStore;

    public StatisticsWriteCommandHandler(PatientStore patientStore, DoctorStore doctorStore, ExamStore examStore)
    {
        _patientStore = patientStore;
        _doctorStore = doctorStore;
        _examStore = examStore;
    }

    public WriteCommandResult Handle(WriteCommandEnvelope envelope)
    {
        return envelope.CommandType switch
        {
            nameof(SeedDataCommand) => HandleSeedData(),
            nameof(ResetDataCommand) => HandleResetData(),
            _ => WriteCommandResult.Failure("UnknownCommand", $"Unsupported command type {envelope.CommandType}"),
        };
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
}
