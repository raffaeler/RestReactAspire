using System.Net;
using System.Net.Http.Json;
using RestReactAspire.Server.Models;

namespace RestReactAspire.Server.Tests;

public class PatientEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PatientEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetApiRoot_ReturnsOk_WithLinks()
    {
        var response = await _client.GetAsync("/api");
        response.EnsureSuccessStatusCode();

        var root = await response.Content.ReadFromJsonAsync<ApiRootResponse>();
        Assert.NotNull(root);
        Assert.Contains(root.Links, l => l.Rel == "self");
        Assert.Contains(root.Links, l => l.Rel == "patients");
    }

    [Fact]
    public async Task GetPatients_ReturnsOk_WithValidStructure()
    {
        var response = await _client.GetAsync("/api/patients");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<PatientListResponse>();
        Assert.NotNull(list);
        Assert.NotNull(list.Items);
        Assert.Contains(list.Links, l => l.Rel == "self");
        Assert.Contains(list.Links, l => l.Rel == "create");
    }

    [Fact]
    public async Task CreatePatient_ReturnsCreated_WithHateoasLinks()
    {
        var request = new CreatePatientRequest("Integration", "Test", new DateOnly(1990, 5, 20), "test@example.com", "555-9999");
        var response = await _client.PostAsJsonAsync("/api/patients", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var patient = await response.Content.ReadFromJsonAsync<PatientResponse>();
        Assert.NotNull(patient);
        Assert.Equal("Integration", patient.FirstName);
        Assert.Equal("Test", patient.LastName);
        Assert.Contains(patient.Links, l => l.Rel == "self");
        Assert.Contains(patient.Links, l => l.Rel == "update");
        Assert.Contains(patient.Links, l => l.Rel == "delete");
        Assert.Contains(patient.Links, l => l.Rel == "collection");
    }

    [Fact]
    public async Task GetPatientById_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.GetAsync($"/api/patients/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndGetPatient_RoundTrips()
    {
        var request = new CreatePatientRequest("Round", "Trip", new DateOnly(2000, 12, 25), "round@example.com", "555-0000");
        var createResponse = await _client.PostAsJsonAsync("/api/patients", request);
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<PatientResponse>();
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/patients/{created.Id}");
        getResponse.EnsureSuccessStatusCode();

        var retrieved = await getResponse.Content.ReadFromJsonAsync<PatientResponse>();
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("Round", retrieved.FirstName);
    }

    [Fact]
    public async Task UpdatePatient_ReturnsOk_WithUpdatedData()
    {
        var createReq = new CreatePatientRequest("Update", "Me", new DateOnly(1995, 3, 15), "update@example.com", "555-1111");
        var createResp = await _client.PostAsJsonAsync("/api/patients", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<PatientResponse>();
        Assert.NotNull(created);

        var updateReq = new UpdatePatientRequest("Updated", "Name", new DateOnly(1995, 3, 15), "updated@example.com", "555-2222");
        var updateResp = await _client.PutAsJsonAsync($"/api/patients/{created.Id}", updateReq);
        updateResp.EnsureSuccessStatusCode();

        var updated = await updateResp.Content.ReadFromJsonAsync<PatientResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.FirstName);
        Assert.Equal("updated@example.com", updated.Email);
    }

    [Fact]
    public async Task DeletePatient_ReturnsNoContent()
    {
        var request = new CreatePatientRequest("Delete", "Me", new DateOnly(1985, 7, 4), "delete@example.com", "555-3333");
        var createResp = await _client.PostAsJsonAsync("/api/patients", request);
        var created = await createResp.Content.ReadFromJsonAsync<PatientResponse>();
        Assert.NotNull(created);

        var deleteResp = await _client.DeleteAsync($"/api/patients/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var getResp = await _client.GetAsync($"/api/patients/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task UpdatePatient_ReturnsNotFound_WhenMissing()
    {
        var request = new UpdatePatientRequest("No", "One", new DateOnly(2000, 1, 1), "no@example.com", "555-0000");
        var response = await _client.PutAsJsonAsync($"/api/patients/{Guid.NewGuid()}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeletePatient_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.DeleteAsync($"/api/patients/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
