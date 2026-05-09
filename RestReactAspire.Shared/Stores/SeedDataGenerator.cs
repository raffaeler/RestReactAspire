using RestReactAspire.Shared.Models;

namespace RestReactAspire.Shared.Stores;

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

    private static readonly string[] Specialties =
    [
        "Cardiology", "Neurology", "Orthopedics", "Dermatology", "Gastroenterology",
        "Ophthalmology", "Pulmonology", "Endocrinology", "Urology", "Oncology",
        "Rheumatology", "Nephrology", "Hematology", "Infectious Disease", "General Surgery",
    ];

    private static readonly string[] AreaCodes =
    [
        "+39 02", "+39 06", "+39 011", "+39 051", "+39 081",
        "+39 055", "+39 041", "+39 010", "+39 091", "+39 049",
    ];

    private static readonly string[] ExamTypes =
    [
        "Blood Test", "MRI Brain", "X-Ray Chest", "ECG", "Skin Biopsy",
        "Colonoscopy", "Eye Exam", "Spirometry", "Thyroid Panel", "Ultrasound",
        "Urinalysis", "Mammography", "CT Scan", "Bone Density Scan", "Stress Test",
    ];

    private static readonly Dictionary<string, (int MinDuration, int MaxDuration)> ExamDurations = new()
    {
        ["Blood Test"] = (15, 30),
        ["MRI Brain"] = (45, 75),
        ["X-Ray Chest"] = (10, 20),
        ["ECG"] = (15, 25),
        ["Skin Biopsy"] = (25, 50),
        ["Colonoscopy"] = (45, 75),
        ["Eye Exam"] = (20, 40),
        ["Spirometry"] = (20, 40),
        ["Thyroid Panel"] = (15, 25),
        ["Ultrasound"] = (30, 60),
        ["Urinalysis"] = (10, 20),
        ["Mammography"] = (20, 40),
        ["CT Scan"] = (30, 60),
        ["Bone Density Scan"] = (30, 50),
        ["Stress Test"] = (60, 120),
    };

    private static readonly string[] Statuses = ["Completed", "Scheduled", "Cancelled"];

    private static readonly Dictionary<string, string[]> CompletedResults = new()
    {
        ["Blood Test"] = [
            "Cholesterol: 210 mg/dL, HDL: 55, LDL: 130. Slightly elevated.",
            "HbA1c: 6.1%, Glucose: 108 mg/dL. Pre-diabetic range.",
            "WBC: 5200/uL, RBC: 4.5M/uL, Platelets: 220K. All within range.",
            "Iron: 45 mcg/dL. Low iron levels detected.",
            "All values within normal range. No abnormalities.",
        ],
        ["MRI Brain"] = [
            "No abnormalities detected. Brain structures within normal limits.",
            "Small white matter lesion noted. Clinical correlation recommended.",
            "Normal MRI. No evidence of mass or hemorrhage.",
        ],
        ["X-Ray Chest"] = [
            "No fractures or lesions. Lung fields clear.",
            "Mild degenerative changes in lumbar spine. No acute findings.",
            "Chest X-ray normal. Heart size within normal limits.",
        ],
        ["ECG"] = [
            "Normal sinus rhythm. No arrhythmia detected.",
            "Sinus bradycardia. Rate 52 bpm. No ST changes.",
            "Normal ECG. Heart rate 72 bpm.",
        ],
        ["Skin Biopsy"] = [
            "Benign nevus confirmed. No malignancy.",
            "Seborrheic keratosis. Benign finding.",
            "Mild dermatitis. No dysplasia.",
        ],
        ["Colonoscopy"] = [
            "No polyps found. Colon mucosa appears healthy.",
            "Two small hyperplastic polyps removed. Benign.",
            "Normal colonoscopy. No abnormalities.",
        ],
        ["Eye Exam"] = [
            "Visual acuity 20/25 both eyes. Mild astigmatism.",
            "Intraocular pressure: 18 mmHg. Optic nerve healthy.",
            "Visual acuity 20/20. No pathology detected.",
        ],
        ["Spirometry"] = [
            "FEV1: 78% predicted. Mild obstructive pattern.",
            "FEV1: 92% predicted. Normal lung function.",
            "FEV1: 85% predicted. Borderline normal.",
        ],
        ["Thyroid Panel"] = [
            "TSH: 4.8 mIU/L, Free T4: 0.9 ng/dL. Borderline hypothyroid.",
            "TSH: 2.1 mIU/L, Free T4: 1.2 ng/dL. Normal thyroid function.",
            "TSH: 0.3 mIU/L. Slightly hyperthyroid. Follow-up recommended.",
        ],
        ["Ultrasound"] = [
            "Kidney ultrasound normal. No stones or obstruction.",
            "Liver ultrasound normal. No focal lesions.",
            "Abdominal ultrasound unremarkable.",
        ],
        ["Urinalysis"] = [
            "No infection markers detected. Normal urinalysis.",
            "Mild proteinuria. Repeat in 3 months.",
            "Normal urinalysis. No abnormalities.",
        ],
        ["Mammography"] = [
            "No suspicious masses identified. BIRADS 1.",
            "Dense breast tissue. BIRADS 2. Benign finding.",
            "Normal mammography. No abnormalities.",
        ],
        ["CT Scan"] = [
            "CT Head normal. No hemorrhage or mass effect.",
            "CT Chest: No pulmonary embolism. Lungs clear.",
            "CT Abdomen normal. No acute findings.",
        ],
        ["Bone Density Scan"] = [
            "T-score: -1.2. Osteopenia detected.",
            "T-score: 0.5. Normal bone density.",
            "T-score: -2.1. Osteoporosis. Treatment recommended.",
        ],
        ["Stress Test"] = [
            "Normal exercise tolerance. No ischemic changes.",
            "Adequate exercise capacity. Mildly reduced recovery.",
            "Stress test normal. Good functional capacity.",
        ],
    };

    private static readonly string[] ExamNotes =
    [
        "Routine check-up.",
        "Follow-up examination.",
        "Patient referred by primary care physician.",
        "Annual screening.",
        "Patient reports persistent symptoms.",
        "Pre-operative evaluation.",
        "Post-treatment monitoring.",
        "Family history screening.",
        "Preventive health check.",
        "Patient requested evaluation.",
    ];

    public static List<Patient> GeneratePatients()
    {
        var rng = new Random(42);
        var patients = new List<Patient>(100);

        for (int i = 0; i < 100; i++)
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
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = new DateOnly(year, month, day),
                Email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant().Replace(" ", "")}_{i}@email.com",
                Phone = $"{areaCode} {phoneNumber}",
            });
        }

        return patients;
    }

    public static List<Doctor> GenerateDoctors()
    {
        var rng = new Random(123);
        var doctors = new List<Doctor>(30);

        for (int i = 0; i < 30; i++)
        {
            var firstName = FirstNames[rng.Next(FirstNames.Length)];
            var lastName = LastNames[rng.Next(LastNames.Length)];
            var specialty = Specialties[i % Specialties.Length];

            doctors.Add(new Doctor
            {
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                Specialty = specialty,
                Email = $"{firstName[0].ToString().ToLowerInvariant()}.{lastName.ToLowerInvariant().Replace(" ", "")}_{i}@hospital.com",
                Phone = $"+39 02 500{i + 1:D4}",
            });
        }

        return doctors;
    }

    public static List<Exam> GenerateExams(List<Patient> patients, List<Doctor> doctors)
    {
        var rng = new Random(999);
        var baseDate = DateOnly.FromDateTime(DateTime.Today);
        var exams = new List<Exam>(200);

        for (int i = 0; i < 200; i++)
        {
            var patient = patients[rng.Next(patients.Count)];
            var doctor = doctors[rng.Next(doctors.Count)];
            var examType = ExamTypes[rng.Next(ExamTypes.Length)];
            var dayOffset = rng.Next(-365, 61);
            var scheduledDate = baseDate.AddDays(dayOffset);
            var (minDur, maxDur) = ExamDurations[examType];
            var durationMinutes = rng.Next(minDur, maxDur + 1);

            string status;
            if (dayOffset < -7)
                status = rng.NextDouble() < 0.9 ? "Completed" : "Cancelled";
            else if (dayOffset > 7)
                status = rng.NextDouble() < 0.95 ? "Scheduled" : "Cancelled";
            else
                status = Statuses[rng.Next(Statuses.Length)];

            var hour = rng.Next(7, 17);
            var minute = (rng.Next(0, 4)) * 15;
            TimeOnly? scheduledTime = status == "Cancelled" && rng.NextDouble() < 0.3
                ? null
                : new TimeOnly(hour, minute);

            int? duration = scheduledTime.HasValue ? durationMinutes : null;

            string? results = null;
            if (status == "Completed" && CompletedResults.TryGetValue(examType, out var resultOptions))
            {
                results = resultOptions[rng.Next(resultOptions.Length)];
            }

            string? notes = ExamNotes[rng.Next(ExamNotes.Length)];

            exams.Add(new Exam
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Type = examType,
                ScheduledDate = scheduledDate,
                ScheduledTime = scheduledTime,
                DurationMinutes = duration,
                Status = status,
                Results = results,
                Notes = notes,
            });
        }

        return exams;
    }
}
