var builder = DistributedApplication.CreateBuilder(args);

// LavinMQ container (shared message broker)
var lavinMq = builder.AddContainer("lavinmq", "cloudamqp/lavinmq")
    .WithEndpoint(name: "amqp", port: 5672, targetPort: 5672)
    .WithHttpEndpoint(name: "management", port: 15672, targetPort: 15672)
    .WithBindMount(@"H:\VMs\ContainerData\lavinmq", "/tmp/amqp");

// Microservices (ports configured via launchSettings.json: 5101-5104)
var patientService = builder.AddProject<Projects.RestReactAspire_PatientService>("patient-service")
    .WithHttpHealthCheck("/health")
    .WaitFor(lavinMq);

var doctorService = builder.AddProject<Projects.RestReactAspire_DoctorService>("doctor-service")
    .WithHttpHealthCheck("/health")
    .WaitFor(lavinMq);

var examService = builder.AddProject<Projects.RestReactAspire_ExamService>("exam-service")
    .WithHttpHealthCheck("/health")
    .WaitFor(lavinMq);

var statisticsService = builder.AddProject<Projects.RestReactAspire_StatisticsService>("statistics-service")
    .WithHttpHealthCheck("/health")
    .WaitFor(lavinMq);

// Gateway server - waits for all microservices
var server = builder.AddProject<Projects.RestReactAspire_Server>("server")
    .WithHttpHealthCheck("/health")
    .WaitFor(lavinMq)
    .WaitFor(patientService)
    .WaitFor(doctorService)
    .WaitFor(examService)
    .WaitFor(statisticsService)
    .WithReference(patientService)
    .WithReference(doctorService)
    .WithReference(examService)
    .WithReference(statisticsService)
    .WithExternalHttpEndpoints();

// Frontend
var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
