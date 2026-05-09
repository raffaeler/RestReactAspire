# RestReactAspire

This is a demo project showing how to build an application step-by-step using Aspire using the GitHub Copilot capabilities.

## Scenario

A day-hospital system for managing patients, doctors, and medical exams. Features include CRUD operations, server-side pagination/search/sorting, statistics dashboards, seed data management, and full Open Telemetry observability.

## Technology Stack

- Backend: .NET 10, ASP.NET Core Minimal APIs, LiteDB, OpenTelemetry, RabbitMQ
- Frontend: React 19, TypeScript, MUI v7, React Router v7, recharts, Vite
- Orchestration: .NET Aspire

## Recent Upgrade Notes

The solution was refreshed after NuGet and frontend package updates.

### Backend updates

- Updated the RabbitMQ integration to work with `RabbitMQ.Client` 7.x.
- Replaced deprecated synchronous connection and channel APIs with the newer async APIs.
- Removed obsolete connection factory configuration that is no longer supported by the upgraded package.
- Revalidated the backend with a successful solution build and passing server tests.

### Frontend updates

- Updated the statistics page to align with stricter `recharts` 3.x TypeScript typings.
- Reworked the custom doctor-axis tick renderer into a typed component compatible with the upgraded chart library.
- Adjusted tooltip formatter and pie-chart label handling to use the current recharts callback shapes.
- Revalidated the frontend with a successful production build.

## GitHub Copilot Model

The code was entirely developed by GitHub Copilot with the `Claude Opus 4.6` model.

## Time-machine

Open the `copilot-instructions-initial.md` in the `.github` folder.

Each step matches with a `git tag name` so that you can rewind the solution matching the corresponding instructions. As it happens for a real-life project, the `copilot-instructions.md` file evolved over time.

Step 0 correspond to the first commit after creating the solution using the Visual Studio 2026 Insiders Aspire template.

All the subsequent steps matches the git tags.

In Step 12, the copilot generated the Skills and a new `copilot-instructions-proposal.md` which then replaced the `copilot-instructions.md`.
