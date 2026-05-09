using LiteDB;
using RestReactAspire.Shared.Cqrs;
using RestReactAspire.Shared.Stores;
using RestReactAspire.StatisticsService;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

LiteDbFactory.ConfigureMapper();
var liteDbConnectionString = builder.Configuration.GetConnectionString("LiteDb") ?? "Filename=statistics.db;Connection=shared";
builder.Services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase(liteDbConnectionString));

// Statistics needs all 3 stores for aggregation
builder.Services.AddSingleton<PatientStore>();
builder.Services.AddSingleton<DoctorStore>();
builder.Services.AddSingleton<ExamStore>();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<WriteCommandResultCoordinator>();
builder.Services.AddSingleton<StatisticsWriteCommandHandler>();

var useInMemoryQueue = builder.Configuration.GetValue("Cqrs:UseInMemoryQueue", builder.Environment.IsEnvironment("Testing"));
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
