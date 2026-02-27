using RestReactAspire.Server.Models;
using RestReactAspire.Server.Stores;
using RestReactAspire.Server.Telemetry;

namespace RestReactAspire.Server.Endpoints;

public static class ExamEndpoints
{
    public static RouteGroupBuilder MapExamEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById).WithName("GetExamById");
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return group;
    }

    public static RouteGroupBuilder MapPatientExamEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetByPatient);

        return group;
    }

    private static IResult GetAll(ExamStore store, ILogger<ExamStore> logger)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("GetAllExams");

        logger.LogInformation("Retrieving all exams");

        var exams = store.GetAll();
        activity?.SetTag("exam.count", exams.Count);

        var items = exams.Select(ToExamResponse).ToList();
        var response = new ExamListResponse(items, [
            new Link("self", "/api/exams", "GET"),
            new Link("create", "/api/exams", "POST")
        ]);

        return Results.Ok(response);
    }

    private static IResult GetByPatient(Guid patientId, ExamStore store, PatientStore patientStore, ILogger<ExamStore> logger)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("GetExamsByPatient");
        activity?.SetTag("patient.id", patientId.ToString());

        var patient = patientStore.GetById(patientId);
        if (patient is null)
        {
            logger.LogWarning("Patient {PatientId} not found when listing exams", patientId);
            return Results.NotFound();
        }

        logger.LogInformation("Retrieving exams for patient {PatientId}", patientId);

        var exams = store.GetByPatientId(patientId);
        activity?.SetTag("exam.count", exams.Count);

        var items = exams.Select(ToExamResponse).ToList();
        var response = new ExamListResponse(items, [
            new Link("self", $"/api/patients/{patientId}/exams", "GET"),
            new Link("create", "/api/exams", "POST"),
            new Link("patient", $"/api/patients/{patientId}", "GET")
        ]);

        return Results.Ok(response);
    }

    private static IResult GetById(Guid id, ExamStore store, ILogger<ExamStore> logger)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("GetExamById");
        activity?.SetTag("exam.id", id.ToString());

        var exam = store.GetById(id);
        if (exam is null)
        {
            logger.LogWarning("Exam {ExamId} not found", id);
            return Results.NotFound();
        }

        logger.LogInformation("Retrieved exam {ExamId}", id);
        return Results.Ok(ToExamResponse(exam));
    }

    private static IResult Create(CreateExamRequest request, ExamStore store, PatientStore patientStore, ILogger<ExamStore> logger)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("CreateExam");

        var patient = patientStore.GetById(request.PatientId);
        if (patient is null)
        {
            logger.LogWarning("Patient {PatientId} not found when creating exam", request.PatientId);
            return Results.NotFound();
        }

        var exam = store.Add(request);
        activity?.SetTag("exam.id", exam.Id.ToString());
        activity?.SetTag("patient.id", exam.PatientId.ToString());
        ExamTelemetry.ExamsCreated.Add(1);

        logger.LogInformation("Created exam {ExamId} of type {ExamType} for patient {PatientId}",
            exam.Id, exam.Type, exam.PatientId);

        return Results.Created($"/api/exams/{exam.Id}", ToExamResponse(exam));
    }

    private static IResult Update(Guid id, UpdateExamRequest request, ExamStore store, ILogger<ExamStore> logger)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("UpdateExam");
        activity?.SetTag("exam.id", id.ToString());

        var exam = store.Update(id, request);
        if (exam is null)
        {
            logger.LogWarning("Exam {ExamId} not found for update", id);
            return Results.NotFound();
        }

        ExamTelemetry.ExamsUpdated.Add(1);
        logger.LogInformation("Updated exam {ExamId}", id);

        return Results.Ok(ToExamResponse(exam));
    }

    private static IResult Delete(Guid id, ExamStore store, ILogger<ExamStore> logger)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("DeleteExam");
        activity?.SetTag("exam.id", id.ToString());

        if (!store.Delete(id))
        {
            logger.LogWarning("Exam {ExamId} not found for deletion", id);
            return Results.NotFound();
        }

        ExamTelemetry.ExamsDeleted.Add(1);
        logger.LogInformation("Deleted exam {ExamId}", id);

        return Results.NoContent();
    }

    private static ExamResponse ToExamResponse(Exam exam)
    {
        return new ExamResponse(
            exam.Id,
            exam.PatientId,
            exam.Type,
            exam.ScheduledDate,
            exam.Status,
            exam.Results,
            exam.Notes,
            [
                new Link("self", $"/api/exams/{exam.Id}", "GET"),
                new Link("update", $"/api/exams/{exam.Id}", "PUT"),
                new Link("delete", $"/api/exams/{exam.Id}", "DELETE"),
                new Link("patient", $"/api/patients/{exam.PatientId}", "GET"),
                new Link("patient-exams", $"/api/patients/{exam.PatientId}/exams", "GET"),
                new Link("collection", "/api/exams", "GET")
            ]);
    }
}
