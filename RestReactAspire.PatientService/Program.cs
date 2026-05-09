using LiteDB;
using RestReactAspire.PatientService;
using RestReactAspire.Shared.Cqrs;
using RestReactAspire.Shared.Stores;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

LiteDbFactory.ConfigureMapper();
var liteDbConnectionString = builder.Configuration.GetConnectionString("LiteDb") ?? "Filename=patient.db;Connection=shared";
builder.Services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase(liteDbConnectionString));

builder.Services.AddSingleton<PatientStore>();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<WriteCommandResultCoordinator>();
builder.Services.AddSingleton<PatientWriteCommandHandler>();

// Register CQRS
var useInMemoryQueue = builder.Configuration.GetValue("Cqrs:UseInMemoryQueue", builder.Environment.IsEnvironment("Testing"));
if (useInMemoryQueue)
{
    builder.Services.AddSingleton<IWriteCommandQueue, PatientInMemoryWriteCommandQueue>();
}
else
{
    builder.Services.AddSingleton<RabbitMqConnectionManager>();
    builder.Services.AddSingleton<IWriteCommandQueue, RabbitMqWriteCommandQueue>();
    builder.Services.AddHostedService<PatientRabbitMqWriteCommandProcessor>();
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
api.MapGroup("patients").MapPatientEndpoints();
api.MapGroup("admin").MapPatientAdminEndpoints();
app.MapDefaultEndpoints();
app.Run();
public partial class Program { }
namespace RestReactAspire.PatientService { public class PatientServiceMarker { } }
