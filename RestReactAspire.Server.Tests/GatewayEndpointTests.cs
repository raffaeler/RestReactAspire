using System.Net.Http.Json;
using RestReactAspire.PatientService.Models;

namespace RestReactAspire.Server.Tests;

public class GatewayEndpointTests : IClassFixture<TestWebApplicationFactory<RestReactAspire.Server.ServerMarker>>
{
    private readonly HttpClient _client;

    public GatewayEndpointTests(TestWebApplicationFactory<RestReactAspire.Server.ServerMarker> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetApiRoot_ReturnsOk_WithAllExpectedLinks()
    {
        var response = await _client.GetAsync("/api");
        response.EnsureSuccessStatusCode();

        var root = await response.Content.ReadFromJsonAsync<ApiRootResponse>();
        Assert.NotNull(root);
        Assert.Contains(root.Links, l => l.Rel == "self");
        Assert.Contains(root.Links, l => l.Rel == "patients");
        Assert.Contains(root.Links, l => l.Rel == "doctors");
        Assert.Contains(root.Links, l => l.Rel == "exams");
        Assert.Contains(root.Links, l => l.Rel == "admin-stats");
        Assert.Contains(root.Links, l => l.Rel == "admin-seed");
        Assert.Contains(root.Links, l => l.Rel == "admin-reset");
        Assert.Contains(root.Links, l => l.Rel == "statistics-patients-by-age-group");
        Assert.Contains(root.Links, l => l.Rel == "statistics-exams-per-doctor");
        Assert.Contains(root.Links, l => l.Rel == "statistics-exams-over-time");
        Assert.Contains(root.Links, l => l.Rel == "statistics-avg-duration-by-exam-type");
    }

    [Fact]
    public async Task GetApiRoot_SelfLink_HasCorrectMethod()
    {
        var response = await _client.GetAsync("/api");
        response.EnsureSuccessStatusCode();

        var root = await response.Content.ReadFromJsonAsync<ApiRootResponse>();
        Assert.NotNull(root);
        var selfLink = root.Links.Single(l => l.Rel == "self");
        Assert.Equal("/api", selfLink.Href);
        Assert.Equal("GET", selfLink.Method);
    }

    [Fact]
    public async Task GetApiRoot_AdminSeedLink_HasCorrectMethod()
    {
        var response = await _client.GetAsync("/api");
        response.EnsureSuccessStatusCode();

        var root = await response.Content.ReadFromJsonAsync<ApiRootResponse>();
        Assert.NotNull(root);
        var seedLink = root.Links.Single(l => l.Rel == "admin-seed");
        Assert.Equal("POST", seedLink.Method);
    }
}
