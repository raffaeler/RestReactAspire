using System.Net;
using System.Net.Http.Json;
using RestReactAspire.PatientService.Models;

namespace RestReactAspire.Server.Tests;

public class PatientServiceEndpointTests : IClassFixture<TestWebApplicationFactory<RestReactAspire.PatientService.PatientServiceMarker>>
{
    private readonly HttpClient _client;

    public PatientServiceEndpointTests(TestWebApplicationFactory<RestReactAspire.PatientService.PatientServiceMarker> factory)
    {
        _client = factory.CreateClient();
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

    [Fact]
    public async Task GetPatients_WithSearch_ReturnsFilteredResults()
    {
        await _client.PostAsJsonAsync("/api/patients", new CreatePatientRequest("SearchAlpha", "One", new DateOnly(1990, 1, 1), "alpha@example.com", "555-0001"));
        await _client.PostAsJsonAsync("/api/patients", new CreatePatientRequest("SearchBeta", "Two", new DateOnly(1991, 2, 2), "beta@example.com", "555-0002"));

        var response = await _client.GetAsync("/api/patients?search=SearchAlpha");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<PatientListResponse>();
        Assert.NotNull(list);
        Assert.All(list.Items, p => Assert.Contains("SearchAlpha", p.FirstName));
    }

    [Fact]
    public async Task GetPatients_WithSearch_ReturnsEmptyWhenNoMatch()
    {
        var response = await _client.GetAsync("/api/patients?search=ZZZNonExistent");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<PatientListResponse>();
        Assert.NotNull(list);
        Assert.Empty(list.Items);
        Assert.Equal(0, list.Pagination.TotalCount);
    }

    [Fact]
    public async Task GetPatients_DefaultSort_ReturnsSortInfo()
    {
        var response = await _client.GetAsync("/api/patients");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<PatientListResponse>();
        Assert.NotNull(list);
        Assert.Equal("lastName", list.Sort.SortBy);
        Assert.Equal("asc", list.Sort.SortDirection);
    }

    [Fact]
    public async Task GetPatients_WithSortParams_ReturnsSortedResults()
    {
        var req1 = new CreatePatientRequest("SortAlpha", "Zebra", new DateOnly(1990, 1, 1), "sortalpha@example.com", "555-8001");
        var req2 = new CreatePatientRequest("SortBeta", "Alpha", new DateOnly(1991, 2, 2), "sortbeta@example.com", "555-8002");
        var create1 = await _client.PostAsJsonAsync("/api/patients", req1);
        var p1 = await create1.Content.ReadFromJsonAsync<PatientResponse>();
        var create2 = await _client.PostAsJsonAsync("/api/patients", req2);
        var p2 = await create2.Content.ReadFromJsonAsync<PatientResponse>();
        Assert.NotNull(p1);
        Assert.NotNull(p2);

        var response = await _client.GetAsync("/api/patients?sortBy=lastName&sortDirection=asc&search=Sort");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<PatientListResponse>();
        Assert.NotNull(list);
        Assert.Equal("lastName", list.Sort.SortBy);
        Assert.Equal("asc", list.Sort.SortDirection);
        Assert.True(list.Items.Count >= 2);
        var sortItems = list.Items.Where(p => p.FirstName.StartsWith("Sort")).ToList();
        Assert.Equal("Alpha", sortItems[0].LastName);
        Assert.Equal("Zebra", sortItems[1].LastName);
    }

    [Fact]
    public async Task GetPatients_WithSortDesc_ReturnsSortedDescending()
    {
        var req1 = new CreatePatientRequest("DescAlpha", "AAA", new DateOnly(1990, 1, 1), "descalpha@example.com", "555-8003");
        var req2 = new CreatePatientRequest("DescBeta", "ZZZ", new DateOnly(1991, 2, 2), "descbeta@example.com", "555-8004");
        var create1 = await _client.PostAsJsonAsync("/api/patients", req1);
        var p1 = await create1.Content.ReadFromJsonAsync<PatientResponse>();
        var create2 = await _client.PostAsJsonAsync("/api/patients", req2);
        var p2 = await create2.Content.ReadFromJsonAsync<PatientResponse>();
        Assert.NotNull(p1);
        Assert.NotNull(p2);

        var response = await _client.GetAsync("/api/patients?sortBy=lastName&sortDirection=desc&search=Desc");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<PatientListResponse>();
        Assert.NotNull(list);
        Assert.Equal("desc", list.Sort.SortDirection);
        var sortItems = list.Items.Where(p => p.FirstName.StartsWith("Desc")).ToList();
        Assert.Equal("ZZZ", sortItems[0].LastName);
        Assert.Equal("AAA", sortItems[1].LastName);
    }

    [Fact]
    public async Task GetPatients_PaginationLinksContainSearchParams()
    {
        for (int i = 0; i < 15; i++)
            await _client.PostAsJsonAsync("/api/patients", new CreatePatientRequest("SortPag", $"Last{i}", new DateOnly(1990, 1, 1), $"sortpag{i}@example.com", $"555-{i:D4}"));

        var response = await _client.GetAsync("/api/patients?search=SortPag&pageSize=10&sortBy=email&sortDirection=desc");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<PatientListResponse>();
        Assert.NotNull(list);
        Assert.All(list.Links.Where(l => l.Rel is "self" or "first" or "last" or "next"), l =>
        {
            Assert.Contains("sortBy=email", l.Href);
            Assert.Contains("sortDirection=desc", l.Href);
        });
    }

    [Fact]
    public async Task GetPatients_WithSearch_PaginationLinksContainSearch()
    {
        for (int i = 0; i < 15; i++)
            await _client.PostAsJsonAsync("/api/patients", new CreatePatientRequest("PagSearch", $"Last{i}", new DateOnly(1990, 1, 1), $"pag{i}@example.com", $"555-{i:D4}"));

        var response = await _client.GetAsync("/api/patients?search=PagSearch&pageSize=10");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<PatientListResponse>();
        Assert.NotNull(list);
        Assert.All(list.Links.Where(l => l.Rel is "self" or "first" or "last" or "next"), l => Assert.Contains("search=PagSearch", l.Href));
    }

    [Fact]
    public async Task AdminSeed_ReturnsOk_WithPatientsCreated()
    {
        var response = await _client.PostAsync("/api/admin/seed", null);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SeedResponse>();
        Assert.NotNull(result);
        Assert.True(result.PatientsCreated > 0);
        Assert.Contains(result.Links, l => l.Rel == "self");
        Assert.Contains(result.Links, l => l.Rel == "patients");
    }

    [Fact]
    public async Task AdminReset_ReturnsOk_WithPatientsDeleted()
    {
        await _client.PostAsync("/api/admin/seed", null);
        var response = await _client.PostAsync("/api/admin/reset", null);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ResetResponse>();
        Assert.NotNull(result);
        Assert.True(result.PatientsDeleted > 0);
        Assert.Contains(result.Links, l => l.Rel == "seed");
    }

    [Fact]
    public async Task AdminStats_ReturnsOk_WithPatientCount()
    {
        await _client.PostAsync("/api/admin/seed", null);
        var response = await _client.GetAsync("/api/admin/stats");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StatsResponse>();
        Assert.NotNull(result);
        Assert.True(result.PatientCount > 0);
        Assert.Contains(result.Links, l => l.Rel == "self");
    }
}
