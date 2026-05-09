using System.Diagnostics;
using RestReactAspire.Infrastructure.Cqrs;
using RestReactAspire.PatientService.Models;
using RestReactAspire.PatientService.Stores;
using RestReactAspire.PatientService.Telemetry;

namespace RestReactAspire.PatientService;

public static class PatientEndpoints
{
    public static RouteGroupBuilder MapPatientEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById).WithName("GetPatientById");
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return group;
    }

    public static RouteGroupBuilder MapPatientAdminEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/seed", Seed);
        group.MapPost("/reset", Reset);
        group.MapGet("/stats", GetStats);

        return group;
    }

    private static IResult GetAll(PatientStore store, ILogger<PatientStore> logger, int page = 1, int pageSize = 10, string? search = null, string sortBy = "lastName", string sortDirection = "asc")
    {
        using var activity = PatientTelemetry.ActivitySource.StartActivity("GetAllPatients");

        logger.LogInformation("Retrieving patients page {Page} with size {PageSize}, search {Search}, sort {SortBy} {SortDirection}", page, pageSize, search, sortBy, sortDirection);

        var (patients, totalCount) = string.IsNullOrWhiteSpace(search)
            ? store.GetPaged(page, pageSize, sortBy, sortDirection)
            : store.SearchPaged(search, page, pageSize, sortBy, sortDirection);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        activity?.SetTag("patient.count", patients.Count);
        activity?.SetTag("patient.totalCount", totalCount);
        if (!string.IsNullOrWhiteSpace(search)) activity?.SetTag("patient.search", search);
        PatientTelemetry.PatientsQueried.Add(1);

        var items = patients.Select(ToPatientResponse).ToList();
        var pagination = new PaginationInfo(page, pageSize, totalCount, totalPages);
        var sort = new SortInfo(sortBy, sortDirection);
        var links = PaginationLinks.Build("/api/patients", page, pageSize, totalPages, search, sortBy, sortDirection,
            new Link("create", "/api/patients", "POST"));
        var response = new PatientListResponse(items, pagination, sort, links);

        return Results.Ok(response);
    }

    private static IResult GetById(Guid id, PatientStore store, ILogger<PatientStore> logger)
    {
        using var activity = PatientTelemetry.ActivitySource.StartActivity("GetPatientById");
        activity?.SetTag("patient.id", id.ToString());

        var patient = store.GetById(id);
        if (patient is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Patient not found");
            logger.LogWarning("Patient {PatientId} not found", id);
            return Results.NotFound();
        }

        PatientTelemetry.PatientsQueried.Add(1);
        logger.LogInformation("Retrieved patient {PatientId}", id);
        return Results.Ok(ToPatientResponse(patient));
    }

    private static async Task<IResult> Create(
        CreatePatientRequest request,
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        PatientStore store,
        ILogger<PatientStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = PatientTelemetry.ActivitySource.StartActivity("CreatePatient");

        var patientId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        var command = new CreatePatientCommand(
            patientId,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Email,
            request.Phone);

        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, command), cancellationToken);
        var result = await resultCoordinator.WaitAsync(commandId, cancellationToken);
        if (!result.Succeeded)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
            logger.LogWarning("Create patient command failed: {ErrorCode} {ErrorMessage}", result.ErrorCode, result.ErrorMessage);
            return Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var patient = store.GetById(patientId);
        if (patient is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Patient not available after command processing");
            logger.LogWarning("Patient {PatientId} not found after successful create command", patientId);
            return Results.Problem("Patient creation did not complete in time.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        activity?.SetTag("patient.id", patient.Id.ToString());
        PatientTelemetry.PatientsCreated.Add(1);

        logger.LogInformation("Created patient {PatientId}: {FirstName} {LastName}",
            patient.Id, patient.FirstName, patient.LastName);

        return Results.Created($"/api/patients/{patient.Id}", ToPatientResponse(patient));
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdatePatientRequest request,
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        PatientStore store,
        ILogger<PatientStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = PatientTelemetry.ActivitySource.StartActivity("UpdatePatient");
        activity?.SetTag("patient.id", id.ToString());

        if (store.GetById(id) is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Patient not found");
            logger.LogWarning("Patient {PatientId} not found for update", id);
            return Results.NotFound();
        }

        var commandId = Guid.NewGuid();
        var command = new UpdatePatientCommand(
            id,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Email,
            request.Phone);

        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, command), cancellationToken);
        var result = await resultCoordinator.WaitAsync(commandId, cancellationToken);
        if (!result.Succeeded)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
            logger.LogWarning("Update patient command failed for {PatientId}: {ErrorCode} {ErrorMessage}", id, result.ErrorCode, result.ErrorMessage);
            return result.ErrorCode == "PatientNotFound"
                ? Results.NotFound()
                : Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var patient = store.GetById(id);
        if (patient is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Patient not available after update command");
            logger.LogWarning("Patient {PatientId} not found after successful update command", id);
            return Results.Problem("Patient update did not complete in time.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        PatientTelemetry.PatientsUpdated.Add(1);
        logger.LogInformation("Updated patient {PatientId}", id);

        return Results.Ok(ToPatientResponse(patient));
    }

    private static async Task<IResult> Delete(
        Guid id,
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        PatientStore store,
        ILogger<PatientStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = PatientTelemetry.ActivitySource.StartActivity("DeletePatient");
        activity?.SetTag("patient.id", id.ToString());

        if (store.GetById(id) is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Patient not found");
            logger.LogWarning("Patient {PatientId} not found for deletion", id);
            return Results.NotFound();
        }

        var commandId = Guid.NewGuid();
        var command = new DeletePatientCommand(id);

        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, command), cancellationToken);
        var result = await resultCoordinator.WaitAsync(commandId, cancellationToken);
        if (!result.Succeeded)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
            logger.LogWarning("Delete patient command failed for {PatientId}: {ErrorCode} {ErrorMessage}", id, result.ErrorCode, result.ErrorMessage);
            return result.ErrorCode == "PatientNotFound"
                ? Results.NotFound()
                : Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        PatientTelemetry.PatientsDeleted.Add(1);
        logger.LogInformation("Deleted patient {PatientId}", id);

        return Results.NoContent();
    }

    private static async Task<IResult> Seed(
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        ILogger<PatientStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = AdminTelemetry.ActivitySource.StartActivity("SeedDatabase");

        logger.LogInformation("Seeding database with sample patient data");

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

        AdminTelemetry.SeedExecuted.Add(1);

        activity?.SetTag("admin.patients_added", patientsCreated);

        logger.LogInformation("Database seeded with {Patients} patients", patientsCreated);

        var response = new SeedResponse(
            patientsCreated,
            0,
            0,
            [
                new Link("self", "/api/admin/seed", "POST"),
                new Link("reset", "/api/admin/reset", "POST"),
                new Link("stats", "/api/admin/stats", "GET"),
                new Link("patients", "/api/patients", "GET")
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

        logger.LogInformation("Resetting patient database");

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

        AdminTelemetry.ResetExecuted.Add(1);

        activity?.SetTag("admin.patients_deleted", deletedPatients);

        logger.LogInformation("Database reset: removed {Patients} patients", deletedPatients);

        var response = new ResetResponse(
            deletedPatients,
            0,
            0,
            [
                new Link("self", "/api/admin/reset", "POST"),
                new Link("seed", "/api/admin/seed", "POST"),
                new Link("stats", "/api/admin/stats", "GET")
            ]);

        return Results.Ok(response);
    }

    private static IResult GetStats(PatientStore store, ILogger<PatientStore> logger)
    {
        using var activity = AdminTelemetry.ActivitySource.StartActivity("GetDatabaseStats");

        var patientCount = store.GetAll().Count;

        AdminTelemetry.StatsQueried.Add(1);

        logger.LogInformation("Database stats: {Patients} patients", patientCount);

        var response = new StatsResponse(
            patientCount,
            0,
            0,
            [
                new Link("self", "/api/admin/stats", "GET"),
                new Link("seed", "/api/admin/seed", "POST"),
                new Link("reset", "/api/admin/reset", "POST"),
                new Link("patients", "/api/patients", "GET")
            ]);

        return Results.Ok(response);
    }

    private static PatientResponse ToPatientResponse(Patient patient)
    {
        return new PatientResponse(
            patient.Id,
            patient.FirstName,
            patient.LastName,
            patient.DateOfBirth,
            patient.Email,
            patient.Phone,
            [
                new Link("self", $"/api/patients/{patient.Id}", "GET"),
                new Link("update", $"/api/patients/{patient.Id}", "PUT"),
                new Link("delete", $"/api/patients/{patient.Id}", "DELETE"),
                new Link("collection", "/api/patients", "GET")
            ]);
    }
}
