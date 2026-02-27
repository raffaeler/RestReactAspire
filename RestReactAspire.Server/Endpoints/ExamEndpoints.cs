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
        group.MapPut("/{id:guid}/doctor", AssignDoctor);

        return group;
    }

    public static RouteGroupBuilder MapPatientExamEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetByPatient);

        return group;
    }

    private static IResult GetAll(ExamStore store, ILogger<ExamStore> logger, int page = 1, int pageSize = 10)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("GetAllExams");

        logger.LogInformation("Retrieving exams page {Page} with size {PageSize}", page, pageSize);

        var (exams, totalCount) = store.GetPaged(page, pageSize);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        activity?.SetTag("exam.count", exams.Count);
        activity?.SetTag("exam.totalCount", totalCount);

        var items = exams.Select(ToExamResponse).ToList();
        var pagination = new PaginationInfo(page, pageSize, totalCount, totalPages);
        var links = PaginationLinks.Build("/api/exams", page, pageSize, totalPages,
            new Link("create", "/api/exams", "POST"));
        var response = new ExamListResponse(items, pagination, links);

        return Results.Ok(response);
    }

    private static IResult GetByPatient(Guid patientId, ExamStore store, PatientStore patientStore, ILogger<ExamStore> logger, int page = 1, int pageSize = 10)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("GetExamsByPatient");
        activity?.SetTag("patient.id", patientId.ToString());

        var patient = patientStore.GetById(patientId);
        if (patient is null)
        {
            logger.LogWarning("Patient {PatientId} not found when listing exams", patientId);
            return Results.NotFound();
        }

        logger.LogInformation("Retrieving exams for patient {PatientId} page {Page} with size {PageSize}", patientId, page, pageSize);

        var (exams, totalCount) = store.GetByPatientIdPaged(patientId, page, pageSize);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        activity?.SetTag("exam.count", exams.Count);
        activity?.SetTag("exam.totalCount", totalCount);

        var items = exams.Select(ToExamResponse).ToList();
        var pagination = new PaginationInfo(page, pageSize, totalCount, totalPages);
        var basePath = $"/api/patients/{patientId}/exams";
        var links = PaginationLinks.Build(basePath, page, pageSize, totalPages,
            new Link("create", "/api/exams", "POST"),
            new Link("patient", $"/api/patients/{patientId}", "GET"));
        var response = new ExamListResponse(items, pagination, links);

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

    private static IResult Create(CreateExamRequest request, ExamStore store, PatientStore patientStore, DoctorStore doctorStore, ILogger<ExamStore> logger)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("CreateExam");

        var patient = patientStore.GetById(request.PatientId);
        if (patient is null)
        {
            logger.LogWarning("Patient {PatientId} not found when creating exam", request.PatientId);
            return Results.NotFound();
        }

        if (request.DoctorId.HasValue)
        {
            var doctor = doctorStore.GetById(request.DoctorId.Value);
            if (doctor is null)
            {
                logger.LogWarning("Doctor {DoctorId} not found when creating exam", request.DoctorId);
                return Results.NotFound();
            }
        }

        var exam = store.Add(request);
        activity?.SetTag("exam.id", exam.Id.ToString());
        activity?.SetTag("patient.id", exam.PatientId.ToString());
        if (exam.DoctorId.HasValue) activity?.SetTag("doctor.id", exam.DoctorId.Value.ToString());
        ExamTelemetry.ExamsCreated.Add(1);

        logger.LogInformation("Created exam {ExamId} of type {ExamType} for patient {PatientId}",
            exam.Id, exam.Type, exam.PatientId);

        return Results.Created($"/api/exams/{exam.Id}", ToExamResponse(exam));
    }

    private static IResult Update(Guid id, UpdateExamRequest request, ExamStore store, DoctorStore doctorStore, ILogger<ExamStore> logger)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("UpdateExam");
        activity?.SetTag("exam.id", id.ToString());

        if (request.DoctorId.HasValue)
        {
            var doctor = doctorStore.GetById(request.DoctorId.Value);
            if (doctor is null)
            {
                logger.LogWarning("Doctor {DoctorId} not found when updating exam", request.DoctorId);
                return Results.NotFound();
            }
        }

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

    private static IResult AssignDoctor(Guid id, AssignDoctorRequest request, ExamStore store, DoctorStore doctorStore, ILogger<ExamStore> logger)
    {
        using var activity = ExamTelemetry.ActivitySource.StartActivity("AssignDoctorToExam");
        activity?.SetTag("exam.id", id.ToString());

        if (request.DoctorId.HasValue)
        {
            var doctor = doctorStore.GetById(request.DoctorId.Value);
            if (doctor is null)
            {
                logger.LogWarning("Doctor {DoctorId} not found when assigning to exam {ExamId}", request.DoctorId, id);
                return Results.NotFound();
            }
        }

        var exam = store.AssignDoctor(id, request.DoctorId);
        if (exam is null)
        {
            logger.LogWarning("Exam {ExamId} not found for doctor assignment", id);
            return Results.NotFound();
        }

        ExamTelemetry.ExamsUpdated.Add(1);
        logger.LogInformation("Assigned doctor {DoctorId} to exam {ExamId}", request.DoctorId, id);

        return Results.Ok(ToExamResponse(exam));
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
            exam.Status,
            exam.Results,
            exam.Notes,
            links);
    }
}
