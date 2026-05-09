using System.Diagnostics;
using LiteDB;
using RestReactAspire.Shared.Cqrs;
using RestReactAspire.Shared.Models;
using RestReactAspire.Shared.Stores;
using RestReactAspire.Shared.Telemetry;

namespace RestReactAspire.StatisticsService;

public static class StatisticsEndpoints
{
    public static RouteGroupBuilder MapStatisticsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/patients-by-age-group", GetPatientsByAgeGroup);
        group.MapGet("/exams-per-doctor", GetExamsPerDoctor);
        group.MapGet("/exams-over-time", GetExamsOverTime);
        group.MapGet("/avg-duration-by-exam-type", GetAvgDurationByExamType);

        return group;
    }

    public static RouteGroupBuilder MapStatisticsAdminEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/seed", Seed);
        group.MapPost("/reset", Reset);
        group.MapGet("/stats", GetStats);

        return group;
    }

    private static IReadOnlyList<Link> GetStatisticsLinks() =>
    [
        new Link("patients-by-age-group", "/api/statistics/patients-by-age-group", "GET"),
        new Link("exams-per-doctor", "/api/statistics/exams-per-doctor", "GET"),
        new Link("exams-over-time", "/api/statistics/exams-over-time", "GET"),
        new Link("avg-duration-by-exam-type", "/api/statistics/avg-duration-by-exam-type", "GET"),
        new Link("patients", "/api/patients", "GET"),
        new Link("doctors", "/api/doctors", "GET"),
        new Link("exams", "/api/exams", "GET"),
    ];

    private static IReadOnlyList<Link> GetAdminLinks() =>
    [
        new Link("self", "/api/admin/stats", "GET"),
        new Link("seed", "/api/admin/seed", "POST"),
        new Link("reset", "/api/admin/reset", "POST"),
        new Link("patients", "/api/patients", "GET"),
        new Link("doctors", "/api/doctors", "GET"),
        new Link("exams", "/api/exams", "GET"),
    ];

    private static IResult GetPatientsByAgeGroup(PatientStore patientStore, ILogger<Program> logger)
    {
        using var activity = StatisticsTelemetry.ActivitySource.StartActivity("GetPatientsByAgeGroup");

        logger.LogInformation("Retrieving patients by age group statistics");

        var patients = patientStore.GetAll();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var ageGroups = patients
            .Select(p =>
            {
                var age = today.Year - p.DateOfBirth.Year;
                if (p.DateOfBirth > today.AddYears(-age)) age--;
                return age;
            })
            .GroupBy(age => age switch
            {
                < 20 => "0-19",
                < 30 => "20-29",
                < 40 => "30-39",
                < 50 => "40-49",
                < 60 => "50-59",
                < 70 => "60-69",
                < 80 => "70-79",
                _ => "80+",
            })
            .Select(g => new AgeGroupItem(g.Key, g.Count()))
            .OrderBy(g => g.AgeGroup)
            .ToList();

        StatisticsTelemetry.PatientsByAgeGroupQueried.Add(1);
        activity?.SetTag("statistics.age_groups_count", ageGroups.Count);

        logger.LogInformation("Returned {Count} age groups", ageGroups.Count);

        return Results.Ok(new PatientsByAgeGroupResponse(ageGroups, GetStatisticsLinks()));
    }

    private static IResult GetExamsPerDoctor(ExamStore examStore, DoctorStore doctorStore, ILogger<Program> logger)
    {
        using var activity = StatisticsTelemetry.ActivitySource.StartActivity("GetExamsPerDoctor");

        logger.LogInformation("Retrieving exams per doctor statistics");

        var exams = examStore.GetAll();
        var doctors = doctorStore.GetAll().ToDictionary(d => d.Id);

        var examsPerDoctor = exams
            .Where(e => e.DoctorId.HasValue && doctors.ContainsKey(e.DoctorId.Value))
            .GroupBy(e => e.DoctorId!.Value)
            .Select(g =>
            {
                var doctor = doctors[g.Key];
                return new ExamsPerDoctorItem(
                    $"{doctor.FirstName} {doctor.LastName}",
                    doctor.Specialty,
                    g.Count());
            })
            .OrderByDescending(x => x.ExamCount)
            .ToList();

        StatisticsTelemetry.ExamsPerDoctorQueried.Add(1);
        activity?.SetTag("statistics.doctors_count", examsPerDoctor.Count);

        logger.LogInformation("Returned exams per doctor for {Count} doctors", examsPerDoctor.Count);

        return Results.Ok(new ExamsPerDoctorResponse(examsPerDoctor, GetStatisticsLinks()));
    }

    private static IResult GetExamsOverTime(ExamStore examStore, ILogger<Program> logger)
    {
        using var activity = StatisticsTelemetry.ActivitySource.StartActivity("GetExamsOverTime");

        logger.LogInformation("Retrieving exams over time statistics");

        var exams = examStore.GetAll();

        var examsOverTime = exams
            .GroupBy(e => new { e.ScheduledDate.Year, e.ScheduledDate.Month })
            .Select(g => new ExamsOverTimeItem(
                $"{g.Key.Year}-{g.Key.Month:D2}",
                g.Count()))
            .OrderBy(x => x.Month)
            .ToList();

        StatisticsTelemetry.ExamsOverTimeQueried.Add(1);
        activity?.SetTag("statistics.months_count", examsOverTime.Count);

        logger.LogInformation("Returned exams over time for {Count} months", examsOverTime.Count);

        return Results.Ok(new ExamsOverTimeResponse(examsOverTime, GetStatisticsLinks()));
    }

    private static IResult GetAvgDurationByExamType(ExamStore examStore, ILogger<Program> logger)
    {
        using var activity = StatisticsTelemetry.ActivitySource.StartActivity("GetAvgDurationByExamType");

        logger.LogInformation("Retrieving average duration by exam type statistics");

        var exams = examStore.GetAll();

        var avgDuration = exams
            .Where(e => e.DurationMinutes.HasValue)
            .GroupBy(e => new { e.Type, e.ScheduledDate.Year, e.ScheduledDate.Month })
            .Select(g => new AvgDurationByExamTypeItem(
                $"{g.Key.Year}-{g.Key.Month:D2}",
                g.Key.Type,
                Math.Round(g.Average(e => e.DurationMinutes!.Value), 1)))
            .OrderBy(x => x.Month)
            .ThenBy(x => x.ExamType)
            .ToList();

        StatisticsTelemetry.AvgDurationByExamTypeQueried.Add(1);
        activity?.SetTag("statistics.data_points", avgDuration.Count);

        logger.LogInformation("Returned average duration data with {Count} data points", avgDuration.Count);

        return Results.Ok(new AvgDurationByExamTypeResponse(avgDuration, GetStatisticsLinks()));
    }

    private static async Task<IResult> Seed(
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        ILogger<Program> logger,
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

        AdminTelemetry.SeedExecuted.Add(1);

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
        ILogger<Program> logger,
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

        AdminTelemetry.ResetExecuted.Add(1);

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

    private static IResult GetStats(ILiteDatabase database, ILogger<Program> logger)
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
            GetAdminLinks());

        return Results.Ok(response);
    }
}
