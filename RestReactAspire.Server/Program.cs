using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RestReactAspire.Infrastructure.Cqrs;
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

// RabbitMQ for admin fanout publish (reset broadcasts)
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<RabbitMqConnectionManager>();

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

api.MapPost("admin/reset", async (RabbitMqConnectionManager connectionManager, IOptions<RabbitMqOptions> options, IHttpClientFactory httpFactory, ILogger<Program> logger) =>
{
    using var activity = AdminTelemetry.ActivitySource.StartActivity("ResetAll");
    AdminTelemetry.ResetExecuted.Add(1);
    logger.LogInformation("Resetting all services via fanout exchange...");

    // Snapshot current counts before reset
    var patientsClient = httpFactory.CreateClient("patients");
    var doctorsClient = httpFactory.CreateClient("doctors");
    var examsClient = httpFactory.CreateClient("exams");

    var preP = await patientsClient.GetAsync("/api/admin/stats");
    var preD = await doctorsClient.GetAsync("/api/admin/stats");
    var preE = await examsClient.GetAsync("/api/admin/stats");

    var prePJson = await preP.Content.ReadFromJsonAsync<JsonDocument>();
    var preDJson = await preD.Content.ReadFromJsonAsync<JsonDocument>();
    var preEJson = await preE.Content.ReadFromJsonAsync<JsonDocument>();

    int GetInt(JsonDocument? doc, string prop) => doc?.RootElement.TryGetProperty(prop, out var el) == true ? el.GetInt32() : 0;
    var patientsBefore = GetInt(prePJson, "patientCount");
    var doctorsBefore = GetInt(preDJson, "doctorCount");
    var examsBefore = GetInt(preEJson, "examCount");

    // Publish ResetDataCommand to fanout exchange (all services receive it simultaneously)
    var opts = options.Value;
    var envelope = WriteCommandEnvelope.Create(Guid.NewGuid(), new ResetDataCommand());
    var payload = JsonSerializer.Serialize(envelope);
    var body = Encoding.UTF8.GetBytes(payload);

    using var channel = await connectionManager.GetConnection()
        .CreateChannelAsync(options: default, cancellationToken: CancellationToken.None);

    await channel.ExchangeDeclareAsync(
        opts.AdminResetExchangeName,
        type: ExchangeType.Fanout,
        durable: true,
        autoDelete: false,
        arguments: null,
        passive: false,
        noWait: false,
        cancellationToken: CancellationToken.None);

    await channel.BasicPublishAsync(
        exchange: opts.AdminResetExchangeName,
        routingKey: string.Empty,
        mandatory: false,
        basicProperties: new BasicProperties { Persistent = true },
        body: body,
        cancellationToken: CancellationToken.None);

    logger.LogInformation("Published ResetDataCommand to fanout exchange {Exchange}; {P} patients, {D} doctors, {E} exams deleted",
        opts.AdminResetExchangeName, patientsBefore, doctorsBefore, examsBefore);

    // Poll until all services confirm reset (up to 3 seconds)
    for (int attempt = 0; attempt < 6; attempt++)
    {
        await Task.Delay(500);

        var postP = await patientsClient.GetAsync("/api/admin/stats");
        var postD = await doctorsClient.GetAsync("/api/admin/stats");
        var postE = await examsClient.GetAsync("/api/admin/stats");

        var postPJson = await postP.Content.ReadFromJsonAsync<JsonDocument>();
        var postDJson = await postD.Content.ReadFromJsonAsync<JsonDocument>();
        var postEJson = await postE.Content.ReadFromJsonAsync<JsonDocument>();

        if (GetInt(postPJson, "patientCount") == 0
            && GetInt(postDJson, "doctorCount") == 0
            && GetInt(postEJson, "examCount") == 0)
        {
            logger.LogInformation("Reset confirmed: all services report 0 records after attempt {Attempt}", attempt + 1);
            break;
        }
    }

    var response = new
    {
        PatientsDeleted = patientsBefore,
        DoctorsDeleted = doctorsBefore,
        ExamsDeleted = examsBefore,
        Links = new[] { new Link("self", "/api/admin/reset", "POST"), new Link("seed", "/api/admin/seed", "POST") }
    };
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
