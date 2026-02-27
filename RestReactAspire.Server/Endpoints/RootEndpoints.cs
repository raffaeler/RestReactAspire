using RestReactAspire.Server.Models;
using RestReactAspire.Server.Telemetry;

namespace RestReactAspire.Server.Endpoints;

public static class RootEndpoints
{
    public static RouteGroupBuilder MapRootEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", (ILogger<Program> logger) =>
        {
            using var activity = RootTelemetry.ActivitySource.StartActivity("GetApiRoot");

            logger.LogInformation("API root requested");
            RootTelemetry.RootRequested.Add(1);

            var response = new ApiRootResponse([
                new Link("self", "/api", "GET"),
                new Link("patients", "/api/patients", "GET"),
                new Link("exams", "/api/exams", "GET"),
                new Link("doctors", "/api/doctors", "GET"),
                new Link("admin-stats", "/api/admin/stats", "GET"),
                new Link("admin-seed", "/api/admin/seed", "POST"),
                new Link("admin-reset", "/api/admin/reset", "POST"),
                new Link("statistics-patients-by-age-group", "/api/statistics/patients-by-age-group", "GET"),
                new Link("statistics-exams-per-doctor", "/api/statistics/exams-per-doctor", "GET"),
                new Link("statistics-exams-over-time", "/api/statistics/exams-over-time", "GET"),
                new Link("statistics-avg-duration-by-exam-type", "/api/statistics/avg-duration-by-exam-type", "GET")
            ]);
            return Results.Ok(response);
        })
        .WithName("GetApiRoot");

        return group;
    }
}
