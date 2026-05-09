using System.Net.Http.Json;
using System.Text.Json;
using RestReactAspire.Server.Endpoints;
using RestReactAspire.Server.Models;
using RestReactAspire.Server.Telemetry;
using Scalar.AspNetCore;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Resolve service addresses from configuration (populated by Aspire service discovery)
// Falls back to localhost ports when running standalone (not via Aspire)
static string GetServiceUrl(IConfiguration config, string serviceName, string fallbackPort)
{
    // Aspire injects URLs via environment variables in various formats:
    //   services__{name}__http__0  →  services:{name}:http:0
    //   services__{name}__https__0  →  services:{name}:https:0
    //   services__{name}__default__0  →  services:{name}:default:0
    string?[] keys =
    [
        config[$"services:{serviceName}:http:0"],
        config[$"services:{serviceName}:https:0"],
        config[$"services:{serviceName}:default:0"],
    ];

    var url = keys.FirstOrDefault(k => !string.IsNullOrEmpty(k));
    if (!string.IsNullOrEmpty(url))
        return url.TrimEnd('/');

    // Fallback for standalone development
    return $"http://localhost:{fallbackPort}";
}

var patientUrl = GetServiceUrl(builder.Configuration, "patient-service", "5101");
var doctorUrl = GetServiceUrl(builder.Configuration, "doctor-service", "5102");
var examUrl = GetServiceUrl(builder.Configuration, "exam-service", "5103");
var statisticsUrl = GetServiceUrl(builder.Configuration, "statistics-service", "5104");

// YARP reverse proxy configured programmatically with resolved service URLs
builder.Services.AddReverseProxy()
    .LoadFromMemory(
        new[]
        {
            new RouteConfig
            {
                RouteId = "patients-route",
                ClusterId = "patient-cluster",
                Match = new RouteMatch { Path = "/api/patients/{**catch-all}" }
            },
            new RouteConfig
            {
                RouteId = "exams-route",
                ClusterId = "exam-cluster",
                Match = new RouteMatch { Path = "/api/exams/{**catch-all}" }
            },
            new RouteConfig
            {
                RouteId = "doctors-route",
                ClusterId = "doctor-cluster",
                Match = new RouteMatch { Path = "/api/doctors/{**catch-all}" }
            },
            new RouteConfig
            {
                RouteId = "statistics-route",
                ClusterId = "statistics-cluster",
                Match = new RouteMatch { Path = "/api/statistics/{**catch-all}" }
            }
        },
        new[]
        {
            new ClusterConfig
            {
                ClusterId = "patient-cluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["patient-service"] = new() { Address = patientUrl }
                }
            },
            new ClusterConfig
            {
                ClusterId = "doctor-cluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["doctor-service"] = new() { Address = doctorUrl }
                }
            },
            new ClusterConfig
            {
                ClusterId = "exam-cluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["exam-service"] = new() { Address = examUrl }
                }
            },
            new ClusterConfig
            {
                ClusterId = "statistics-cluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["statistics-service"] = new() { Address = statisticsUrl }
                }
            }
        });

// HttpClient for admin fan-out calls - use resolved service URLs
builder.Services.AddHttpClient("patients", c => c.BaseAddress = new Uri(patientUrl));
builder.Services.AddHttpClient("doctors", c => c.BaseAddress = new Uri(doctorUrl));
builder.Services.AddHttpClient("exams", c => c.BaseAddress = new Uri(examUrl));
builder.Services.AddHttpClient("statistics", c => c.BaseAddress = new Uri(statisticsUrl));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1", permanent: false));
}

// API root discovery endpoint (direct, not proxied)
var api = app.MapGroup("/api");
api.MapRootEndpoints();

