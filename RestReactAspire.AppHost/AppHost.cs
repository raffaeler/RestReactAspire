var builder = DistributedApplication.CreateBuilder(args);

var lavinMq = builder.AddContainer("lavinmq", "cloudamqp/lavinmq")
    .WithEndpoint(name: "amqp", port: 5672, targetPort: 5672)
    .WithEndpoint(name: "management", port: 15672, targetPort: 15672)
    .WithBindMount(@"H:\VMs\ContainerData\lavinmq", "/tmp/amqp");

var server = builder.AddProject<Projects.RestReactAspire_Server>("server")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
