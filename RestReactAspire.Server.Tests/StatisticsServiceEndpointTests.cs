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
        var response = await _client.GetAsync("/api/statistics/patients-by-age-group");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PatientsByAgeGroupResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Contains(result.Links, l => l.Rel == "patients");
        Assert.Contains(result.Links, l => l.Rel == "doctors");
        Assert.Contains(result.Links, l => l.Rel == "exams");
    }

    [Fact]
    public async Task GetExamsPerDoctor_ReturnsOk_WithLinks()
    {
        var response = await _client.GetAsync("/api/statistics/exams-per-doctor");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ExamsPerDoctorResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Contains(result.Links, l => l.Rel == "patients");
    }

    [Fact]
    public async Task GetExamsOverTime_ReturnsOk_WithLinks()
    {
        var response = await _client.GetAsync("/api/statistics/exams-over-time");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ExamsOverTimeResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Contains(result.Links, l => l.Rel == "patients");
    }

    [Fact]
    public async Task GetAvgDurationByExamType_ReturnsOk_WithLinks()
    {
        var response = await _client.GetAsync("/api/statistics/avg-duration-by-exam-type");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AvgDurationByExamTypeResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Contains(result.Links, l => l.Rel == "patients");
    }

    [Fact]
    public async Task AdminSeed_PopulatesAllThreeCollections()
    {
        var response = await _client.PostAsync("/api/admin/seed", null);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SeedResponse>();
        Assert.NotNull(result);
        Assert.True(result.PatientsCreated > 0);
        Assert.True(result.DoctorsCreated > 0);
        Assert.True(result.ExamsCreated > 0);
    }

    [Fact]
    public async Task AdminSeed_ThenStatisticsHaveData()
    {
        await _client.PostAsync("/api/admin/seed", null);

        var ageResponse = await _client.GetAsync("/api/statistics/patients-by-age-group");
        var ageResult = await ageResponse.Content.ReadFromJsonAsync<PatientsByAgeGroupResponse>();
        Assert.NotNull(ageResult);
        Assert.NotEmpty(ageResult.Items);

        var docResponse = await _client.GetAsync("/api/statistics/exams-per-doctor");
        var docResult = await docResponse.Content.ReadFromJsonAsync<ExamsPerDoctorResponse>();
        Assert.NotNull(docResult);
        Assert.NotEmpty(docResult.Items);

        var timeResponse = await _client.GetAsync("/api/statistics/exams-over-time");
        var timeResult = await timeResponse.Content.ReadFromJsonAsync<ExamsOverTimeResponse>();
        Assert.NotNull(timeResult);
        Assert.NotEmpty(timeResult.Items);

        var durResponse = await _client.GetAsync("/api/statistics/avg-duration-by-exam-type");
        var durResult = await durResponse.Content.ReadFromJsonAsync<AvgDurationByExamTypeResponse>();
        Assert.NotNull(durResult);
    }

    [Fact]
    public async Task AdminReset_ClearsAllData()
    {
        await _client.PostAsync("/api/admin/seed", null);
        var resetResponse = await _client.PostAsync("/api/admin/reset", null);
        resetResponse.EnsureSuccessStatusCode();

        var resetResult = await resetResponse.Content.ReadFromJsonAsync<ResetResponse>();
        Assert.NotNull(resetResult);
        Assert.True(resetResult.PatientsDeleted > 0);
        Assert.True(resetResult.DoctorsDeleted > 0);
        Assert.True(resetResult.ExamsDeleted > 0);
    }

    [Fact]
    public async Task AdminStats_ReturnsOk_WithAllCounts()
    {
        await _client.PostAsync("/api/admin/seed", null);
        var response = await _client.GetAsync("/api/admin/stats");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StatsResponse>();
        Assert.NotNull(result);
        Assert.True(result.PatientCount > 0);
        Assert.True(result.DoctorCount > 0);
        Assert.True(result.ExamCount > 0);
        Assert.Contains(result.Links, l => l.Rel == "patients");
        Assert.Contains(result.Links, l => l.Rel == "doctors");
        Assert.Contains(result.Links, l => l.Rel == "exams");
    }
}
