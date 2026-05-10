using System.Net;
using System.Net.Http.Json;
using RestReactAspire.ExamService.Models;

namespace RestReactAspire.Server.Tests;

public class ExamServiceEndpointTests : IClassFixture<TestWebApplicationFactory<RestReactAspire.ExamService.ExamServiceMarker>>
{
    private readonly HttpClient _client;

    public ExamServiceEndpointTests(TestWebApplicationFactory<RestReactAspire.ExamService.ExamServiceMarker> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetExams_ReturnsOk_WithValidStructure()
    {
        var response = await _client.GetAsync("/api/exams", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<ExamListResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(list);
        Assert.NotNull(list.Items);
        Assert.Contains(list.Links, l => l.Rel == "self");
        Assert.Contains(list.Links, l => l.Rel == "create");
    }

    [Fact]
    public async Task CreateExam_ReturnsCreated_WithHateoasLinks()
    {
        var request = new CreateExamRequest(Guid.NewGuid(), null, "Blood Test", new DateOnly(2025, 6, 15), new TimeOnly(9, 0), 30, "Scheduled", null, null);

        var response = await _client.PostAsJsonAsync("/api/exams", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var exam = await response.Content.ReadFromJsonAsync<ExamResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(exam);
        Assert.Equal("Blood Test", exam.Type);
        Assert.Contains(exam.Links, l => l.Rel == "self");
        Assert.Contains(exam.Links, l => l.Rel == "update");
        Assert.Contains(exam.Links, l => l.Rel == "delete");
        Assert.Contains(exam.Links, l => l.Rel == "assign-doctor");
        Assert.Contains(exam.Links, l => l.Rel == "patient");
        Assert.Contains(exam.Links, l => l.Rel == "patient-exams");
        Assert.Contains(exam.Links, l => l.Rel == "collection");
    }

    [Fact]
    public async Task GetExamById_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.GetAsync($"/api/exams/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndGetExam_RoundTrips()
    {
        var request = new CreateExamRequest(Guid.NewGuid(), null, "X-Ray", new DateOnly(2025, 7, 1), new TimeOnly(10, 30), 15, "Scheduled", null, "Chest X-Ray");
        var createResponse = await _client.PostAsJsonAsync("/api/exams", request, TestContext.Current.CancellationToken);
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<ExamResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/exams/{created.Id}", TestContext.Current.CancellationToken);
        getResponse.EnsureSuccessStatusCode();

        var retrieved = await getResponse.Content.ReadFromJsonAsync<ExamResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("X-Ray", retrieved.Type);
        Assert.Equal("Chest X-Ray", retrieved.Notes);
    }

    [Fact]
    public async Task UpdateExam_ReturnsOk_WithUpdatedData()
    {
        var createReq = new CreateExamRequest(Guid.NewGuid(), null, "Blood Test", new DateOnly(2025, 6, 15), new TimeOnly(9, 0), 30, "Scheduled", null, null);
        var createResp = await _client.PostAsJsonAsync("/api/exams", createReq, TestContext.Current.CancellationToken);
        var created = await createResp.Content.ReadFromJsonAsync<ExamResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(created);

        var updateReq = new UpdateExamRequest(null, "Blood Test", new DateOnly(2025, 6, 15), new TimeOnly(9, 0), 30, "Completed", "Normal levels", "Annual checkup");
        var updateResp = await _client.PutAsJsonAsync($"/api/exams/{created.Id}", updateReq, TestContext.Current.CancellationToken);
        updateResp.EnsureSuccessStatusCode();

        var updated = await updateResp.Content.ReadFromJsonAsync<ExamResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("Completed", updated.Status);
        Assert.Equal("Normal levels", updated.Results);
        Assert.Equal("Annual checkup", updated.Notes);
    }

    [Fact]
    public async Task DeleteExam_ReturnsNoContent()
    {
        var request = new CreateExamRequest(Guid.NewGuid(), null, "MRI", new DateOnly(2025, 8, 1), new TimeOnly(14, 0), 60, "Scheduled", null, null);
        var createResp = await _client.PostAsJsonAsync("/api/exams", request, TestContext.Current.CancellationToken);
        var created = await createResp.Content.ReadFromJsonAsync<ExamResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(created);

        var deleteResp = await _client.DeleteAsync($"/api/exams/{created.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var getResp = await _client.GetAsync($"/api/exams/{created.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task UpdateExam_ReturnsNotFound_WhenMissing()
    {
        var request = new UpdateExamRequest(null, "MRI", new DateOnly(2025, 7, 1), new TimeOnly(14, 0), 60, "Completed", null, null);
        var response = await _client.PutAsJsonAsync($"/api/exams/{Guid.NewGuid()}", request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteExam_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.DeleteAsync($"/api/exams/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignDoctor_ReturnsOk_WithDoctorLinks()
    {
        var doctorId = Guid.NewGuid();
        var createReq = new CreateExamRequest(Guid.NewGuid(), null, "Blood Test", new DateOnly(2025, 6, 15), new TimeOnly(9, 0), 30, "Scheduled", null, null);
        var examResp = await _client.PostAsJsonAsync("/api/exams", createReq, TestContext.Current.CancellationToken);
        var exam = await examResp.Content.ReadFromJsonAsync<ExamResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(exam);

        var assignResp = await _client.PutAsJsonAsync($"/api/exams/{exam.Id}/doctor", new AssignDoctorRequest(doctorId), TestContext.Current.CancellationToken);
        assignResp.EnsureSuccessStatusCode();

        var updated = await assignResp.Content.ReadFromJsonAsync<ExamResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(doctorId, updated.DoctorId);
        Assert.Contains(updated.Links, l => l.Rel == "doctor");
        Assert.Contains(updated.Links, l => l.Rel == "doctor-exams");
    }

    [Fact]
    public async Task AssignDoctor_CanChangeDoctorOnExam()
    {
        var doctor1Id = Guid.NewGuid();
        var doctor2Id = Guid.NewGuid();

        var examReq = new CreateExamRequest(Guid.NewGuid(), doctor1Id, "X-Ray", new DateOnly(2025, 7, 1), new TimeOnly(10, 0), 15, "Scheduled", null, null);
        var examResp = await _client.PostAsJsonAsync("/api/exams", examReq, TestContext.Current.CancellationToken);
        var exam = await examResp.Content.ReadFromJsonAsync<ExamResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(exam);
        Assert.Equal(doctor1Id, exam.DoctorId);

        var assignResp = await _client.PutAsJsonAsync($"/api/exams/{exam.Id}/doctor", new AssignDoctorRequest(doctor2Id), TestContext.Current.CancellationToken);
        assignResp.EnsureSuccessStatusCode();

        var updated = await assignResp.Content.ReadFromJsonAsync<ExamResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(doctor2Id, updated.DoctorId);
    }

    [Fact]
    public async Task AssignDoctor_CanUnassignDoctor()
    {
        var doctorId = Guid.NewGuid();

        var examReq = new CreateExamRequest(Guid.NewGuid(), doctorId, "MRI", new DateOnly(2025, 8, 1), new TimeOnly(14, 0), 60, "Scheduled", null, null);
        var examResp = await _client.PostAsJsonAsync("/api/exams", examReq, TestContext.Current.CancellationToken);
        var exam = await examResp.Content.ReadFromJsonAsync<ExamResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(exam);

        var assignResp = await _client.PutAsJsonAsync($"/api/exams/{exam.Id}/doctor", new AssignDoctorRequest(null), TestContext.Current.CancellationToken);
        assignResp.EnsureSuccessStatusCode();

        var updated = await assignResp.Content.ReadFromJsonAsync<ExamResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Null(updated.DoctorId);
        Assert.DoesNotContain(updated.Links, l => l.Rel == "doctor");
    }

    [Fact]
    public async Task AssignDoctor_ReturnsNotFound_WhenExamMissing()
    {
        var response = await _client.PutAsJsonAsync($"/api/exams/{Guid.NewGuid()}/doctor", new AssignDoctorRequest(Guid.NewGuid()), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPatientExams_ReturnsList()
    {
        var patientId = Guid.NewGuid();
        var otherPatientId = Guid.NewGuid();

        await _client.PostAsJsonAsync("/api/exams",
            new CreateExamRequest(patientId, null, "Blood Test", new DateOnly(2025, 6, 1), new TimeOnly(8, 0), 20, "Scheduled", null, null), TestContext.Current.CancellationToken);
        await _client.PostAsJsonAsync("/api/exams",
            new CreateExamRequest(patientId, null, "X-Ray", new DateOnly(2025, 6, 2), new TimeOnly(10, 0), 15, "Scheduled", null, null), TestContext.Current.CancellationToken);
        await _client.PostAsJsonAsync("/api/exams",
            new CreateExamRequest(otherPatientId, null, "MRI", new DateOnly(2025, 6, 3), new TimeOnly(14, 0), 60, "Scheduled", null, null), TestContext.Current.CancellationToken);

        var response = await _client.GetAsync($"/api/patients/{patientId}/exams", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<ExamListResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(list);
        Assert.Equal(2, list.Items.Count);
        Assert.All(list.Items, e => Assert.Equal(patientId, e.PatientId));
        Assert.Contains(list.Links, l => l.Rel == "patient");
    }

    [Fact]
    public async Task GetPatientExams_ReturnsEmptyList_ForUnknownPatient()
    {
        var response = await _client.GetAsync($"/api/patients/{Guid.NewGuid()}/exams", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<ExamListResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(list);
        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task GetDoctorExams_ReturnsList()
    {
        var doctorId = Guid.NewGuid();
        var otherDoctorId = Guid.NewGuid();

        await _client.PostAsJsonAsync("/api/exams",
            new CreateExamRequest(Guid.NewGuid(), doctorId, "Blood Test", new DateOnly(2025, 6, 1), new TimeOnly(8, 0), 20, "Scheduled", null, null), TestContext.Current.CancellationToken);
        await _client.PostAsJsonAsync("/api/exams",
            new CreateExamRequest(Guid.NewGuid(), doctorId, "X-Ray", new DateOnly(2025, 6, 2), new TimeOnly(10, 0), 15, "Scheduled", null, null), TestContext.Current.CancellationToken);
        await _client.PostAsJsonAsync("/api/exams",
            new CreateExamRequest(Guid.NewGuid(), otherDoctorId, "MRI", new DateOnly(2025, 6, 3), new TimeOnly(14, 0), 60, "Scheduled", null, null), TestContext.Current.CancellationToken);

        var response = await _client.GetAsync($"/api/doctors/{doctorId}/exams", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<ExamListResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(list);
        Assert.Equal(2, list.Items.Count);
        Assert.All(list.Items, e => Assert.Equal(doctorId, e.DoctorId));
        Assert.Contains(list.Links, l => l.Rel == "doctor");
    }

    [Fact]
    public async Task GetDoctorExams_ReturnsEmptyList_ForUnknownDoctor()
    {
        var response = await _client.GetAsync($"/api/doctors/{Guid.NewGuid()}/exams", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<ExamListResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(list);
        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task GetExams_WithSearch_ReturnsFilteredResults()
    {
        await _client.PostAsJsonAsync("/api/exams", new CreateExamRequest(Guid.NewGuid(), null, "Echocardiogram", new DateOnly(2025, 6, 1), new TimeOnly(9, 0), 45, "Scheduled", null, null), TestContext.Current.CancellationToken);
        await _client.PostAsJsonAsync("/api/exams", new CreateExamRequest(Guid.NewGuid(), null, "Ultrasound", new DateOnly(2025, 6, 2), new TimeOnly(11, 0), 30, "Scheduled", null, null), TestContext.Current.CancellationToken);

        var response = await _client.GetAsync("/api/exams?search=Echocardiogram", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<ExamListResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(list);
        Assert.All(list.Items, e => Assert.Contains("Echocardiogram", e.Type));
    }

    [Fact]
    public async Task GetExams_WithSearch_ByStatus()
    {
        await _client.PostAsJsonAsync("/api/exams", new CreateExamRequest(Guid.NewGuid(), null, "Blood Test", new DateOnly(2025, 9, 1), new TimeOnly(8, 0), 20, "Cancelled", null, null), TestContext.Current.CancellationToken);
        await _client.PostAsJsonAsync("/api/exams", new CreateExamRequest(Guid.NewGuid(), null, "X-Ray", new DateOnly(2025, 9, 2), new TimeOnly(10, 0), 15, "Scheduled", null, null), TestContext.Current.CancellationToken);

        var response = await _client.GetAsync("/api/exams?search=Cancelled", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<ExamListResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(list);
        Assert.Contains(list.Items, e => e.Status == "Cancelled");
    }

    [Fact]
    public async Task GetPatientExams_WithSearch_ReturnsFilteredResults()
    {
        var patientId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/exams", new CreateExamRequest(patientId, null, "Colonoscopy", new DateOnly(2025, 6, 1), new TimeOnly(7, 30), 60, "Scheduled", null, null), TestContext.Current.CancellationToken);
        await _client.PostAsJsonAsync("/api/exams", new CreateExamRequest(patientId, null, "X-Ray", new DateOnly(2025, 6, 2), new TimeOnly(10, 0), 15, "Scheduled", null, null), TestContext.Current.CancellationToken);

        var response = await _client.GetAsync($"/api/patients/{patientId}/exams?search=Colonoscopy", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<ExamListResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(list);
        Assert.All(list.Items, e => Assert.Contains("Colonoscopy", e.Type));
    }

    [Fact]
    public async Task GetExams_DefaultSort_ReturnsSortInfo()
    {
        var response = await _client.GetAsync("/api/exams", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<ExamListResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(list);
        Assert.Equal("scheduledDate", list.Sort.SortBy);
        Assert.Equal("asc", list.Sort.SortDirection);
    }

    [Fact]
    public async Task AdminSeed_ReturnsOk_WithExamsCreated()
    {
        var response = await _client.PostAsync("/api/admin/seed", null, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SeedResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.ExamsCreated > 0);
        Assert.Contains(result.Links, l => l.Rel == "exams");
    }

    [Fact]
    public async Task AdminReset_ReturnsOk_WithExamsDeleted()
    {
        await _client.PostAsync("/api/admin/seed", null, TestContext.Current.CancellationToken);
        var response = await _client.PostAsync("/api/admin/reset", null, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ResetResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.ExamsDeleted > 0);
        Assert.Contains(result.Links, l => l.Rel == "seed");
    }
}
