namespace RestReactAspire.StatisticsService.Models;

// For reading patient data from PatientService
internal record PatientSummary(Guid Id, string FirstName, string LastName, DateOnly DateOfBirth);

// For reading doctor data from DoctorService
internal record DoctorSummary(Guid Id, string FirstName, string LastName, string Specialty);

// For reading exam data from ExamService
internal record ExamSummary(Guid Id, Guid PatientId, Guid? DoctorId, string Type, DateOnly ScheduledDate, TimeOnly? ScheduledTime, int? DurationMinutes, string Status, string? Results, string? Notes);
