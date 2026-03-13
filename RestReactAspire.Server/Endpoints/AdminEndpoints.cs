using System.Diagnostics;
using LiteDB;
using RestReactAspire.Server.Cqrs;
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

    private static async Task<IResult> Seed(
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        ILogger<PatientStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = AdminTelemetry.ActivitySource.StartActivity("SeedDatabase");

        logger.LogInformation("Seeding database with sample data");

        var commandId = Guid.NewGuid();
        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, new SeedDataCommand()), cancellationToken);
        var result = await resultCoordinator.WaitAsync(commandId, cancellationToken);
        if (!result.Succeeded)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
            logger.LogWarning("Seed command failed: {ErrorCode} {ErrorMessage}", result.ErrorCode, result.ErrorMessage);
            return Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var patientsCreated = result.PatientsAffected;
        var doctorsCreated = result.DoctorsAffected;
        var examsCreated = result.ExamsAffected;

        AdminTelemetry.DatabaseSeeded.Add(1);

        activity?.SetTag("admin.patients_added", patientsCreated);
        activity?.SetTag("admin.doctors_added", doctorsCreated);
        activity?.SetTag("admin.exams_added", examsCreated);

        logger.LogInformation("Database seeded with {Patients} patients, {Doctors} doctors, {Exams} exams",
            patientsCreated, doctorsCreated, examsCreated);

        var response = new SeedResponse(
            patientsCreated,
            doctorsCreated,
            examsCreated,
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

    private static async Task<IResult> Reset(
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        ILogger<PatientStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = AdminTelemetry.ActivitySource.StartActivity("ResetDatabase");

        logger.LogInformation("Resetting database");

        var commandId = Guid.NewGuid();
        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, new ResetDataCommand()), cancellationToken);
        var result = await resultCoordinator.WaitAsync(commandId, cancellationToken);
        if (!result.Succeeded)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
            logger.LogWarning("Reset command failed: {ErrorCode} {ErrorMessage}", result.ErrorCode, result.ErrorMessage);
            return Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var deletedPatients = result.PatientsAffected;
        var deletedDoctors = result.DoctorsAffected;
        var deletedExams = result.ExamsAffected;

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

        AdminTelemetry.StatsQueried.Add(1);

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
