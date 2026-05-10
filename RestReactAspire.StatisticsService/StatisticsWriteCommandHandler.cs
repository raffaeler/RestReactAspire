using System.Text.Json;
using LiteDB;
using RestReactAspire.Infrastructure.Cqrs;
using RestReactAspire.StatisticsService.Data;
using RestReactAspire.StatisticsService.Stores;

namespace RestReactAspire.StatisticsService;

public sealed class StatisticsWriteCommandHandler : IWriteCommandHandler
{
    private readonly ILiteDatabase _db;

    public StatisticsWriteCommandHandler(ILiteDatabase db)
    {
        _db = db;
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
        var patientsCol = _db.GetCollection<Patient>("patients");
        var doctorsCol = _db.GetCollection<Doctor>("doctors");
        var examsCol = _db.GetCollection<Exam>("exams");

        patientsCol.DeleteAll();
        doctorsCol.DeleteAll();
        examsCol.DeleteAll();

        var patientIds = SeedDataGenerator.GeneratePatients();
        var doctorIds = SeedDataGenerator.GenerateDoctors();
        var examIds = SeedDataGenerator.GenerateExams(patientIds, doctorIds);

        var patients = SeedDataGenerator.GeneratePatientEntities(patientIds);
        var doctors = SeedDataGenerator.GenerateDoctorEntities(doctorIds);
        var exams = SeedDataGenerator.GenerateExamEntities(examIds, patientIds, doctorIds);

        patientsCol.InsertBulk(patients);
        doctorsCol.InsertBulk(doctors);
        examsCol.InsertBulk(exams);

        return WriteCommandResult.Success(
            patientsAffected: patients.Count,
            doctorsAffected: doctors.Count,
            examsAffected: exams.Count);
    }

    private WriteCommandResult HandleResetData()
    {
        var patientsCol = _db.GetCollection<Patient>("patients");
        var doctorsCol = _db.GetCollection<Doctor>("doctors");
        var examsCol = _db.GetCollection<Exam>("exams");

        var deletedPatients = patientsCol.DeleteAll();
        var deletedDoctors = doctorsCol.DeleteAll();
        var deletedExams = examsCol.DeleteAll();

        return WriteCommandResult.Success(
            patientsAffected: deletedPatients,
            doctorsAffected: deletedDoctors,
            examsAffected: deletedExams);
    }
}
