using System.Net;
using System.Net.Http.Json;
using RestReactAspire.StatisticsService.Models;

namespace RestReactAspire.Server.Tests;

public class StatisticsServiceEndpointTests : IClassFixture<TestWebApplicationFactory<RestReactAspire.StatisticsService.StatisticsServiceMarker>>
{
    private readonly HttpClient _client;

    public StatisticsServiceEndpointTests(TestWebApplicationFactory<RestReactAspire.StatisticsService.StatisticsServiceMarker> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPatientsByAgeGroup_ReturnsOk_WithLinks()
    {
        var response = await _client.GetAsync("/api/statistics/patients-by-age-group", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PatientsByAgeGroupResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Contains(result.Links, l => l.Rel == "patients");
        Assert.Contains(result.Links, l => l.Rel == "doctors");
        Assert.Contains(result.Links, l => l.Rel == "exams");
    }

    [Fact]
    public async Task GetExamsPerDoctor_ReturnsOk_WithLinks()
    {
        var response = await _client.GetAsync("/api/statistics/exams-per-doctor", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ExamsPerDoctorResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Contains(result.Links, l => l.Rel == "patients");
    }

    [Fact]
    public async Task GetExamsOverTime_ReturnsOk_WithLinks()
    {
        var response = await _client.GetAsync("/api/statistics/exams-over-time", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ExamsOverTimeResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Contains(result.Links, l => l.Rel == "patients");
    }

    [Fact]
    public async Task GetAvgDurationByExamType_ReturnsOk_WithLinks()
    {
        var response = await _client.GetAsync("/api/statistics/avg-duration-by-exam-type", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AvgDurationByExamTypeResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Contains(result.Links, l => l.Rel == "patients");
    }

    [Fact]
    public async Task AdminSeed_PopulatesAllThreeCollections()
    {
        var response = await _client.PostAsync("/api/admin/seed", null, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SeedResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.PatientsCreated > 0);
        Assert.True(result.DoctorsCreated > 0);
        Assert.True(result.ExamsCreated > 0);
    }

    [Fact]
    public async Task AdminSeed_ThenStatisticsHaveData()
    {
        await _client.PostAsync("/api/admin/seed", null, TestContext.Current.CancellationToken);

        var ageResponse = await _client.GetAsync("/api/statistics/patients-by-age-group", TestContext.Current.CancellationToken);
        var ageResult = await ageResponse.Content.ReadFromJsonAsync<PatientsByAgeGroupResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(ageResult);
        Assert.NotEmpty(ageResult.Items);

        var docResponse = await _client.GetAsync("/api/statistics/exams-per-doctor", TestContext.Current.CancellationToken);
        var docResult = await docResponse.Content.ReadFromJsonAsync<ExamsPerDoctorResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(docResult);
        Assert.NotEmpty(docResult.Items);

        var timeResponse = await _client.GetAsync("/api/statistics/exams-over-time", TestContext.Current.CancellationToken);
        var timeResult = await timeResponse.Content.ReadFromJsonAsync<ExamsOverTimeResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(timeResult);
        Assert.NotEmpty(timeResult.Items);

        var durResponse = await _client.GetAsync("/api/statistics/avg-duration-by-exam-type", TestContext.Current.CancellationToken);
        var durResult = await durResponse.Content.ReadFromJsonAsync<AvgDurationByExamTypeResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(durResult);
    }

    [Fact]
    public async Task AdminReset_ClearsAllData()
    {
        await _client.PostAsync("/api/admin/seed", null, TestContext.Current.CancellationToken);
        var resetResponse = await _client.PostAsync("/api/admin/reset", null, TestContext.Current.CancellationToken);
        resetResponse.EnsureSuccessStatusCode();

        var resetResult = await resetResponse.Content.ReadFromJsonAsync<ResetResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(resetResult);
        Assert.True(resetResult.PatientsDeleted > 0);
        Assert.True(resetResult.DoctorsDeleted > 0);
        Assert.True(resetResult.ExamsDeleted > 0);
    }

    [Fact]
    public async Task AdminStats_ReturnsOk_WithAllCounts()
    {
        await _client.PostAsync("/api/admin/seed", null, TestContext.Current.CancellationToken);
        var response = await _client.GetAsync("/api/admin/stats", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StatsResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.PatientCount > 0);
        Assert.True(result.DoctorCount > 0);
        Assert.True(result.ExamCount > 0);
        Assert.Contains(result.Links, l => l.Rel == "patients");
        Assert.Contains(result.Links, l => l.Rel == "doctors");
        Assert.Contains(result.Links, l => l.Rel == "exams");
    }
}
