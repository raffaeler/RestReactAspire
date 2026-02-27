using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RestReactAspire.Server.Models;

namespace RestReactAspire.Server.Tests;

public class ExamEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ExamEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<PatientResponse> CreatePatientAsync()
    {
        var request = new CreatePatientRequest("Exam", "Patient", new DateOnly(1990, 1, 1), "exam@example.com", "555-0000");
        var response = await _client.PostAsJsonAsync("/api/patients", request);
        response.EnsureSuccessStatusCode();
        var patient = await response.Content.ReadFromJsonAsync<PatientResponse>();
        Assert.NotNull(patient);
        return patient;
    }

    [Fact]
    public async Task GetApiRoot_ReturnsExamsLink()
    {
        var response = await _client.GetAsync("/api");
        response.EnsureSuccessStatusCode();

        var root = await response.Content.ReadFromJsonAsync<ApiRootResponse>();
        Assert.NotNull(root);
        Assert.Contains(root.Links, l => l.Rel == "exams");
    }

    [Fact]
    public async Task GetExams_ReturnsOk_WithValidStructure()
    {
        var response = await _client.GetAsync("/api/exams");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<ExamListResponse>();
        Assert.NotNull(list);
        Assert.NotNull(list.Items);
        Assert.Contains(list.Links, l => l.Rel == "self");
        Assert.Contains(list.Links, l => l.Rel == "create");
    }

    [Fact]
    public async Task CreateExam_ReturnsCreated_WithHateoasLinks()
    {
        var patient = await CreatePatientAsync();
        var request = new CreateExamRequest(patient.Id, null, "Blood Test", new DateOnly(2025, 6, 15), "Scheduled", null, null);

        var response = await _client.PostAsJsonAsync("/api/exams", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var exam = await response.Content.ReadFromJsonAsync<ExamResponse>();
        Assert.NotNull(exam);
        Assert.Equal("Blood Test", exam.Type);
        Assert.Equal(patient.Id, exam.PatientId);
        Assert.Contains(exam.Links, l => l.Rel == "self");
        Assert.Contains(exam.Links, l => l.Rel == "update");
        Assert.Contains(exam.Links, l => l.Rel == "delete");
        Assert.Contains(exam.Links, l => l.Rel == "assign-doctor");
        Assert.Contains(exam.Links, l => l.Rel == "patient");
        Assert.Contains(exam.Links, l => l.Rel == "patient-exams");
        Assert.Contains(exam.Links, l => l.Rel == "collection");
    }

    [Fact]
    public async Task CreateExam_ReturnsNotFound_WhenPatientMissing()
    {
        var request = new CreateExamRequest(Guid.NewGuid(), null, "Blood Test", new DateOnly(2025, 6, 15), "Scheduled", null, null);
        var response = await _client.PostAsJsonAsync("/api/exams", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetExamById_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.GetAsync($"/api/exams/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndGetExam_RoundTrips()
    {
        var patient = await CreatePatientAsync();
        var request = new CreateExamRequest(patient.Id, null, "X-Ray", new DateOnly(2025, 7, 1), "Scheduled", null, "Chest X-Ray");
        var createResponse = await _client.PostAsJsonAsync("/api/exams", request);
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<ExamResponse>();
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/exams/{created.Id}");
        getResponse.EnsureSuccessStatusCode();

        var retrieved = await getResponse.Content.ReadFromJsonAsync<ExamResponse>();
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("X-Ray", retrieved.Type);
        Assert.Equal("Chest X-Ray", retrieved.Notes);
    }

    [Fact]
    public async Task UpdateExam_ReturnsOk_WithUpdatedData()
    {
        var patient = await CreatePatientAsync();
        var createReq = new CreateExamRequest(patient.Id, null, "Blood Test", new DateOnly(2025, 6, 15), "Scheduled", null, null);
        var createResp = await _client.PostAsJsonAsync("/api/exams", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ExamResponse>();
        Assert.NotNull(created);

        var updateReq = new UpdateExamRequest(null, "Blood Test", new DateOnly(2025, 6, 15), "Completed", "Normal levels", "Annual checkup");
        var updateResp = await _client.PutAsJsonAsync($"/api/exams/{created.Id}", updateReq);
        updateResp.EnsureSuccessStatusCode();

        var updated = await updateResp.Content.ReadFromJsonAsync<ExamResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Completed", updated.Status);
        Assert.Equal("Normal levels", updated.Results);
        Assert.Equal("Annual checkup", updated.Notes);
    }

    [Fact]
    public async Task DeleteExam_ReturnsNoContent()
    {
        var patient = await CreatePatientAsync();
        var request = new CreateExamRequest(patient.Id, null, "MRI", new DateOnly(2025, 8, 1), "Scheduled", null, null);
        var createResp = await _client.PostAsJsonAsync("/api/exams", request);
        var created = await createResp.Content.ReadFromJsonAsync<ExamResponse>();
        Assert.NotNull(created);

        var deleteResp = await _client.DeleteAsync($"/api/exams/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var getResp = await _client.GetAsync($"/api/exams/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task UpdateExam_ReturnsNotFound_WhenMissing()
    {
        var request = new UpdateExamRequest(null, "MRI", new DateOnly(2025, 7, 1), "Completed", null, null);
        var response = await _client.PutAsJsonAsync($"/api/exams/{Guid.NewGuid()}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteExam_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.DeleteAsync($"/api/exams/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPatientExams_ReturnsOnlyPatientExams()
    {
        var patient1 = await CreatePatientAsync();
        var patient2 = await CreatePatientAsync();

        await _client.PostAsJsonAsync("/api/exams",
            new CreateExamRequest(patient1.Id, null, "Blood Test", new DateOnly(2025, 6, 1), "Scheduled", null, null));
        await _client.PostAsJsonAsync("/api/exams",
            new CreateExamRequest(patient1.Id, null, "X-Ray", new DateOnly(2025, 6, 2), "Scheduled", null, null));
        await _client.PostAsJsonAsync("/api/exams",
            new CreateExamRequest(patient2.Id, null, "MRI", new DateOnly(2025, 6, 3), "Scheduled", null, null));

        var response = await _client.GetAsync($"/api/patients/{patient1.Id}/exams");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<ExamListResponse>();
        Assert.NotNull(list);
        Assert.Equal(2, list.Items.Count);
        Assert.All(list.Items, e => Assert.Equal(patient1.Id, e.PatientId));
        Assert.Contains(list.Links, l => l.Rel == "patient");
    }

    [Fact]
    public async Task GetPatientExams_ReturnsNotFound_WhenPatientMissing()
    {
        var response = await _client.GetAsync($"/api/patients/{Guid.NewGuid()}/exams");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PatientResponse_ContainsExamsLink()
    {
        var patient = await CreatePatientAsync();
        var response = await _client.GetAsync($"/api/patients/{patient.Id}");
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<PatientResponse>();
        Assert.NotNull(data);
        Assert.Contains(data.Links, l => l.Rel == "exams");
    }
}
