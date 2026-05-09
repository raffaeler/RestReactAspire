namespace RestReactAspire.StatisticsService.Data;

using RestReactAspire.StatisticsService.Stores;

/// <summary>
/// Deterministic seed data generator for statistics testing mode.
/// Uses the same Random seeds as the per-service generators for consistent GUIDs.
/// </summary>
public static class SeedDataGenerator
{
    private static readonly string[] FirstNames =
    [
        "Maria", "Luca", "Giulia", "Marco", "Anna", "Paolo", "Sara", "Andrea",
        "Francesca", "Alessandro", "Elena", "Roberto", "Chiara", "Stefano", "Valentina",
        "Giuseppe", "Laura", "Davide", "Silvia", "Matteo", "Sofia", "Federico",
        "Martina", "Riccardo", "Giorgia", "Tommaso", "Eleonora", "Gabriele", "Aurora",
        "Lorenzo", "Camilla", "Simone", "Beatrice", "Daniele", "Alice", "Emanuele",
        "Vittoria", "Nicola", "Ginevra", "Pietro", "Arianna", "Edoardo", "Noemi",
        "Filippo", "Greta", "Giacomo", "Emma", "Leonardo", "Marta", "Antonio",
    ];

    private static readonly string[] LastNames =
    [
        "Rossi", "Bianchi", "Ferrari", "Russo", "Romano", "Colombo", "Ricci", "Marino",
        "Greco", "Bruno", "Gallo", "Conti", "De Luca", "Mancini", "Barbieri",
        "Fontana", "Santoro", "Marini", "Rinaldi", "Caruso", "Ferrara", "Lombardi",
        "Moretti", "Costa", "Giordano", "Pellegrini", "Serra", "Fabbri", "Marchetti",
        "Rizzo", "Monti", "Cattaneo", "Villa", "Martini", "Gatti", "Leone",
        "Longo", "Gentile", "Martinelli", "Vitale", "Basile", "Ferraro", "Guerra",
        "Palumbo", "Esposito", "Silvestri", "Benedetti", "Orlando", "Grassi", "Coppola",
    ];

    private static readonly string[] AreaCodes = ["+39 02", "+39 06", "+39 011", "+39 051", "+39 081", "+39 055", "+39 041", "+39 010", "+39 091", "+39 049"];

    private static readonly string[] Specialties = ["Cardiology", "Neurology", "Orthopedics", "Pediatrics", "Dermatology", "Radiology", "Oncology", "Gastroenterology"];

    private static readonly string[] ExamTypes = ["Blood Test", "X-Ray", "MRI", "CT Scan", "Ultrasound", "ECG", "Endoscopy", "Colonoscopy"];

    public static List<Guid> GeneratePatients()
    {
        var rng = new Random(42);
        var ids = new List<Guid>(100);
        for (int i = 0; i < 100; i++) ids.Add(Guid.NewGuid());
        return ids;
    }

    public static List<Guid> GenerateDoctors()
    {
        var rng = new Random(123);
        var ids = new List<Guid>(30);
        for (int i = 0; i < 30; i++) ids.Add(Guid.NewGuid());
        return ids;
    }

    public static List<Guid> GenerateExams(List<Guid> patientIds, List<Guid> doctorIds)
    {
        var rng = new Random(456);
        var ids = new List<Guid>(200);
        for (int i = 0; i < 200; i++) ids.Add(Guid.NewGuid());
        return ids;
    }

    public static List<Patient> GeneratePatientEntities(List<Guid> ids)
    {
        var rng = new Random(42);
        var patients = new List<Patient>(ids.Count);
        for (int i = 0; i < ids.Count; i++)
        {
            var firstName = FirstNames[rng.Next(FirstNames.Length)];
            var lastName = LastNames[rng.Next(LastNames.Length)];
            var year = rng.Next(1945, 2006);
            var month = rng.Next(1, 13);
            var day = rng.Next(1, DateTime.DaysInMonth(year, month) + 1);
            var areaCode = AreaCodes[rng.Next(AreaCodes.Length)];
            var phoneNumber = rng.Next(1000000, 9999999);

            patients.Add(new Patient
            {
                Id = ids[i],
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = new DateOnly(year, month, day),
            });
        }
        return patients;
    }

    public static List<Doctor> GenerateDoctorEntities(List<Guid> ids)
    {
        var rng = new Random(123);
        var doctors = new List<Doctor>(ids.Count);
        for (int i = 0; i < ids.Count; i++)
        {
            var firstName = FirstNames[rng.Next(FirstNames.Length)];
            var lastName = LastNames[rng.Next(LastNames.Length)];
            doctors.Add(new Doctor
            {
                Id = ids[i],
                FirstName = firstName,
                LastName = lastName,
                Specialty = Specialties[rng.Next(Specialties.Length)],
            });
        }
        return doctors;
    }

    public static List<Exam> GenerateExamEntities(List<Guid> ids, List<Guid> patientIds, List<Guid> doctorIds)
    {
        var rng = new Random(456);
        var exams = new List<Exam>(ids.Count);
        for (int i = 0; i < ids.Count; i++)
        {
            var year = rng.Next(2023, 2026);
            var month = rng.Next(1, 13);
            var day = rng.Next(1, DateTime.DaysInMonth(year, month) + 1);
            exams.Add(new Exam
            {
                Id = ids[i],
                PatientId = patientIds[rng.Next(patientIds.Count)],
                DoctorId = rng.Next(2) == 1 ? doctorIds[rng.Next(doctorIds.Count)] : null,
                Type = ExamTypes[rng.Next(ExamTypes.Length)],
                ScheduledDate = new DateOnly(year, month, day),
                DurationMinutes = rng.Next(15, 121),
            });
        }
        return exams;
    }
}
