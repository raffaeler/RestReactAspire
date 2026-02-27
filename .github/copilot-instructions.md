This solution is designed to provide an examplar tutorial of the REST architectural style.
The design must be HATEOS compliant, and the API must be designed to be easily navigable by clients.

## Scenario
The scenario is a fictious day-hospital management system, where patients can be admitted, treated, and discharged. The system must allow for the management of patient records, including personal information, medical history, and treatment plans.
APIs and UI must implement the following features:
Step 1. Patient data management: Create, Read, Update, and Delete (CRUD) operations for patient records.
Step 2. Exams management: CRUD operations for medical exams, including scheduling and results.
Step 3. Doctors management: CRUD operations for doctor records, including their specialties and schedules. They can be assigned to exams and eventually changed if needed.
Step 4. Change the in-memory storage with LiteDB, to persist the data.
Step 5. Add an admin page that allow to populate the db with sample data, and to reset the database. The sample data is composed by 20 patients, 10 doctors and 30 exams, with random but meaningful data that is correctly related (e.g., patients have medical history, exams are assigned to doctors and patients, etc.).
Step 6. Modify all the lists on the front-end to paginate the results, to avoid loading all the data at once and to improve the performance of the application. The pagination should be implemented on the server-side, and the API should support query parameters for page number and page size. The front-end should display pagination controls to allow users to navigate through the pages of results.
Step 7. On each page, implement a search functionality that allows users to filter the results based on specific criteria (e.g., search patients by name, search exams by date, etc.). The search should be implemented on the server-side, and the API should support query parameters for the search criteria. The front-end should display a search input and a button to trigger the search, and the results should be updated accordingly.
Step 8. All the pages should have a pre-defined order for the displayed items, based on a specific field: patients should be ordered by name, doctors by speciality and then by name, exams by date. In addition to enforcing these default, provide the possibility for the user to change the order by clicking on the column headers and supporting the ascending/descending order. Ordering must be done on the backend, and the API should support query parameters for the ordering criteria and direction. The front-end should display indicators for the current ordering and allow users to change it by clicking on the column headers.
Step 9. Add the possibility to set the duration to the Exams, and to calculate the end time of the exam based on the start time and the duration. The API should support this functionality, and the front-end should display the end time of the exam accordingly. Revise the seeding of the sample data to include the duration and end time for the exams, and ensure that the relationships between patients, doctors, and exams are correctly maintained.
Step 10. Verify that OpenTelemetry instrumentation is already added to all the API, including Traces (Activities), Metrics and Logs. The instrumentation should be designed to provide insights into the performance and behavior of the API, and to help identify and troubleshoot issues.

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