// Admin fan-out endpoints
api.MapPost("admin/seed", async (IHttpClientFactory httpFactory, ILogger<Program> logger) =>
{
    using var activity = AdminTelemetry.ActivitySource.StartActivity("SeedAll");
    AdminTelemetry.SeedExecuted.Add(1);
    logger.LogInformation("Seeding all services...");

    var patientsClient = httpFactory.CreateClient("patients");
    var doctorsClient = httpFactory.CreateClient("doctors");
    var examsClient = httpFactory.CreateClient("exams");
    var statsClient = httpFactory.CreateClient("statistics");

    var pTask = patientsClient.PostAsync("/api/admin/seed", null);
    var dTask = doctorsClient.PostAsync("/api/admin/seed", null);

    await Task.WhenAll(pTask, dTask);

    // Seed exams after patients and doctors (exams reference both)
    var eResponse = await examsClient.PostAsync("/api/admin/seed", null);

    // Seed statistics last (after all data is in place)
    var sResponse = await statsClient.PostAsync("/api/admin/seed", null);

    var pJson = await pTask.Result.Content.ReadFromJsonAsync<JsonDocument>();
    var dJson = await dTask.Result.Content.ReadFromJsonAsync<JsonDocument>();
    var eJson = await eResponse.Content.ReadFromJsonAsync<JsonDocument>();

    int GetInt(JsonDocument? doc, string prop) => doc?.RootElement.TryGetProperty(prop, out var el) == true ? el.GetInt32() : 0;

    var response = new { PatientsCreated = GetInt(pJson, "patientsCreated"), DoctorsCreated = GetInt(dJson, "doctorsCreated"), ExamsCreated = GetInt(eJson, "examsCreated"), Links = new[] { new Link("self", "/api/admin/seed", "POST"), new Link("stats", "/api/admin/stats", "GET") } };
    return Results.Ok(response);
});

api.MapPost("admin/reset", async (IHttpClientFactory httpFactory, ILogger<Program> logger) =>
{
    using var activity = AdminTelemetry.ActivitySource.StartActivity("ResetAll");
    AdminTelemetry.ResetExecuted.Add(1);
    logger.LogInformation("Resetting all services...");

    var patientsClient = httpFactory.CreateClient("patients");
    var doctorsClient = httpFactory.CreateClient("doctors");
    var examsClient = httpFactory.CreateClient("exams");
    var statsClient = httpFactory.CreateClient("statistics");

    var pTask = patientsClient.PostAsync("/api/admin/reset", null);
    var dTask = doctorsClient.PostAsync("/api/admin/reset", null);
    var eTask = examsClient.PostAsync("/api/admin/reset", null);
    var sTask = statsClient.PostAsync("/api/admin/reset", null);

    await Task.WhenAll(pTask, dTask, eTask, sTask);

    var pJson = await pTask.Result.Content.ReadFromJsonAsync<JsonDocument>();
    var dJson = await dTask.Result.Content.ReadFromJsonAsync<JsonDocument>();
    var eJson = await eTask.Result.Content.ReadFromJsonAsync<JsonDocument>();

    int GetInt(JsonDocument? doc, string prop) => doc?.RootElement.TryGetProperty(prop, out var el) == true ? el.GetInt32() : 0;

    var response = new { PatientsDeleted = GetInt(pJson, "patientsDeleted"), DoctorsDeleted = GetInt(dJson, "doctorsDeleted"), ExamsDeleted = GetInt(eJson, "examsDeleted"), Links = new[] { new Link("self", "/api/admin/reset", "POST"), new Link("seed", "/api/admin/seed", "POST") } };
    return Results.Ok(response);
});

api.MapGet("admin/stats", async (IHttpClientFactory httpFactory, ILogger<Program> logger) =>
{
    using var activity = AdminTelemetry.ActivitySource.StartActivity("GetStats");
    AdminTelemetry.StatsQueried.Add(1);
    logger.LogInformation("Getting stats from all services...");

    var patientsClient = httpFactory.CreateClient("patients");
    var doctorsClient = httpFactory.CreateClient("doctors");
    var examsClient = httpFactory.CreateClient("exams");

    var pResponse = await patientsClient.GetAsync("/api/admin/stats");
    var dResponse = await doctorsClient.GetAsync("/api/admin/stats");
    var eResponse = await examsClient.GetAsync("/api/admin/stats");

    var pJson = await pResponse.Content.ReadFromJsonAsync<JsonDocument>();
    var dJson = await dResponse.Content.ReadFromJsonAsync<JsonDocument>();
    var eJson = await eResponse.Content.ReadFromJsonAsync<JsonDocument>();

    int GetInt(JsonDocument? doc, string prop) => doc?.RootElement.TryGetProperty(prop, out var el) == true ? el.GetInt32() : 0;

    var response = new { PatientCount = GetInt(pJson, "patientCount"), DoctorCount = GetInt(dJson, "doctorCount"), ExamCount = GetInt(eJson, "examCount"), Links = new[] { new Link("self", "/api/admin/stats", "GET"), new Link("seed", "/api/admin/seed", "POST") } };
    return Results.Ok(response);
});

// YARP reverse proxy for all other /api/ routes
app.MapReverseProxy();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

public partial class Program { }
namespace RestReactAspire.Server { public class ServerMarker { } }
