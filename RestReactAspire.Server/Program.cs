using RestReactAspire.Server.Endpoints;
using RestReactAspire.Server.Stores;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register application services
builder.Services.AddSingleton<PatientStore>();

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

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

public partial class Program { }
