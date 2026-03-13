using LiteDB;
using RestReactAspire.Server.Cqrs;
using RestReactAspire.Server.Endpoints;
using RestReactAspire.Server.Stores;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register LiteDB
LiteDbFactory.ConfigureMapper();
var liteDbConnectionString = builder.Configuration.GetConnectionString("LiteDb") ?? "Filename=hospital.db;Connection=shared";
builder.Services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase(liteDbConnectionString));

// Register application services
builder.Services.AddSingleton<PatientStore>();
builder.Services.AddSingleton<ExamStore>();
builder.Services.AddSingleton<DoctorStore>();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<WriteCommandResultCoordinator>();
builder.Services.AddSingleton<WriteCommandHandler>();

var useInMemoryQueue = builder.Configuration.GetValue("Cqrs:UseInMemoryQueue", builder.Environment.IsEnvironment("Testing"));
if (useInMemoryQueue)
{
    builder.Services.AddSingleton<IWriteCommandQueue, InMemoryWriteCommandQueue>();
}
else
{
    builder.Services.AddSingleton<RabbitMqConnectionManager>();
    builder.Services.AddSingleton<IWriteCommandQueue, RabbitMqWriteCommandQueue>();
    builder.Services.AddHostedService<RabbitMqWriteCommandProcessor>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var api = app.MapGroup("/api");
api.MapRootEndpoints();
api.MapGroup("patients").MapPatientEndpoints();
api.MapGroup("exams").MapExamEndpoints();
api.MapGroup("patients/{patientId:guid}/exams").MapPatientExamEndpoints();
api.MapGroup("doctors").MapDoctorEndpoints();
api.MapGroup("doctors/{doctorId:guid}/exams").MapDoctorExamEndpoints();
api.MapGroup("admin").MapAdminEndpoints();
api.MapGroup("statistics").MapStatisticsEndpoints();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

public partial class Program { }
