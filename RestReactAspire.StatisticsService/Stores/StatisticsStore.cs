using LiteDB;

namespace RestReactAspire.StatisticsService.Stores;

/// <summary>
/// Provides local data access for statistics in testing mode.
/// Wraps ILiteDatabase to read Patient, Doctor, and Exam collections.
/// </summary>
public sealed class StatisticsStore
{
    private readonly ILiteDatabase _db;

    public StatisticsStore(ILiteDatabase db)
    {
        _db = db;
    }

    public List<Patient> GetAllPatients() =>
        _db.GetCollection<Patient>("patients").FindAll().ToList();

    public List<Doctor> GetAllDoctors() =>
        _db.GetCollection<Doctor>("doctors").FindAll().ToList();

    public List<Exam> GetAllExams() =>
        _db.GetCollection<Exam>("exams").FindAll().ToList();

    public int GetPatientCount() =>
        _db.GetCollection<Patient>("patients").Count();

    public int GetDoctorCount() =>
        _db.GetCollection<Doctor>("doctors").Count();

    public int GetExamCount() =>
        _db.GetCollection<Exam>("exams").Count();
}

// Local entity copies for testing — only used when reading from local LiteDB
public sealed class Patient
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
}

public sealed class Doctor
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
}

public sealed class Exam
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateOnly ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
    public int? DurationMinutes { get; set; }
}
