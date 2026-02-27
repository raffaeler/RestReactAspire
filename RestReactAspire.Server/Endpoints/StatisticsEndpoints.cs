using LiteDB;
using RestReactAspire.Server.Models;
using RestReactAspire.Server.Telemetry;

namespace RestReactAspire.Server.Endpoints;

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

    private static IResult GetPatientsByAgeGroup(ILiteDatabase database, ILogger<Program> logger)
    {
        using var activity = StatisticsTelemetry.ActivitySource.StartActivity("GetPatientsByAgeGroup");

        logger.LogInformation("Retrieving patients by age group statistics");

        var patients = database.GetCollection<Patient>("patients").FindAll().ToList();
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

    private static IResult GetExamsPerDoctor(ILiteDatabase database, ILogger<Program> logger)
    {
        using var activity = StatisticsTelemetry.ActivitySource.StartActivity("GetExamsPerDoctor");

        logger.LogInformation("Retrieving exams per doctor statistics");

        var exams = database.GetCollection<Exam>("exams").FindAll().ToList();
        var doctors = database.GetCollection<Doctor>("doctors").FindAll().ToDictionary(d => d.Id);

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

    private static IResult GetExamsOverTime(ILiteDatabase database, ILogger<Program> logger)
    {
        using var activity = StatisticsTelemetry.ActivitySource.StartActivity("GetExamsOverTime");

        logger.LogInformation("Retrieving exams over time statistics");

        var exams = database.GetCollection<Exam>("exams").FindAll().ToList();

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

    private static IResult GetAvgDurationByExamType(ILiteDatabase database, ILogger<Program> logger)
    {
        using var activity = StatisticsTelemetry.ActivitySource.StartActivity("GetAvgDurationByExamType");

        logger.LogInformation("Retrieving average duration by exam type statistics");

        var exams = database.GetCollection<Exam>("exams").FindAll().ToList();

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
}
