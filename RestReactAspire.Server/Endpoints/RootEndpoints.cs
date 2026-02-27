using RestReactAspire.Server.Models;

namespace RestReactAspire.Server.Endpoints;

public static class RootEndpoints
{
    public static RouteGroupBuilder MapRootEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", () =>
        {
            var response = new ApiRootResponse([
                new Link("self", "/api", "GET"),
                new Link("patients", "/api/patients", "GET"),
                new Link("exams", "/api/exams", "GET"),
                new Link("doctors", "/api/doctors", "GET"),
                new Link("admin-stats", "/api/admin/stats", "GET"),
                new Link("admin-seed", "/api/admin/seed", "POST"),
                new Link("admin-reset", "/api/admin/reset", "POST")
            ]);
            return Results.Ok(response);
        })
        .WithName("GetApiRoot");

        return group;
    }
}
