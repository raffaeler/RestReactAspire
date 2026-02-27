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

    private static IResult GetAll(PatientStore store, ILogger<PatientStore> logger)
    {
        using var activity = PatientTelemetry.ActivitySource.StartActivity("GetAllPatients");

        logger.LogInformation("Retrieving all patients");

        var patients = store.GetAll();
        activity?.SetTag("patient.count", patients.Count);

        var items = patients.Select(ToPatientResponse).ToList();
        var response = new PatientListResponse(items, [
            new Link("self", "/api/patients", "GET"),
            new Link("create", "/api/patients", "POST")
        ]);

        return Results.Ok(response);
    }

    private static IResult GetById(Guid id, PatientStore store, ILogger<PatientStore> logger)
    {
        using var activity = PatientTelemetry.ActivitySource.StartActivity("GetPatientById");
        activity?.SetTag("patient.id", id.ToString());

        var patient = store.GetById(id);
        if (patient is null)
        {
            logger.LogWarning("Patient {PatientId} not found", id);
            return Results.NotFound();
        }

        logger.LogInformation("Retrieved patient {PatientId}", id);
        return Results.Ok(ToPatientResponse(patient));
    }

    private static IResult Create(CreatePatientRequest request, PatientStore store, ILogger<PatientStore> logger)
    {
        using var activity = PatientTelemetry.ActivitySource.StartActivity("CreatePatient");

        var patient = store.Add(request);
        activity?.SetTag("patient.id", patient.Id.ToString());
        PatientTelemetry.PatientsCreated.Add(1);

        logger.LogInformation("Created patient {PatientId}: {FirstName} {LastName}",
            patient.Id, patient.FirstName, patient.LastName);

        return Results.Created($"/api/patients/{patient.Id}", ToPatientResponse(patient));
    }

    private static IResult Update(Guid id, UpdatePatientRequest request, PatientStore store, ILogger<PatientStore> logger)
    {
        using var activity = PatientTelemetry.ActivitySource.StartActivity("UpdatePatient");
        activity?.SetTag("patient.id", id.ToString());

        var patient = store.Update(id, request);
        if (patient is null)
        {
            logger.LogWarning("Patient {PatientId} not found for update", id);
            return Results.NotFound();
        }

        PatientTelemetry.PatientsUpdated.Add(1);
        logger.LogInformation("Updated patient {PatientId}", id);

        return Results.Ok(ToPatientResponse(patient));
    }

    private static IResult Delete(Guid id, PatientStore store, ILogger<PatientStore> logger)
    {
        using var activity = PatientTelemetry.ActivitySource.StartActivity("DeletePatient");
        activity?.SetTag("patient.id", id.ToString());

        if (!store.Delete(id))
        {
            logger.LogWarning("Patient {PatientId} not found for deletion", id);
            return Results.NotFound();
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
