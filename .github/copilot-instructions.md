This solution is designed to provide an examplar tutoria of the REST architectural style.
The design must be HATEOS compliant, and the API must be designed to be easily navigable by clients.

## Scenario
The scenario is a fictious day-hospital management system, where patients can be admitted, treated, and discharged. The system must allow for the management of patient records, including personal information, medical history, and treatment plans.
APIs and UI must implement the following features:
- Patient data management: Create, Read, Update, and Delete (CRUD) operations for patient records.
- Exams management: CRUD operations for medical exams, including scheduling and results.
- Doctors management: CRUD operations for doctor records, including their specialties and schedules.

## Technology stack
- Backend
  - .NET 10, ASP.NET Core Web API
  - Minimal APIs
  - Aspire
  - Test suite using xUnit
- Frontend
  - React with TypeScript
  - Centralized client to consume the API, discovered according to the HATEOS principles.

## Technical Requirements
- The API must be designed using HATEOS REST principles, including the use of HTTP methods (GET, POST, PUT, DELETE) and status codes.
- All the APIs must be instrumented with Traces (Activities), Metrics and Logs, using OpenTelemetry.
