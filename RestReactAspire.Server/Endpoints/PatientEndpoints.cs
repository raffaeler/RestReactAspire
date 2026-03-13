using System.Diagnostics;
using RestReactAspire.Server.Cqrs;
using RestReactAspire.Server.Models;
using RestReactAspire.Server.Stores;
using RestReactAspire.Server.Telemetry;

namespace RestReactAspire.Server.Endpoints;

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
                new Link("exams", $"/api/patients/{patient.Id}/exams", "GET"),
                new Link("collection", "/api/patients", "GET")
            ]);
    }
}
