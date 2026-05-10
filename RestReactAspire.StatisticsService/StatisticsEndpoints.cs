using System.Diagnostics;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using RestReactAspire.Infrastructure.Cqrs;
using RestReactAspire.StatisticsService.Data;
using RestReactAspire.StatisticsService.Models;
using RestReactAspire.StatisticsService.Stores;
using RestReactAspire.StatisticsService.Telemetry;

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

    // Dual-mode: uses StatisticsStore when available (testing), HTTP otherwise (production)
    private static async Task<IResult> GetPatientsByAgeGroup(
        [FromServices] StatisticsStore? store, [FromServices] IHttpClientFactory? httpFactory, [FromServices] ILogger<Program> logger)
    {
        using var activity = StatisticsTelemetry.ActivitySource.StartActivity("GetPatientsByAgeGroup");
        logger.LogInformation("Retrieving patients by age group statistics");

        DateOnly[]? datesOfBirth;
        if (store is not null)
        {
            datesOfBirth = store.GetAllPatients().Select(p => p.DateOfBirth).ToArray();
        }
        else if (httpFactory is not null)
        {
            var patientsClient = httpFactory.CreateClient("patients");
            var patients = await patientsClient.GetFromJsonAsync<List<PatientSummary>>("/api/patients?page=1&pageSize=10000");
            if (patients is null)
                return Results.Problem("Failed to retrieve patient data from PatientService", statusCode: StatusCodes.Status502BadGateway);
            datesOfBirth = patients.Select(p => p.DateOfBirth).ToArray();
        }
        else
        {
            return Results.Problem("No data source available", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var ageGroups = datesOfBirth
            .Select(dob =>
            {
                var age = today.Year - dob.Year;
                if (dob > today.AddYears(-age)) age--;
                return age;
            })
            .GroupBy(age => age switch
            {
                < 20 => "0-19", < 30 => "20-29", < 40 => "30-39", < 50 => "40-49",
                < 60 => "50-59", < 70 => "60-69", < 80 => "70-79", _ => "80+",
            })
            .Select(g => new AgeGroupItem(g.Key, g.Count()))
            .OrderBy(g => g.AgeGroup).ToList();

        StatisticsTelemetry.PatientsByAgeGroupQueried.Add(1);
        activity?.SetTag("statistics.age_groups_count", ageGroups.Count);
        logger.LogInformation("Returned {Count} age groups", ageGroups.Count);
        return Results.Ok(new PatientsByAgeGroupResponse(ageGroups, GetStatisticsLinks()));
    }

    private static async Task<IResult> GetExamsPerDoctor(
        [FromServices] StatisticsStore? store, [FromServices] IHttpClientFactory? httpFactory, [FromServices] ILogger<Program> logger)
    {
        using var activity = StatisticsTelemetry.ActivitySource.StartActivity("GetExamsPerDoctor");
        logger.LogInformation("Retrieving exams per doctor statistics");

        List<(Guid? DoctorId, string DoctorFirstName, string DoctorLastName, string DoctorSpecialty)> data;
        if (store is not null)
        {
            List<Exam> exams = store.GetAllExams();
            var doctors = store.GetAllDoctors().ToDictionary(d => d.Id);
            data = exams
                .Where(e => e.DoctorId.HasValue && doctors.ContainsKey(e.DoctorId.Value))
                .Select(e => ((Guid?)e.DoctorId, doctors[e.DoctorId!.Value].FirstName, doctors[e.DoctorId!.Value].LastName, doctors[e.DoctorId!.Value].Specialty))
                .ToList();
        }
        else if (httpFactory is not null)
        {
            var examsClient = httpFactory.CreateClient("exams");
            var doctorsClient = httpFactory.CreateClient("doctors");
            var exams = await examsClient.GetFromJsonAsync<List<ExamSummary>>("/api/exams?page=1&pageSize=10000");
            var doctors = await doctorsClient.GetFromJsonAsync<List<DoctorSummary>>("/api/doctors?page=1&pageSize=10000");
            if (exams is null || doctors is null)
                return Results.Problem("Failed to retrieve data from services", statusCode: StatusCodes.Status502BadGateway);
            var doctorDict = doctors.ToDictionary(d => d.Id);
            data = exams
                .Where(e => e.DoctorId.HasValue && doctorDict.ContainsKey(e.DoctorId.Value))
                .Select(e => (e.DoctorId, doctorDict[e.DoctorId!.Value].FirstName, doctorDict[e.DoctorId!.Value].LastName, doctorDict[e.DoctorId!.Value].Specialty))
                .ToList();
        }
        else
        {
            return Results.Problem("No data source available", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var examsPerDoctor = data
            .GroupBy(x => x.DoctorId!.Value)
            .Select(g =>
            {
                var first = g.First();
                return new ExamsPerDoctorItem($"{first.DoctorFirstName} {first.DoctorLastName}", first.DoctorSpecialty ?? "", g.Count());
            })
            .OrderByDescending(x => x.ExamCount).ToList();

        StatisticsTelemetry.ExamsPerDoctorQueried.Add(1);
        activity?.SetTag("statistics.doctors_count", examsPerDoctor.Count);
        logger.LogInformation("Returned exams per doctor for {Count} doctors", examsPerDoctor.Count);
        return Results.Ok(new ExamsPerDoctorResponse(examsPerDoctor, GetStatisticsLinks()));
    }

    private static async Task<IResult> GetExamsOverTime(
        [FromServices] StatisticsStore? store, [FromServices] IHttpClientFactory? httpFactory, [FromServices] ILogger<Program> logger)
    {
        using var activity = StatisticsTelemetry.ActivitySource.StartActivity("GetExamsOverTime");
        logger.LogInformation("Retrieving exams over time statistics");

        List<(DateOnly ScheduledDate, int Count)> source;
        if (store is not null)
        {
            source = store.GetAllExams().Select(e => (e.ScheduledDate, 1)).ToList();
        }
        else if (httpFactory is not null)
        {
            var examsClient = httpFactory.CreateClient("exams");
            var exams = await examsClient.GetFromJsonAsync<List<ExamSummary>>("/api/exams?page=1&pageSize=10000");
            if (exams is null)
                return Results.Problem("Failed to retrieve exam data from ExamService", statusCode: StatusCodes.Status502BadGateway);
            source = exams.Select(e => (e.ScheduledDate, 1)).ToList();
        }
        else
        {
            return Results.Problem("No data source available", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var examsOverTime = source
            .GroupBy(e => new { e.ScheduledDate.Year, e.ScheduledDate.Month })
            .Select(g => new ExamsOverTimeItem($"{g.Key.Year}-{g.Key.Month:D2}", g.Count()))
            .OrderBy(x => x.Month).ToList();

        StatisticsTelemetry.ExamsOverTimeQueried.Add(1);
        activity?.SetTag("statistics.months_count", examsOverTime.Count);
        logger.LogInformation("Returned exams over time for {Count} months", examsOverTime.Count);
        return Results.Ok(new ExamsOverTimeResponse(examsOverTime, GetStatisticsLinks()));
    }

    private static async Task<IResult> GetAvgDurationByExamType(
        [FromServices] StatisticsStore? store, [FromServices] IHttpClientFactory? httpFactory, [FromServices] ILogger<Program> logger)
    {
        using var activity = StatisticsTelemetry.ActivitySource.StartActivity("GetAvgDurationByExamType");
        logger.LogInformation("Retrieving average duration by exam type statistics");

        List<(string Type, DateOnly ScheduledDate, int? DurationMinutes)> source;
        if (store is not null)
        {
            source = store.GetAllExams().Select(e => (e.Type, e.ScheduledDate, e.DurationMinutes)).ToList();
        }
        else if (httpFactory is not null)
        {
            var examsClient = httpFactory.CreateClient("exams");
            var exams = await examsClient.GetFromJsonAsync<List<ExamSummary>>("/api/exams?page=1&pageSize=10000");
            if (exams is null)
                return Results.Problem("Failed to retrieve exam data from ExamService", statusCode: StatusCodes.Status502BadGateway);
            source = exams.Select(e => (e.Type, e.ScheduledDate, e.DurationMinutes)).ToList();
        }
        else
        {
            return Results.Problem("No data source available", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var avgDuration = source
            .Where(e => e.DurationMinutes.HasValue)
            .GroupBy(e => new { e.Type, e.ScheduledDate.Year, e.ScheduledDate.Month })
            .Select(g => new AvgDurationByExamTypeItem($"{g.Key.Year}-{g.Key.Month:D2}", g.Key.Type, Math.Round(g.Average(e => e.DurationMinutes!.Value), 1)))
            .OrderBy(x => x.Month).ThenBy(x => x.ExamType).ToList();

        StatisticsTelemetry.AvgDurationByExamTypeQueried.Add(1);
        activity?.SetTag("statistics.data_points", avgDuration.Count);
        logger.LogInformation("Returned average duration data with {Count} data points", avgDuration.Count);
        return Results.Ok(new AvgDurationByExamTypeResponse(avgDuration, GetStatisticsLinks()));
    }

    private static async Task<IResult> Seed([FromServices] StatisticsStore? store, [FromServices] ILiteDatabase? db, [FromServices] IHttpClientFactory? httpFactory,
        [FromServices] IWriteCommandQueue writeQueue, [FromServices] WriteCommandResultCoordinator resultCoordinator,
        [FromServices] ILogger<Program> logger, CancellationToken cancellationToken)
    {
        using var activity = AdminTelemetry.ActivitySource.StartActivity("SeedDatabase");
        logger.LogInformation("Seeding statistics data");

        int patientsCreated = 0, doctorsCreated = 0, examsCreated = 0;

        if (store is not null && db is not null)
        {
            // Testing mode: write seed data to local in-memory DB
            var patientIds = SeedDataGenerator.GeneratePatients();
            var doctorIds = SeedDataGenerator.GenerateDoctors();
            var examIds = SeedDataGenerator.GenerateExams(patientIds, doctorIds);

            var patientsCol = db.GetCollection<Patient>("patients");
            var doctorsCol = db.GetCollection<Doctor>("doctors");
            var examsCol = db.GetCollection<Exam>("exams");

            // Generate actual entities for the DB (same deterministic data as other services)
            var patients = SeedDataGenerator.GeneratePatientEntities(patientIds);
            var doctors = SeedDataGenerator.GenerateDoctorEntities(doctorIds);
            var exams = SeedDataGenerator.GenerateExamEntities(examIds, patientIds, doctorIds);

            patientsCol.DeleteAll();
            doctorsCol.DeleteAll();
            examsCol.DeleteAll();

            patientsCol.InsertBulk(patients);
            doctorsCol.InsertBulk(doctors);
            examsCol.InsertBulk(exams);

            patientsCreated = patients.Count;
            doctorsCreated = doctors.Count;
            examsCreated = exams.Count;
        }
        else if (httpFactory is not null)
        {
            try
            {
                var patientsClient = httpFactory.CreateClient("patients");
                var doctorsClient = httpFactory.CreateClient("doctors");
                var examsClient = httpFactory.CreateClient("exams");

                var patientSeedResponse = await patientsClient.PostAsync("/api/admin/seed", null, cancellationToken);
                var doctorSeedResponse = await doctorsClient.PostAsync("/api/admin/seed", null, cancellationToken);
                var examSeedResponse = await examsClient.PostAsync("/api/admin/seed", null, cancellationToken);

                if (patientSeedResponse.IsSuccessStatusCode)
                {
                    var pr = await patientSeedResponse.Content.ReadFromJsonAsync<SeedResponse>(cancellationToken: cancellationToken);
                    patientsCreated = pr?.PatientsCreated ?? 0;
                }
                if (doctorSeedResponse.IsSuccessStatusCode)
                {
                    var dr = await doctorSeedResponse.Content.ReadFromJsonAsync<SeedResponse>(cancellationToken: cancellationToken);
                    doctorsCreated = dr?.DoctorsCreated ?? 0;
                }
                if (examSeedResponse.IsSuccessStatusCode)
                {
                    var er = await examSeedResponse.Content.ReadFromJsonAsync<SeedResponse>(cancellationToken: cancellationToken);
                    examsCreated = er?.ExamsCreated ?? 0;
                }
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                logger.LogError(ex, "Seed fan-out failed");
                return Results.Problem($"Seed fan-out failed: {ex.Message}", statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }

        var commandId = Guid.NewGuid();
        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, new SeedDataCommand()), cancellationToken);
        await resultCoordinator.WaitAsync(commandId, cancellationToken);

        AdminTelemetry.SeedExecuted.Add(1);
        activity?.SetTag("admin.patients_added", patientsCreated);
        activity?.SetTag("admin.doctors_added", doctorsCreated);
        activity?.SetTag("admin.exams_added", examsCreated);
        logger.LogInformation("Database seeded with {Patients} patients, {Doctors} doctors, {Exams} exams", patientsCreated, doctorsCreated, examsCreated);

        return Results.Ok(new SeedResponse(patientsCreated, doctorsCreated, examsCreated,
        [
            new Link("self", "/api/admin/seed", "POST"), new Link("reset", "/api/admin/reset", "POST"),
            new Link("stats", "/api/admin/stats", "GET"), new Link("patients", "/api/patients", "GET"),
            new Link("doctors", "/api/doctors", "GET"), new Link("exams", "/api/exams", "GET")
        ]));
    }

    private static async Task<IResult> Reset([FromServices] StatisticsStore? store, [FromServices] ILiteDatabase? db, [FromServices] IHttpClientFactory? httpFactory,
        [FromServices] IWriteCommandQueue writeQueue, [FromServices] WriteCommandResultCoordinator resultCoordinator,
        [FromServices] ILogger<Program> logger, CancellationToken cancellationToken)
    {
        using var activity = AdminTelemetry.ActivitySource.StartActivity("ResetDatabase");
        logger.LogInformation("Resetting statistics data");

        int deletedPatients = 0, deletedDoctors = 0, deletedExams = 0;

        if (store is not null && db is not null)
        {
            // Testing mode: clear local DB collections and return counts
            var patientsCol = db.GetCollection<Patient>("patients");
            var doctorsCol = db.GetCollection<Doctor>("doctors");
            var examsCol = db.GetCollection<Exam>("exams");

            deletedPatients = patientsCol.DeleteAll();
            deletedDoctors = doctorsCol.DeleteAll();
            deletedExams = examsCol.DeleteAll();
        }
        else if (httpFactory is not null)
        {
            try
            {
                var pc = httpFactory.CreateClient("patients");
                var dc = httpFactory.CreateClient("doctors");
                var ec = httpFactory.CreateClient("exams");
                var pr = await pc.PostAsync("/api/admin/reset", null, cancellationToken);
                var dr = await dc.PostAsync("/api/admin/reset", null, cancellationToken);
                var er = await ec.PostAsync("/api/admin/reset", null, cancellationToken);
                if (pr.IsSuccessStatusCode) { var r = await pr.Content.ReadFromJsonAsync<ResetResponse>(cancellationToken: cancellationToken); deletedPatients = r?.PatientsDeleted ?? 0; }
                if (dr.IsSuccessStatusCode) { var r = await dr.Content.ReadFromJsonAsync<ResetResponse>(cancellationToken: cancellationToken); deletedDoctors = r?.DoctorsDeleted ?? 0; }
                if (er.IsSuccessStatusCode) { var r = await er.Content.ReadFromJsonAsync<ResetResponse>(cancellationToken: cancellationToken); deletedExams = r?.ExamsDeleted ?? 0; }
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                logger.LogError(ex, "Reset fan-out failed");
                return Results.Problem($"Reset fan-out failed: {ex.Message}", statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }

        var commandId = Guid.NewGuid();
        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, new ResetDataCommand()), cancellationToken);
        await resultCoordinator.WaitAsync(commandId, cancellationToken);

        AdminTelemetry.ResetExecuted.Add(1);
        activity?.SetTag("admin.patients_deleted", deletedPatients);
        activity?.SetTag("admin.doctors_deleted", deletedDoctors);
        activity?.SetTag("admin.exams_deleted", deletedExams);
        logger.LogInformation("Database reset: removed {Patients} patients, {Doctors} doctors, {Exams} exams", deletedPatients, deletedDoctors, deletedExams);

        return Results.Ok(new ResetResponse(deletedPatients, deletedDoctors, deletedExams,
        [
            new Link("self", "/api/admin/reset", "POST"), new Link("seed", "/api/admin/seed", "POST"),
            new Link("stats", "/api/admin/stats", "GET")
        ]));
    }

    private static async Task<IResult> GetStats([FromServices] StatisticsStore? store, [FromServices] IHttpClientFactory? httpFactory, [FromServices] ILogger<Program> logger)
    {
        using var activity = AdminTelemetry.ActivitySource.StartActivity("GetDatabaseStats");
        logger.LogInformation("Retrieving database stats");

        int patientCount = 0, doctorCount = 0, examCount = 0;

        if (store is not null)
        {
            patientCount = store.GetPatientCount();
            doctorCount = store.GetDoctorCount();
            examCount = store.GetExamCount();
        }
        else if (httpFactory is not null)
        {
            try
            {
                var pc = httpFactory.CreateClient("patients");
                var dc = httpFactory.CreateClient("doctors");
                var ec = httpFactory.CreateClient("exams");
                var pr = await pc.GetAsync("/api/admin/stats");
                var dr = await dc.GetAsync("/api/admin/stats");
                var er = await ec.GetAsync("/api/admin/stats");
                if (pr.IsSuccessStatusCode) { var s = await pr.Content.ReadFromJsonAsync<StatsResponse>(); patientCount = s?.PatientCount ?? 0; }
                if (dr.IsSuccessStatusCode) { var s = await dr.Content.ReadFromJsonAsync<StatsResponse>(); doctorCount = s?.DoctorCount ?? 0; }
                if (er.IsSuccessStatusCode) { var s = await er.Content.ReadFromJsonAsync<StatsResponse>(); examCount = s?.ExamCount ?? 0; }
            }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to retrieve stats from some services"); }
        }

        AdminTelemetry.StatsQueried.Add(1);
        logger.LogInformation("Database stats: {Patients} patients, {Doctors} doctors, {Exams} exams", patientCount, doctorCount, examCount);
        return Results.Ok(new StatsResponse(patientCount, doctorCount, examCount, GetAdminLinks()));
    }
}
