This solution is designed to provide an examplar tutorial of the REST architectural style.
The design must be HATEOS compliant, and the API must be designed to be easily navigable by clients.

## Scenario
The scenario is a fictious day-hospital management system, where patients can be admitted, treated, and discharged. The system must allow for the management of patient records, including personal information, medical history, and treatment plans.
APIs and UI must implement the following features:
Step 1. Patient data management: Create, Read, Update, and Delete (CRUD) operations for patient records.
Step 2. Exams management: CRUD operations for medical exams, including scheduling and results.
Step 3. Doctors management: CRUD operations for doctor records, including their specialties and schedules. They can be assigned to exams and eventually changed if needed.
Step 4. Change the in-memory storage with LiteDB, to persist the data.

## Technology stack
- Backend
  - .NET 10, ASP.NET Core Web API
  - Minimal APIs
  - Aspire
  - Test suite using xUnit
  - LiteDB (https://github.com/litedb-org/LiteDB) for data storage, to avoid evolving the data over schema changes, and to keep the solution simple and self-contained.
- Frontend
  - React with TypeScript
  - MUI components
  - React Router to navigate between pages
  - Centralized client to consume the API, discovered according to the HATEOS principles.

## Technical Requirements
- The API must be designed using HATEOS REST principles, including the use of HTTP methods (GET, POST, PUT, DELETE) and status codes.
- All the APIs must be instrumented with Traces (Activities), Metrics and Logs, using OpenTelemetry.
