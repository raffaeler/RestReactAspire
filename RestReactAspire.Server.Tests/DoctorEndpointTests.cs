using System.Net;
using System.Net.Http.Json;
using RestReactAspire.Server.Models;

namespace RestReactAspire.Server.Tests;

public class DoctorEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DoctorEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<DoctorResponse> CreateDoctorAsync()
    {
        var request = new CreateDoctorRequest("Test", "Doctor", "General", "test@hospital.com", "555-0000");
        var response = await _client.PostAsJsonAsync("/api/doctors", request);
        response.EnsureSuccessStatusCode();
        var doctor = await response.Content.ReadFromJsonAsync<DoctorResponse>();
        Assert.NotNull(doctor);
        return doctor;
    }

    private async Task<PatientResponse> CreatePatientAsync()
    {
        var request = new CreatePatientRequest("Test", "Patient", new DateOnly(1990, 1, 1), "patient@example.com", "555-0000");
        var response = await _client.PostAsJsonAsync("/api/patients", request);
        response.EnsureSuccessStatusCode();
        var patient = await response.Content.ReadFromJsonAsync<PatientResponse>();
        Assert.NotNull(patient);
        return patient;
    }

    [Fact]
    public async Task GetApiRoot_ReturnsDoctorsLink()
    {
        var response = await _client.GetAsync("/api");
        response.EnsureSuccessStatusCode();

        var root = await response.Content.ReadFromJsonAsync<ApiRootResponse>();
        Assert.NotNull(root);
        Assert.Contains(root.Links, l => l.Rel == "doctors");
    }

    [Fact]
    public async Task GetDoctors_ReturnsOk_WithValidStructure()
    {
        var response = await _client.GetAsync("/api/doctors");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<DoctorListResponse>();
        Assert.NotNull(list);
        Assert.NotNull(list.Items);
        Assert.Contains(list.Links, l => l.Rel == "self");
        Assert.Contains(list.Links, l => l.Rel == "create");
    }

    [Fact]
    public async Task CreateDoctor_ReturnsCreated_WithHateoasLinks()
    {
        var request = new CreateDoctorRequest("Alice", "Cardio", "Cardiology", "alice@hospital.com", "555-1111");

        var response = await _client.PostAsJsonAsync("/api/doctors", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var doctor = await response.Content.ReadFromJsonAsync<DoctorResponse>();
        Assert.NotNull(doctor);
        Assert.Equal("Alice", doctor.FirstName);
        Assert.Equal("Cardiology", doctor.Specialty);
        Assert.Contains(doctor.Links, l => l.Rel == "self");
        Assert.Contains(doctor.Links, l => l.Rel == "update");
        Assert.Contains(doctor.Links, l => l.Rel == "delete");
        Assert.Contains(doctor.Links, l => l.Rel == "exams");
        Assert.Contains(doctor.Links, l => l.Rel == "collection");
    }

    [Fact]
    public async Task GetDoctorById_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.GetAsync($"/api/doctors/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndGetDoctor_RoundTrips()
    {
        var request = new CreateDoctorRequest("Round", "Trip", "Radiology", "round@hospital.com", "555-2222");
        var createResponse = await _client.PostAsJsonAsync("/api/doctors", request);
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<DoctorResponse>();
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/doctors/{created.Id}");
        getResponse.EnsureSuccessStatusCode();

        var retrieved = await getResponse.Content.ReadFromJsonAsync<DoctorResponse>();
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("Round", retrieved.FirstName);
        Assert.Equal("Radiology", retrieved.Specialty);
    }

    [Fact]
    public async Task UpdateDoctor_ReturnsOk_WithUpdatedData()
    {
        var createReq = new CreateDoctorRequest("Update", "Me", "General", "update@hospital.com", "555-3333");
        var createResp = await _client.PostAsJsonAsync("/api/doctors", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<DoctorResponse>();
        Assert.NotNull(created);

        var updateReq = new UpdateDoctorRequest("Updated", "Name", "Surgery", "updated@hospital.com", "555-4444");
        var updateResp = await _client.PutAsJsonAsync($"/api/doctors/{created.Id}", updateReq);
        updateResp.EnsureSuccessStatusCode();

        var updated = await updateResp.Content.ReadFromJsonAsync<DoctorResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.FirstName);
        Assert.Equal("Surgery", updated.Specialty);
        Assert.Equal("updated@hospital.com", updated.Email);
    }

    [Fact]
    public async Task DeleteDoctor_ReturnsNoContent()
    {
        var request = new CreateDoctorRequest("Delete", "Me", "General", "delete@hospital.com", "555-5555");
        var createResp = await _client.PostAsJsonAsync("/api/doctors", request);
        var created = await createResp.Content.ReadFromJsonAsync<DoctorResponse>();
        Assert.NotNull(created);

        var deleteResp = await _client.DeleteAsync($"/api/doctors/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var getResp = await _client.GetAsync($"/api/doctors/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task UpdateDoctor_ReturnsNotFound_WhenMissing()
    {
        var request = new UpdateDoctorRequest("No", "One", "General", "no@hospital.com", "555-0000");
        var response = await _client.PutAsJsonAsync($"/api/doctors/{Guid.NewGuid()}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDoctor_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.DeleteAsync($"/api/doctors/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignDoctor_ReturnsOk_WithDoctorLinks()
    {
        var patient = await CreatePatientAsync();
        var doctor = await CreateDoctorAsync();
        var examReq = new CreateExamRequest(patient.Id, null, "Blood Test", new DateOnly(2025, 6, 15), "Scheduled", null, null);
        var examResp = await _client.PostAsJsonAsync("/api/exams", examReq);
        var exam = await examResp.Content.ReadFromJsonAsync<ExamResponse>();
        Assert.NotNull(exam);

        var assignResp = await _client.PutAsJsonAsync($"/api/exams/{exam.Id}/doctor", new AssignDoctorRequest(doctor.Id));
        assignResp.EnsureSuccessStatusCode();

        var updated = await assignResp.Content.ReadFromJsonAsync<ExamResponse>();
        Assert.NotNull(updated);
        Assert.Equal(doctor.Id, updated.DoctorId);
        Assert.Contains(updated.Links, l => l.Rel == "doctor");
        Assert.Contains(updated.Links, l => l.Rel == "doctor-exams");
    }

    [Fact]
    public async Task AssignDoctor_CanChangeDoctorOnExam()
    {
        var patient = await CreatePatientAsync();
        var doctor1 = await CreateDoctorAsync();
        var doctor2 = await CreateDoctorAsync();

        var examReq = new CreateExamRequest(patient.Id, doctor1.Id, "X-Ray", new DateOnly(2025, 7, 1), "Scheduled", null, null);
        var examResp = await _client.PostAsJsonAsync("/api/exams", examReq);
        var exam = await examResp.Content.ReadFromJsonAsync<ExamResponse>();
        Assert.NotNull(exam);
        Assert.Equal(doctor1.Id, exam.DoctorId);

        var assignResp = await _client.PutAsJsonAsync($"/api/exams/{exam.Id}/doctor", new AssignDoctorRequest(doctor2.Id));
        assignResp.EnsureSuccessStatusCode();

        var updated = await assignResp.Content.ReadFromJsonAsync<ExamResponse>();
        Assert.NotNull(updated);
        Assert.Equal(doctor2.Id, updated.DoctorId);
    }

    [Fact]
    public async Task AssignDoctor_CanUnassignDoctor()
    {
        var patient = await CreatePatientAsync();
        var doctor = await CreateDoctorAsync();

        var examReq = new CreateExamRequest(patient.Id, doctor.Id, "MRI", new DateOnly(2025, 8, 1), "Scheduled", null, null);
        var examResp = await _client.PostAsJsonAsync("/api/exams", examReq);
        var exam = await examResp.Content.ReadFromJsonAsync<ExamResponse>();
        Assert.NotNull(exam);

        var assignResp = await _client.PutAsJsonAsync($"/api/exams/{exam.Id}/doctor", new AssignDoctorRequest(null));
        assignResp.EnsureSuccessStatusCode();

        var updated = await assignResp.Content.ReadFromJsonAsync<ExamResponse>();
        Assert.NotNull(updated);
        Assert.Null(updated.DoctorId);
        Assert.DoesNotContain(updated.Links, l => l.Rel == "doctor");
    }

    [Fact]
    public async Task AssignDoctor_ReturnsNotFound_WhenExamMissing()
    {
        var doctor = await CreateDoctorAsync();
        var response = await _client.PutAsJsonAsync($"/api/exams/{Guid.NewGuid()}/doctor", new AssignDoctorRequest(doctor.Id));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignDoctor_ReturnsNotFound_WhenDoctorMissing()
    {
        var patient = await CreatePatientAsync();
        var examReq = new CreateExamRequest(patient.Id, null, "Blood Test", new DateOnly(2025, 6, 15), "Scheduled", null, null);
        var examResp = await _client.PostAsJsonAsync("/api/exams", examReq);
        var exam = await examResp.Content.ReadFromJsonAsync<ExamResponse>();
        Assert.NotNull(exam);

        var response = await _client.PutAsJsonAsync($"/api/exams/{exam.Id}/doctor", new AssignDoctorRequest(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDoctorExams_ReturnsOnlyDoctorExams()
    {
        var patient = await CreatePatientAsync();
        var doctor1 = await CreateDoctorAsync();
        var doctor2 = await CreateDoctorAsync();

        await _client.PostAsJsonAsync("/api/exams",
            new CreateExamRequest(patient.Id, doctor1.Id, "Blood Test", new DateOnly(2025, 6, 1), "Scheduled", null, null));
        await _client.PostAsJsonAsync("/api/exams",
            new CreateExamRequest(patient.Id, doctor1.Id, "X-Ray", new DateOnly(2025, 6, 2), "Scheduled", null, null));
        await _client.PostAsJsonAsync("/api/exams",
            new CreateExamRequest(patient.Id, doctor2.Id, "MRI", new DateOnly(2025, 6, 3), "Scheduled", null, null));

        var response = await _client.GetAsync($"/api/doctors/{doctor1.Id}/exams");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<ExamListResponse>();
        Assert.NotNull(list);
        Assert.Equal(2, list.Items.Count);
        Assert.All(list.Items, e => Assert.Equal(doctor1.Id, e.DoctorId));
        Assert.Contains(list.Links, l => l.Rel == "doctor");
    }

    [Fact]
    public async Task GetDoctorExams_ReturnsNotFound_WhenDoctorMissing()
    {
        var response = await _client.GetAsync($"/api/doctors/{Guid.NewGuid()}/exams");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDoctors_WithSearch_ReturnsFilteredResults()
    {
        await _client.PostAsJsonAsync("/api/doctors", new CreateDoctorRequest("SearchDoc", "Alpha", "Dermatology", "searchdoc@hospital.com", "555-9001"));
        await _client.PostAsJsonAsync("/api/doctors", new CreateDoctorRequest("OtherDoc", "Beta", "Surgery", "otherdoc@hospital.com", "555-9002"));

        var response = await _client.GetAsync("/api/doctors?search=SearchDoc");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<DoctorListResponse>();
        Assert.NotNull(list);
        Assert.All(list.Items, d => Assert.Contains("SearchDoc", d.FirstName));
    }

    [Fact]
    public async Task GetDoctors_WithSearch_BySpecialty()
    {
        await _client.PostAsJsonAsync("/api/doctors", new CreateDoctorRequest("Doc", "One", "UniqueSpecialty", "doc1@hospital.com", "555-9003"));
        await _client.PostAsJsonAsync("/api/doctors", new CreateDoctorRequest("Doc", "Two", "OtherSpecialty", "doc2@hospital.com", "555-9004"));

        var response = await _client.GetAsync("/api/doctors?search=UniqueSpecialty");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<DoctorListResponse>();
        Assert.NotNull(list);
        Assert.Contains(list.Items, d => d.Specialty == "UniqueSpecialty");
    }
}
