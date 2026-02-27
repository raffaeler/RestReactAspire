using LiteDB;
using RestReactAspire.Server.Models;
using RestReactAspire.Server.Stores;
using RestReactAspire.Server.Telemetry;

namespace RestReactAspire.Server.Endpoints;

public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/seed", Seed);
        group.MapPost("/reset", Reset);
        group.MapGet("/stats", GetStats);

        return group;
    }

    private static IResult Seed(ILiteDatabase database, ILogger<PatientStore> logger)
    {
        using var activity = AdminTelemetry.ActivitySource.StartActivity("SeedDatabase");

        logger.LogInformation("Seeding database with sample data");

        var patients = SeedDataGenerator.GeneratePatients();
        var doctors = SeedDataGenerator.GenerateDoctors();
        var exams = SeedDataGenerator.GenerateExams(patients, doctors);

        var patientCollection = database.GetCollection<Patient>("patients");
        var doctorCollection = database.GetCollection<Doctor>("doctors");
        var examCollection = database.GetCollection<Exam>("exams");

        patientCollection.InsertBulk(patients);
        doctorCollection.InsertBulk(doctors);
        examCollection.InsertBulk(exams);

        AdminTelemetry.DatabaseSeeded.Add(1);

        activity?.SetTag("admin.patients_added", patients.Count);
        activity?.SetTag("admin.doctors_added", doctors.Count);
        activity?.SetTag("admin.exams_added", exams.Count);

        logger.LogInformation("Database seeded with {Patients} patients, {Doctors} doctors, {Exams} exams",
            patients.Count, doctors.Count, exams.Count);

        var response = new SeedResponse(
            patients.Count,
            doctors.Count,
            exams.Count,
            [
                new Link("self", "/api/admin/seed", "POST"),
                new Link("reset", "/api/admin/reset", "POST"),
                new Link("stats", "/api/admin/stats", "GET"),
                new Link("patients", "/api/patients", "GET"),
                new Link("doctors", "/api/doctors", "GET"),
                new Link("exams", "/api/exams", "GET")
            ]);

        return Results.Ok(response);
    }

    private static IResult Reset(ILiteDatabase database, ILogger<PatientStore> logger)
    {
        using var activity = AdminTelemetry.ActivitySource.StartActivity("ResetDatabase");

        logger.LogInformation("Resetting database");

        var patientCollection = database.GetCollection<Patient>("patients");
        var doctorCollection = database.GetCollection<Doctor>("doctors");
        var examCollection = database.GetCollection<Exam>("exams");

        var deletedPatients = patientCollection.DeleteAll();
        var deletedDoctors = doctorCollection.DeleteAll();
        var deletedExams = examCollection.DeleteAll();

        AdminTelemetry.DatabaseReset.Add(1);

        activity?.SetTag("admin.patients_deleted", deletedPatients);
        activity?.SetTag("admin.doctors_deleted", deletedDoctors);
        activity?.SetTag("admin.exams_deleted", deletedExams);

        logger.LogInformation("Database reset: removed {Patients} patients, {Doctors} doctors, {Exams} exams",
            deletedPatients, deletedDoctors, deletedExams);

        var response = new ResetResponse(
            deletedPatients,
            deletedDoctors,
            deletedExams,
            [
                new Link("self", "/api/admin/reset", "POST"),
                new Link("seed", "/api/admin/seed", "POST"),
                new Link("stats", "/api/admin/stats", "GET")
            ]);

        return Results.Ok(response);
    }

    private static IResult GetStats(ILiteDatabase database, ILogger<PatientStore> logger)
    {
        using var activity = AdminTelemetry.ActivitySource.StartActivity("GetDatabaseStats");

        var patientCount = database.GetCollection<Patient>("patients").Count();
        var doctorCount = database.GetCollection<Doctor>("doctors").Count();
        var examCount = database.GetCollection<Exam>("exams").Count();

        logger.LogInformation("Database stats: {Patients} patients, {Doctors} doctors, {Exams} exams",
            patientCount, doctorCount, examCount);

        var response = new StatsResponse(
            patientCount,
            doctorCount,
            examCount,
            [
                new Link("self", "/api/admin/stats", "GET"),
                new Link("seed", "/api/admin/seed", "POST"),
                new Link("reset", "/api/admin/reset", "POST"),
                new Link("patients", "/api/patients", "GET"),
                new Link("doctors", "/api/doctors", "GET"),
                new Link("exams", "/api/exams", "GET")
            ]);

        return Results.Ok(response);
    }
}
