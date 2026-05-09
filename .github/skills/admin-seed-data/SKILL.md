---
name: Admin and Seed Data
description: Manage database seeding, reset operations, and the admin interface.
globs:
  - "RestReactAspire.Server/Endpoints/AdminEndpoints.cs"
  - "**/Data/SeedDataGenerator.cs"
  - "**/Models/AdminDto.cs"
  - "frontend/src/pages/AdminPage.tsx"
---

# Admin and Seed Data

## Gateway Fan-Out Pattern
Admin endpoints (`/api/admin/seed`, `/api/admin/reset`, `/api/admin/stats`) are handled by the **YARP gateway using a fan-out pattern**:
- The gateway receives the request and fans it out to all microservices.
- **Seed must be sequential**: patients and doctors seeded first (in parallel), then exams (which reference both), then statistics (which queries all three). This ensures referential integrity.
- **Deterministic IDs**: `SeedDataGenerator` uses fixed `Random` seeds (42, 123, 999). All services call the same generator methods, producing identical GUIDs. This is how the ExamService stiches exams to the correct patient and doctor IDs without cross-service calls.
- Each service seeds/resets/queries its own database independently.
- The gateway aggregates responses and returns a combined result to the client.

## Admin API Endpoints
Served by the gateway, registered under `/api/admin`.

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/seed` | POST | Fans out seed to all services; aggregates results |
| `/reset` | POST | Fans out reset to all services; aggregates results |
| `/stats` | GET | Queries all services for counts; aggregates results |

### Response DTOs (per-service `Models/AdminDto.cs`)
- `SeedResponse(int PatientsCreated, int DoctorsCreated, int ExamsCreated, Links)`
- `ResetResponse(int PatientsDeleted, int DoctorsDeleted, int ExamsDeleted, Links)`
- `StatsResponse(int PatientCount, int DoctorCount, int ExamCount, Links)`

## Seed Data Generator
Each microservice owns its own `SeedDataGenerator` in its `Data/` directory (e.g., `PatientService.Data.SeedDataGenerator`). Each microservice calls its own generator to populate its database with the relevant entity subset. Fixed `Random` seeds ensure deterministic, matching GUIDs across services.

### Current Data Volumes
- **100 patients** — random Italian names, varied dates of birth, email, phone
- **30 doctors** — random Italian names, assigned from 15 medical specialties
- **200 exams** — distributed across patients and doctors with realistic types, durations, dates, statuses, results, and notes

### Data Characteristics
- Patient names drawn from pools of 50 first names and 50 last names.
- Doctor specialties: Cardiology, Neurology, Orthopedics, Dermatology, Gastroenterology, Ophthalmology, Pulmonology, Endocrinology, Urology, Oncology, Rheumatology, Nephrology, Hematology, Infectious Disease, General Surgery.
- Exam types: Blood Test, MRI Brain, X-Ray Chest, ECG, Skin Biopsy, Colonoscopy, Eye Exam, Spirometry, Thyroid Panel, Ultrasound, Urinalysis, Mammography, CT Scan, Bone Density Scan, Stress Test.
- Each exam type has min/max duration ranges for realistic `DurationMinutes`.
- Completed exams have type-specific result strings and notes.
- Scheduled dates span a 12-month range for meaningful time-series charts.

### Modifying Seed Data
- Adjust counts in `GeneratePatients()`, `GenerateDoctors()`, `GenerateExams()`.
- Add new name pools, specialties, or exam types to the static arrays.
- Ensure cross-references remain valid (exams reference valid patient and doctor IDs).

## Frontend Admin Page
- Displays current database stats (patient, doctor, exam counts).
- Provides "Seed Database" and "Reset Database" buttons.
- Shows operation results after seeding or resetting.
