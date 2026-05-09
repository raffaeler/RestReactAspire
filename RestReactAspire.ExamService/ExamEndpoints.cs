using System.Diagnostics;
using RestReactAspire.Shared.Cqrs;
using RestReactAspire.Shared.Models;
using RestReactAspire.Shared.Stores;
using RestReactAspire.Shared.Telemetry;

namespace RestReactAspire.ExamService;

public static class ExamEndpoints
{
    public static RouteGroupBuilder MapExamEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById).WithName("GetExamById");
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);
        group.MapPut("/{id:guid}/doctor", AssignDoctor);

        return group;
    }

    public static RouteGroupBuilder MapPatientExamEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetByPatient);

        return group;
    }

    public static RouteGroupBuilder MapDoctorExamEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetByDoctor);

        return group;
    }

    public static RouteGroupBuilder MapExamAdminEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/seed", Seed);
        group.MapPost("/reset", Reset);
        group.MapGet("/stats", GetStats);

        return group;
    }

    private static IResult GetAll(ExamStore store, ILogger<ExamStore> logger, int page = 1, int pageSize = 10, string? search = null, string sortBy = "scheduledDate", string sortDirection = "asc")
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("GetAllExams");

        logger.LogInformation("Retrieving exams page {Page} with size {PageSize}, search {Search}, sort {SortBy} {SortDirection}", page, pageSize, search, sortBy, sortDirection);

        var (exams, totalCount) = string.IsNullOrWhiteSpace(search)
            ? store.GetPaged(page, pageSize, sortBy, sortDirection)
            : store.SearchPaged(search, page, pageSize, sortBy, sortDirection);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        activity?.SetTag("exam.count", exams.Count);
        activity?.SetTag("exam.totalCount", totalCount);
        if (!string.IsNullOrWhiteSpace(search)) activity?.SetTag("exam.search", search);
        ExamTelemetry.ExamsQueried.Add(1);

        var items = exams.Select(ToExamResponse).ToList();
        var pagination = new PaginationInfo(page, pageSize, totalCount, totalPages);
        var sort = new SortInfo(sortBy, sortDirection);
        var links = PaginationLinks.Build("/api/exams", page, pageSize, totalPages, search, sortBy, sortDirection,
            new Link("create", "/api/exams", "POST"));
        var response = new ExamListResponse(items, pagination, sort, links);

        return Results.Ok(response);
    }

    private static IResult GetByPatient(Guid patientId, ExamStore store, ILogger<ExamStore> logger, int page = 1, int pageSize = 10, string? search = null, string sortBy = "scheduledDate", string sortDirection = "asc")
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("GetExamsByPatient");
        activity?.SetTag("patient.id", patientId.ToString());

        // In a standalone microservice, we don't validate patient existence (the gateway handles that).
        logger.LogInformation("Retrieving exams for patient {PatientId} page {Page} with size {PageSize}, search {Search}, sort {SortBy} {SortDirection}", patientId, page, pageSize, search, sortBy, sortDirection);

        var (exams, totalCount) = string.IsNullOrWhiteSpace(search)
            ? store.GetByPatientIdPaged(patientId, page, pageSize, sortBy, sortDirection)
            : store.SearchByPatientIdPaged(patientId, search, page, pageSize, sortBy, sortDirection);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        activity?.SetTag("exam.count", exams.Count);
        activity?.SetTag("exam.totalCount", totalCount);
        if (!string.IsNullOrWhiteSpace(search)) activity?.SetTag("exam.search", search);
        ExamTelemetry.ExamsQueried.Add(1);

        var items = exams.Select(ToExamResponse).ToList();
        var pagination = new PaginationInfo(page, pageSize, totalCount, totalPages);
        var sort = new SortInfo(sortBy, sortDirection);
        var basePath = $"/api/patients/{patientId}/exams";
        var links = PaginationLinks.Build(basePath, page, pageSize, totalPages, search, sortBy, sortDirection,
            new Link("create", "/api/exams", "POST"),
            new Link("patient", $"/api/patients/{patientId}", "GET"));
        var response = new ExamListResponse(items, pagination, sort, links);

        return Results.Ok(response);
    }

    private static IResult GetByDoctor(Guid doctorId, ExamStore store, ILogger<ExamStore> logger, int page = 1, int pageSize = 10, string? search = null, string sortBy = "scheduledDate", string sortDirection = "asc")
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("GetExamsByDoctor");
        activity?.SetTag("doctor.id", doctorId.ToString());

        // In a standalone microservice, we don't validate doctor existence (the gateway handles that).
        logger.LogInformation("Retrieving exams for doctor {DoctorId} page {Page} with size {PageSize}, search {Search}, sort {SortBy} {SortDirection}", doctorId, page, pageSize, search, sortBy, sortDirection);

        var (exams, totalCount) = string.IsNullOrWhiteSpace(search)
            ? store.GetByDoctorIdPaged(doctorId, page, pageSize, sortBy, sortDirection)
            : store.SearchByDoctorIdPaged(doctorId, search, page, pageSize, sortBy, sortDirection);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        activity?.SetTag("exam.count", exams.Count);
        activity?.SetTag("exam.totalCount", totalCount);
        if (!string.IsNullOrWhiteSpace(search)) activity?.SetTag("exam.search", search);
        ExamTelemetry.ExamsQueried.Add(1);

        var items = exams.Select(ToExamResponse).ToList();
        var pagination = new PaginationInfo(page, pageSize, totalCount, totalPages);
        var sort = new SortInfo(sortBy, sortDirection);
        var basePath = $"/api/doctors/{doctorId}/exams";
        var links = PaginationLinks.Build(basePath, page, pageSize, totalPages, search, sortBy, sortDirection,
            new Link("create", "/api/exams", "POST"),
            new Link("doctor", $"/api/doctors/{doctorId}", "GET"));
        var response = new ExamListResponse(items, pagination, sort, links);

        return Results.Ok(response);
    }

    private static IResult GetById(Guid id, ExamStore store, ILogger<ExamStore> logger)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("GetExamById");
        activity?.SetTag("exam.id", id.ToString());

        var exam = store.GetById(id);
        if (exam is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Exam not found");
            logger.LogWarning("Exam {ExamId} not found", id);
            return Results.NotFound();
        }

        ExamTelemetry.ExamsQueried.Add(1);
        logger.LogInformation("Retrieved exam {ExamId}", id);
        return Results.Ok(ToExamResponse(exam));
    }

    private static async Task<IResult> Create(
        CreateExamRequest request,
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        ExamStore store,
        ILogger<ExamStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("CreateExam");

        // No patient/doctor validation — the gateway is responsible for that.

        var examId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        var command = new CreateExamCommand(
            examId,
            request.PatientId,
            request.DoctorId,
            request.Type,
            request.ScheduledDate,
            request.ScheduledTime,
            request.DurationMinutes,
            request.Status,
            request.Results,
            request.Notes);

        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, command), cancellationToken);
        var result = await resultCoordinator.WaitAsync(commandId, cancellationToken);
        if (!result.Succeeded)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
            logger.LogWarning("Create exam command failed: {ErrorCode} {ErrorMessage}", result.ErrorCode, result.ErrorMessage);
            return Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var exam = store.GetById(examId);
        if (exam is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Exam not available after command processing");
            logger.LogWarning("Exam {ExamId} not found after successful create command", examId);
            return Results.Problem("Exam creation did not complete in time.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        activity?.SetTag("exam.id", exam.Id.ToString());
        activity?.SetTag("patient.id", exam.PatientId.ToString());
        if (exam.DoctorId.HasValue) activity?.SetTag("doctor.id", exam.DoctorId.Value.ToString());
        ExamTelemetry.ExamsCreated.Add(1);

        logger.LogInformation("Created exam {ExamId} of type {ExamType} for patient {PatientId}",
            exam.Id, exam.Type, exam.PatientId);

        return Results.Created($"/api/exams/{exam.Id}", ToExamResponse(exam));
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateExamRequest request,
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        ExamStore store,
        ILogger<ExamStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("UpdateExam");
        activity?.SetTag("exam.id", id.ToString());

        if (store.GetById(id) is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Exam not found");
            logger.LogWarning("Exam {ExamId} not found for update", id);
            return Results.NotFound();
        }

        // No doctor validation — the gateway handles that.

        var commandId = Guid.NewGuid();
        var command = new UpdateExamCommand(
            id,
            request.DoctorId,
            request.Type,
            request.ScheduledDate,
            request.ScheduledTime,
            request.DurationMinutes,
            request.Status,
            request.Results,
            request.Notes);

        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, command), cancellationToken);
        var result = await resultCoordinator.WaitAsync(commandId, cancellationToken);
        if (!result.Succeeded)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
            logger.LogWarning("Update exam command failed for {ExamId}: {ErrorCode} {ErrorMessage}", id, result.ErrorCode, result.ErrorMessage);
            return result.ErrorCode switch
            {
                "ExamNotFound" => Results.NotFound(),
                _ => Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status503ServiceUnavailable)
            };
        }

        var exam = store.GetById(id);
        if (exam is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Exam not available after update command");
            logger.LogWarning("Exam {ExamId} not found after successful update command", id);
            return Results.Problem("Exam update did not complete in time.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        ExamTelemetry.ExamsUpdated.Add(1);
        logger.LogInformation("Updated exam {ExamId}", id);

        return Results.Ok(ToExamResponse(exam));
    }

    private static async Task<IResult> Delete(
        Guid id,
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        ExamStore store,
        ILogger<ExamStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("DeleteExam");
        activity?.SetTag("exam.id", id.ToString());

        if (store.GetById(id) is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Exam not found");
            logger.LogWarning("Exam {ExamId} not found for deletion", id);
            return Results.NotFound();
        }

        var commandId = Guid.NewGuid();
        var command = new DeleteExamCommand(id);

        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, command), cancellationToken);
        var result = await resultCoordinator.WaitAsync(commandId, cancellationToken);
        if (!result.Succeeded)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
            logger.LogWarning("Delete exam command failed for {ExamId}: {ErrorCode} {ErrorMessage}", id, result.ErrorCode, result.ErrorMessage);
            return result.ErrorCode == "ExamNotFound"
                ? Results.NotFound()
                : Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        ExamTelemetry.ExamsDeleted.Add(1);
        logger.LogInformation("Deleted exam {ExamId}", id);

        return Results.NoContent();
    }

    private static async Task<IResult> AssignDoctor(
        Guid id,
        AssignDoctorRequest request,
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        ExamStore store,
        ILogger<ExamStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("AssignDoctorToExam");
        activity?.SetTag("exam.id", id.ToString());

        if (store.GetById(id) is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Exam not found");
            logger.LogWarning("Exam {ExamId} not found for doctor assignment", id);
            return Results.NotFound();
        }

        // No doctor validation — the gateway handles that.

        var commandId = Guid.NewGuid();
        var command = new AssignDoctorToExamCommand(id, request.DoctorId);

        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, command), cancellationToken);
        var result = await resultCoordinator.WaitAsync(commandId, cancellationToken);
        if (!result.Succeeded)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
            logger.LogWarning("Assign doctor command failed for {ExamId}: {ErrorCode} {ErrorMessage}", id, result.ErrorCode, result.ErrorMessage);
            return result.ErrorCode switch
            {
                "ExamNotFound" => Results.NotFound(),
                _ => Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status503ServiceUnavailable)
            };
        }

        var exam = store.GetById(id);
        if (exam is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Exam not available after assignment command");
            logger.LogWarning("Exam {ExamId} not found after successful doctor assignment command", id);
            return Results.Problem("Doctor assignment did not complete in time.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        ExamTelemetry.ExamsUpdated.Add(1);
        logger.LogInformation("Assigned doctor {DoctorId} to exam {ExamId}", request.DoctorId, id);

        return Results.Ok(ToExamResponse(exam));
    }

    private static async Task<IResult> Seed(
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        ILogger<ExamStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("SeedExamData");

        logger.LogInformation("Seeding exam database with sample data");

        var commandId = Guid.NewGuid();
        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, new SeedDataCommand()), cancellationToken);
        var result = await resultCoordinator.WaitAsync(commandId, cancellationToken);
        if (!result.Succeeded)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
            logger.LogWarning("Seed exam command failed: {ErrorCode} {ErrorMessage}", result.ErrorCode, result.ErrorMessage);
            return Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var examsCreated = result.ExamsAffected;
        ExamTelemetry.ExamsCreated.Add(examsCreated);

        activity?.SetTag("admin.exams_added", examsCreated);
        logger.LogInformation("Exam database seeded with {Exams} exams", examsCreated);

        var response = new SeedResponse(
            0,
            0,
            examsCreated,
            [
                new Link("self", "/api/admin/seed", "POST"),
                new Link("reset", "/api/admin/reset", "POST"),
                new Link("stats", "/api/admin/stats", "GET"),
                new Link("exams", "/api/exams", "GET")
            ]);

        return Results.Ok(response);
    }

    private static async Task<IResult> Reset(
        IWriteCommandQueue writeQueue,
        WriteCommandResultCoordinator resultCoordinator,
        ILogger<ExamStore> logger,
        CancellationToken cancellationToken)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("ResetExamData");

        logger.LogInformation("Resetting exam database");

        var commandId = Guid.NewGuid();
        resultCoordinator.Prepare(commandId);
        await writeQueue.EnqueueAsync(WriteCommandEnvelope.Create(commandId, new ResetDataCommand()), cancellationToken);
        var result = await resultCoordinator.WaitAsync(commandId, cancellationToken);
        if (!result.Succeeded)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
            logger.LogWarning("Reset exam command failed: {ErrorCode} {ErrorMessage}", result.ErrorCode, result.ErrorMessage);
            return Results.Problem(result.ErrorMessage, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var deletedExams = result.ExamsAffected;
        ExamTelemetry.ExamsDeleted.Add(deletedExams);

        activity?.SetTag("admin.exams_deleted", deletedExams);
        logger.LogInformation("Exam database reset: removed {Exams} exams", deletedExams);

        var response = new ResetResponse(
            0,
            0,
            deletedExams,
            [
                new Link("self", "/api/admin/reset", "POST"),
                new Link("seed", "/api/admin/seed", "POST"),
                new Link("stats", "/api/admin/stats", "GET")
            ]);

        return Results.Ok(response);
    }

    private static IResult GetStats(ExamStore store, ILogger<ExamStore> logger)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("GetExamStats");

        var allExams = store.GetAll();
        var examCount = allExams.Count;

        ExamTelemetry.ExamsQueried.Add(1);
        logger.LogInformation("Exam stats: {ExamCount} exams", examCount);

        return Results.Ok(new { examCount });
    }

    internal static ExamResponse ToExamResponse(Exam exam)
    {
        var links = new List<Link>
        {
            new Link("self", $"/api/exams/{exam.Id}", "GET"),
            new Link("update", $"/api/exams/{exam.Id}", "PUT"),
            new Link("delete", $"/api/exams/{exam.Id}", "DELETE"),
            new Link("assign-doctor", $"/api/exams/{exam.Id}/doctor", "PUT"),
            new Link("patient", $"/api/patients/{exam.PatientId}", "GET"),
            new Link("patient-exams", $"/api/patients/{exam.PatientId}/exams", "GET"),
            new Link("collection", "/api/exams", "GET")
        };

        if (exam.DoctorId.HasValue)
        {
            links.Add(new Link("doctor", $"/api/doctors/{exam.DoctorId.Value}", "GET"));
            links.Add(new Link("doctor-exams", $"/api/doctors/{exam.DoctorId.Value}/exams", "GET"));
        }

        return new ExamResponse(
            exam.Id,
            exam.PatientId,
            exam.DoctorId,
            exam.Type,
            exam.ScheduledDate,
            exam.ScheduledTime,
            exam.DurationMinutes,
            exam.EndTime,
            exam.Status,
            exam.Results,
            exam.Notes,
            links);
    }
}
