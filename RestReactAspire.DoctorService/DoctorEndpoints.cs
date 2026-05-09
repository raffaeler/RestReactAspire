using System.Diagnostics;
using RestReactAspire.Shared.Cqrs;
using RestReactAspire.Shared.Models;
using RestReactAspire.Shared.Stores;
using RestReactAspire.Shared.Telemetry;

namespace RestReactAspire.DoctorService;

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

    public static RouteGroupBuilder MapDoctorAdminEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/seed", Seed);
        group.MapPost("/reset", Reset);
        group.MapGet("/stats", GetStats);

        return group;
    }

    private static IResult GetAll(DoctorStore store, ILogger<DoctorStore> logger, int page = 1, int pageSize = 10, string? search = null, string sortBy = "specialty", string sortDirection = "asc")
    {
        using var activity = DoctorTelemetry.ActivitySource.StartActivity("GetAllDoctors");

        logger.LogInformation("Retrieving doctors page {Page} with size {PageSize}, search {Search}, sort {SortBy} {SortDirection}", page, pageSize, search, sortBy, sortDirection);

        var (doctors, totalCount) = string.IsNullOrWhiteSpace(search)
            ? store.GetPaged(page, pageSize, sortBy, sortDirection)
            : store.SearchPaged(search, page, pageSize, sortBy, sortDirection);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        activity?.SetTag("doctor.count", doctors.Count);
        activity?.SetTag("doctor.totalCount", totalCount);
        if (!string.IsNullOrWhiteSpace(search)) activity?.SetTag("doctor.search", search);
        DoctorTelemetry.DoctorsQueried.Add(1);

        var items = doctors.Select(ToDoctorResponse).ToList();
        var pagination = new PaginationInfo(page, pageSize, totalCount, totalPages);
        var sort = new SortInfo(sortBy, sortDirection);
        var links = PaginationLinks.Build("/api/doctors", page, pageSize, totalPages, search, sortBy, sortDirection,
            new Link("create", "/api/doctors", "POST"));
        var response = new DoctorListResponse(items, pagination, sort, links);

        return Results.Ok(response);
    }

    private static IResult GetById(Guid id, DoctorStore store, ILogger<DoctorStore> logger)
    {
        using var activity = DoctorTelemetry.ActivitySource.StartActivity("GetDoctorById");
        activity?.SetTag("doctor.id", id.ToString());

        var doctor = store.GetById(id);
        if (doctor is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Doctor not found");
            logger.LogWarning("Doctor {DoctorId} not found", id);
            return Results.NotFound();
        }

        DoctorTelemetry.DoctorsQueried.Add(1);
        logger.LogInformation("Retrieved doctor {DoctorId}", id);

        return Results.Ok(ToDoctorResponse(doctor));
    }

    private static async Task<IResult> Create(
        CreateDoctorRequest request,
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        DoctorStore store,
        ILogger<DoctorStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = DoctorTelemetry.ActivitySource.StartActivity("CreateDoctor");

        var doctorId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        var command = new CreateDoctorCommand(
            doctorId,
            request.FirstName,
            request.LastName,
            request.Specialty,
            request.Email,
            request.Phone);

        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, command), cancellationToken);
        var result = await resultCoordinator.WaitAsync(commandId, cancellationToken);
        if (!result.Succeeded)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
            logger.LogWarning("Create doctor command failed: {ErrorCode} {ErrorMessage}", result.ErrorCode, result.ErrorMessage);
            return Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var doctor = store.GetById(doctorId);
        if (doctor is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Doctor not available after command processing");
            logger.LogWarning("Doctor {DoctorId} not found after successful create command", doctorId);
            return Results.Problem("Doctor creation did not complete in time.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        activity?.SetTag("doctor.id", doctor.Id.ToString());
        DoctorTelemetry.DoctorsCreated.Add(1);

        logger.LogInformation("Created doctor {DoctorId}: {FirstName} {LastName}",
            doctor.Id, doctor.FirstName, doctor.LastName);

        return Results.Created($"/api/doctors/{doctor.Id}", ToDoctorResponse(doctor));
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateDoctorRequest request,
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        DoctorStore store,
        ILogger<DoctorStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = DoctorTelemetry.ActivitySource.StartActivity("UpdateDoctor");
        activity?.SetTag("doctor.id", id.ToString());

        if (store.GetById(id) is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Doctor not found");
            logger.LogWarning("Doctor {DoctorId} not found for update", id);
            return Results.NotFound();
        }

        var commandId = Guid.NewGuid();
        var command = new UpdateDoctorCommand(
            id,
            request.FirstName,
            request.LastName,
            request.Specialty,
            request.Email,
            request.Phone);

        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, command), cancellationToken);
        var result = await resultCoordinator.WaitAsync(commandId, cancellationToken);
        if (!result.Succeeded)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
            logger.LogWarning("Update doctor command failed for {DoctorId}: {ErrorCode} {ErrorMessage}", id, result.ErrorCode, result.ErrorMessage);
            return result.ErrorCode == "DoctorNotFound"
                ? Results.NotFound()
                : Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var doctor = store.GetById(id);
        if (doctor is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Doctor not available after update command");
            logger.LogWarning("Doctor {DoctorId} not found after successful update command", id);
            return Results.Problem("Doctor update did not complete in time.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        DoctorTelemetry.DoctorsUpdated.Add(1);
        logger.LogInformation("Updated doctor {DoctorId}", id);

        return Results.Ok(ToDoctorResponse(doctor));
    }

    private static async Task<IResult> Delete(
        Guid id,
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        DoctorStore store,
        ILogger<DoctorStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = DoctorTelemetry.ActivitySource.StartActivity("DeleteDoctor");
        activity?.SetTag("doctor.id", id.ToString());

        if (store.GetById(id) is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Doctor not found");
            logger.LogWarning("Doctor {DoctorId} not found for deletion", id);
            return Results.NotFound();
        }

        var commandId = Guid.NewGuid();
        var command = new DeleteDoctorCommand(id);

        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, command), cancellationToken);
        var result = await resultCoordinator.WaitAsync(commandId, cancellationToken);
        if (!result.Succeeded)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
            logger.LogWarning("Delete doctor command failed for {DoctorId}: {ErrorCode} {ErrorMessage}", id, result.ErrorCode, result.ErrorMessage);
            return result.ErrorCode == "DoctorNotFound"
                ? Results.NotFound()
                : Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        DoctorTelemetry.DoctorsDeleted.Add(1);
        logger.LogInformation("Deleted doctor {DoctorId}", id);

        return Results.NoContent();
    }

    private static async Task<IResult> Seed(
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        ILogger<DoctorStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = DoctorTelemetry.ActivitySource.StartActivity("SeedDoctors");

        logger.LogInformation("Seeding doctor data");

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

        var doctorsCreated = result.DoctorsAffected;
        DoctorTelemetry.DoctorsCreated.Add(doctorsCreated);

        activity?.SetTag("admin.doctors_added", doctorsCreated);
        logger.LogInformation("Doctors seeded: {Doctors} doctors", doctorsCreated);

        var response = new SeedResponse(
            0,
            doctorsCreated,
            0,
            [
                new Link("self", "/api/admin/seed", "POST"),
                new Link("reset", "/api/admin/reset", "POST"),
                new Link("stats", "/api/admin/stats", "GET"),
                new Link("doctors", "/api/doctors", "GET")
            ]);

        return Results.Ok(response);
    }

    private static async Task<IResult> Reset(
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        ILogger<DoctorStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = DoctorTelemetry.ActivitySource.StartActivity("ResetDoctors");

        logger.LogInformation("Resetting doctor data");

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

        var deletedDoctors = result.DoctorsAffected;
        DoctorTelemetry.DoctorsDeleted.Add(deletedDoctors);

        activity?.SetTag("admin.doctors_deleted", deletedDoctors);
        logger.LogInformation("Doctors reset: removed {Doctors} doctors", deletedDoctors);

        var response = new ResetResponse(
            0,
            deletedDoctors,
            0,
            [
                new Link("self", "/api/admin/reset", "POST"),
                new Link("seed", "/api/admin/seed", "POST"),
                new Link("stats", "/api/admin/stats", "GET")
            ]);

        return Results.Ok(response);
    }

    private static IResult GetStats(DoctorStore store, ILogger<DoctorStore> logger)
    {
        using var activity = DoctorTelemetry.ActivitySource.StartActivity("GetDoctorStats");

        var doctorCount = store.GetAll().Count;
        DoctorTelemetry.DoctorsQueried.Add(1);

        logger.LogInformation("Doctor stats: {Doctors} doctors", doctorCount);

        var response = new StatsResponse(
            0,
            doctorCount,
            0,
            [
                new Link("self", "/api/admin/stats", "GET"),
                new Link("seed", "/api/admin/seed", "POST"),
                new Link("reset", "/api/admin/reset", "POST"),
                new Link("doctors", "/api/doctors", "GET")
            ]);

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
                new Link("collection", "/api/doctors", "GET")
            ]);
    }
}
