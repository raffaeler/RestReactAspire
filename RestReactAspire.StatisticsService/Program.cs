using LiteDB;
using RestReactAspire.Infrastructure.Cqrs;
using RestReactAspire.StatisticsService;
using RestReactAspire.StatisticsService.Stores;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

LiteDbFactory.ConfigureMapper();
var liteDbConnectionString = builder.Configuration.GetConnectionString("LiteDb") ?? "Filename=statistics.db;Connection=shared";
builder.Services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase(liteDbConnectionString));

var useInMemoryQueue = builder.Configuration.GetValue("Cqrs:UseInMemoryQueue", builder.Environment.IsEnvironment("Testing"));
var isTesting = useInMemoryQueue;

// In testing mode, use local statistics store (in-memory LiteDB);
// in production, use HTTP clients to query other services
if (isTesting)
{
    builder.Services.AddSingleton<StatisticsStore>();
}
else
{
    builder.Services.AddHttpClient("patients", c => c.BaseAddress = new Uri("http://localhost:5101"));
    builder.Services.AddHttpClient("doctors", c => c.BaseAddress = new Uri("http://localhost:5102"));
    builder.Services.AddHttpClient("exams", c => c.BaseAddress = new Uri("http://localhost:5103"));
}

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<WriteCommandResultCoordinator>();
builder.Services.AddSingleton<IWriteCommandHandler, StatisticsWriteCommandHandler>();
if (useInMemoryQueue)
{
    builder.Services.AddSingleton<IWriteCommandQueue, StatisticsInMemoryWriteCommandQueue>();
}
else
{
    builder.Services.AddSingleton<RabbitMqConnectionManager>();
    builder.Services.AddSingleton<IWriteCommandQueue, RabbitMqWriteCommandQueue>();
    builder.Services.AddHostedService<StatisticsRabbitMqWriteCommandProcessor>();
}

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1", permanent: false));
}

var api = app.MapGroup("/api");
api.MapGroup("statistics").MapStatisticsEndpoints();
api.MapGroup("admin").MapStatisticsAdminEndpoints();
app.MapDefaultEndpoints();
app.Run();
public partial class Program { }
namespace RestReactAspire.StatisticsService { public class StatisticsServiceMarker { } }
