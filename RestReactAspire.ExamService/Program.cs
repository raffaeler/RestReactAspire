using LiteDB;
using RestReactAspire.ExamService;
using RestReactAspire.ExamService.Stores;
using RestReactAspire.Infrastructure.Cqrs;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

LiteDbFactory.ConfigureMapper();
var liteDbConnectionString = builder.Configuration.GetConnectionString("LiteDb") ?? "Filename=exam.db;Connection=shared";
builder.Services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase(liteDbConnectionString));

builder.Services.AddSingleton<ExamStore>();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<WriteCommandResultCoordinator>();
builder.Services.AddSingleton<ExamWriteCommandHandler>();
builder.Services.AddSingleton<IWriteCommandHandler>(sp => sp.GetRequiredService<ExamWriteCommandHandler>());

var useInMemoryQueue = builder.Configuration.GetValue("Cqrs:UseInMemoryQueue", builder.Environment.IsEnvironment("Testing"));
if (useInMemoryQueue)
{
    builder.Services.AddSingleton<IWriteCommandQueue, ExamInMemoryWriteCommandQueue>();
}
else
{
    builder.Services.AddSingleton<RabbitMqConnectionManager>();
    builder.Services.AddSingleton<IWriteCommandQueue, RabbitMqWriteCommandQueue>();
    builder.Services.AddHostedService<ExamRabbitMqWriteCommandProcessor>();
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
api.MapGroup("exams").MapExamEndpoints();
// Sub-resources: patient exams and doctor exams
api.MapGroup("patients/{patientId:guid}/exams").MapPatientExamEndpoints();
api.MapGroup("doctors/{doctorId:guid}/exams").MapDoctorExamEndpoints();
api.MapGroup("admin").MapExamAdminEndpoints();
app.MapDefaultEndpoints();
app.Run();
public partial class Program { }
namespace RestReactAspire.ExamService { public class ExamServiceMarker { } }
