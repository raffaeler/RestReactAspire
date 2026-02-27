---
name: Aspire Orchestration
description: Configure and manage the .NET Aspire AppHost for local development orchestration.
globs:
  - "RestReactAspire.AppHost/**"
---

# Aspire Orchestration

## Overview
The solution uses **.NET Aspire** to orchestrate the backend API server and frontend during local development.

## AppHost Configuration
Located in `RestReactAspire.AppHost/AppHost.cs`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.RestReactAspire_Server>("server")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
```

## Key Components
- **Server**: ASP.NET Core Minimal API project (`RestReactAspire.Server`).
- **Frontend**: React/Vite app added via `AddViteApp()` with a reference to the server.
- **Health checks**: `/health` and `/alive` endpoints configured in `Extensions.cs`.
- **Service discovery**: Enabled by default via Aspire service defaults.

## Service Defaults (`Extensions.cs`)
Shared configuration applied to all services:
- OpenTelemetry (traces, metrics, logs)
- Health checks (readiness + liveness)
- Service discovery
- HTTP client resilience

## Adding New Services
1. Add the project to the solution.
2. Register it in `AppHost.cs` with `builder.AddProject<T>()`.
3. Add references between services with `.WithReference()`.
4. Ensure the new service calls `builder.AddServiceDefaults()` for consistent telemetry and health checks.
