using System.Net;
using System.Net.Http.Json;
using RestReactAspire.DoctorService.Models;

namespace RestReactAspire.Server.Tests;

public class DoctorServiceEndpointTests : IClassFixture<TestWebApplicationFactory<RestReactAspire.DoctorService.DoctorServiceMarker>>
{
    private readonly HttpClient _client;

    public DoctorServiceEndpointTests(TestWebApplicationFactory<RestReactAspire.DoctorService.DoctorServiceMarker> factory)
    {
        _client = factory.CreateClient();
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

    [Fact]
    public async Task GetDoctors_DefaultSort_ReturnsSortInfo()
    {
        var response = await _client.GetAsync("/api/doctors");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<DoctorListResponse>();
        Assert.NotNull(list);
        Assert.Equal("specialty", list.Sort.SortBy);
        Assert.Equal("asc", list.Sort.SortDirection);
    }

    [Fact]
    public async Task AdminSeed_ReturnsOk_WithDoctorsCreated()
    {
        var response = await _client.PostAsync("/api/admin/seed", null);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SeedResponse>();
        Assert.NotNull(result);
        Assert.True(result.DoctorsCreated > 0);
        Assert.Contains(result.Links, l => l.Rel == "self");
        Assert.Contains(result.Links, l => l.Rel == "doctors");
    }

    [Fact]
    public async Task AdminReset_ReturnsOk_WithDoctorsDeleted()
    {
        await _client.PostAsync("/api/admin/seed", null);
        var response = await _client.PostAsync("/api/admin/reset", null);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ResetResponse>();
        Assert.NotNull(result);
        Assert.True(result.DoctorsDeleted > 0);
        Assert.Contains(result.Links, l => l.Rel == "seed");
    }

    [Fact]
    public async Task AdminStats_ReturnsOk_WithDoctorCount()
    {
        await _client.PostAsync("/api/admin/seed", null);
        var response = await _client.GetAsync("/api/admin/stats");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StatsResponse>();
        Assert.NotNull(result);
        Assert.True(result.DoctorCount > 0);
        Assert.Contains(result.Links, l => l.Rel == "self");
    }
}
