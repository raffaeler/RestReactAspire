using RestReactAspire.Server.Models;
using RestReactAspire.Server.Stores;
using RestReactAspire.Server.Telemetry;

namespace RestReactAspire.Server.Endpoints;

public static class DoctorEndpoints
{
    public static RouteGroupBuilder MapDoctorEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById).WithName("GetDoctorById");
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return group;
    }

    public static RouteGroupBuilder MapDoctorExamEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetByDoctor);

        return group;
    }

    private static IResult GetAll(DoctorStore store, ILogger<DoctorStore> logger, int page = 1, int pageSize = 10)
    {
        using var activity = DoctorTelemetry.ActivitySource.StartActivity("GetAllDoctors");

        logger.LogInformation("Retrieving doctors page {Page} with size {PageSize}", page, pageSize);

        var (doctors, totalCount) = store.GetPaged(page, pageSize);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        activity?.SetTag("doctor.count", doctors.Count);
        activity?.SetTag("doctor.totalCount", totalCount);

        var items = doctors.Select(ToDoctorResponse).ToList();
        var pagination = new PaginationInfo(page, pageSize, totalCount, totalPages);
        var links = PaginationLinks.Build("/api/doctors", page, pageSize, totalPages,
            new Link("create", "/api/doctors", "POST"));
        var response = new DoctorListResponse(items, pagination, links);

        return Results.Ok(response);
    }

    private static IResult GetById(Guid id, DoctorStore store, ILogger<DoctorStore> logger)
    {
        using var activity = DoctorTelemetry.ActivitySource.StartActivity("GetDoctorById");
        activity?.SetTag("doctor.id", id.ToString());

        var doctor = store.GetById(id);
        if (doctor is null)
        {
            logger.LogWarning("Doctor {DoctorId} not found", id);
            return Results.NotFound();
        }

        logger.LogInformation("Retrieved doctor {DoctorId}", id);
        return Results.Ok(ToDoctorResponse(doctor));
    }

    private static IResult Create(CreateDoctorRequest request, DoctorStore store, ILogger<DoctorStore> logger)
    {
        using var activity = DoctorTelemetry.ActivitySource.StartActivity("CreateDoctor");

        var doctor = store.Add(request);
        activity?.SetTag("doctor.id", doctor.Id.ToString());
        DoctorTelemetry.DoctorsCreated.Add(1);

        logger.LogInformation("Created doctor {DoctorId}: {FirstName} {LastName}",
            doctor.Id, doctor.FirstName, doctor.LastName);

        return Results.Created($"/api/doctors/{doctor.Id}", ToDoctorResponse(doctor));
    }

    private static IResult Update(Guid id, UpdateDoctorRequest request, DoctorStore store, ILogger<DoctorStore> logger)
    {
        using var activity = DoctorTelemetry.ActivitySource.StartActivity("UpdateDoctor");
        activity?.SetTag("doctor.id", id.ToString());

        var doctor = store.Update(id, request);
        if (doctor is null)
        {
            logger.LogWarning("Doctor {DoctorId} not found for update", id);
            return Results.NotFound();
        }

        DoctorTelemetry.DoctorsUpdated.Add(1);
        logger.LogInformation("Updated doctor {DoctorId}", id);

        return Results.Ok(ToDoctorResponse(doctor));
    }

    private static IResult Delete(Guid id, DoctorStore store, ILogger<DoctorStore> logger)
    {
        using var activity = DoctorTelemetry.ActivitySource.StartActivity("DeleteDoctor");
        activity?.SetTag("doctor.id", id.ToString());

        if (!store.Delete(id))
        {
            logger.LogWarning("Doctor {DoctorId} not found for deletion", id);
            return Results.NotFound();
        }

        DoctorTelemetry.DoctorsDeleted.Add(1);
        logger.LogInformation("Deleted doctor {DoctorId}", id);

        return Results.NoContent();
    }

    private static IResult GetByDoctor(Guid doctorId, ExamStore examStore, DoctorStore doctorStore, ILogger<DoctorStore> logger, int page = 1, int pageSize = 10)
    {
        using var activity = DoctorTelemetry.ActivitySource.StartActivity("GetExamsByDoctor");
        activity?.SetTag("doctor.id", doctorId.ToString());

        var doctor = doctorStore.GetById(doctorId);
        if (doctor is null)
        {
            logger.LogWarning("Doctor {DoctorId} not found when listing exams", doctorId);
            return Results.NotFound();
        }

        logger.LogInformation("Retrieving exams for doctor {DoctorId} page {Page} with size {PageSize}", doctorId, page, pageSize);

        var (exams, totalCount) = examStore.GetByDoctorIdPaged(doctorId, page, pageSize);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        activity?.SetTag("exam.count", exams.Count);
        activity?.SetTag("exam.totalCount", totalCount);

        var items = exams.Select(ExamEndpoints.ToExamResponse).ToList();
        var pagination = new PaginationInfo(page, pageSize, totalCount, totalPages);
        var basePath = $"/api/doctors/{doctorId}/exams";
        var links = PaginationLinks.Build(basePath, page, pageSize, totalPages,
            new Link("doctor", $"/api/doctors/{doctorId}", "GET"));
        var response = new ExamListResponse(items, pagination, links);

        return Results.Ok(response);
    }

    private static DoctorResponse ToDoctorResponse(Doctor doctor)
    {
        return new DoctorResponse(
            doctor.Id,
            doctor.FirstName,
            doctor.LastName,
            doctor.Specialty,
            doctor.Email,
            doctor.Phone,
            [
                new Link("self", $"/api/doctors/{doctor.Id}", "GET"),
                new Link("update", $"/api/doctors/{doctor.Id}", "PUT"),
                new Link("delete", $"/api/doctors/{doctor.Id}", "DELETE"),
                new Link("exams", $"/api/doctors/{doctor.Id}/exams", "GET"),
                new Link("collection", "/api/doctors", "GET")
            ]);
    }
}
