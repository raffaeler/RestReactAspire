using RestReactAspire.Server.Models;

namespace RestReactAspire.Server.Stores;

public static class SeedDataGenerator
{
    public static List<Patient> GeneratePatients()
    {
        return
        [
            new Patient { Id = Guid.NewGuid(), FirstName = "Maria", LastName = "Rossi", DateOfBirth = new DateOnly(1985, 3, 14), Email = "maria.rossi@email.com", Phone = "+39 02 1234567" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Luca", LastName = "Bianchi", DateOfBirth = new DateOnly(1990, 7, 22), Email = "luca.bianchi@email.com", Phone = "+39 06 2345678" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Giulia", LastName = "Ferrari", DateOfBirth = new DateOnly(1978, 11, 5), Email = "giulia.ferrari@email.com", Phone = "+39 011 3456789" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Marco", LastName = "Russo", DateOfBirth = new DateOnly(1965, 1, 30), Email = "marco.russo@email.com", Phone = "+39 051 4567890" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Anna", LastName = "Romano", DateOfBirth = new DateOnly(1992, 9, 18), Email = "anna.romano@email.com", Phone = "+39 081 5678901" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Paolo", LastName = "Colombo", DateOfBirth = new DateOnly(1973, 4, 25), Email = "paolo.colombo@email.com", Phone = "+39 055 6789012" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Sara", LastName = "Ricci", DateOfBirth = new DateOnly(1988, 12, 8), Email = "sara.ricci@email.com", Phone = "+39 041 7890123" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Andrea", LastName = "Marino", DateOfBirth = new DateOnly(1955, 6, 12), Email = "andrea.marino@email.com", Phone = "+39 010 8901234" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Francesca", LastName = "Greco", DateOfBirth = new DateOnly(2000, 2, 28), Email = "francesca.greco@email.com", Phone = "+39 091 9012345" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Alessandro", LastName = "Bruno", DateOfBirth = new DateOnly(1982, 8, 16), Email = "alessandro.bruno@email.com", Phone = "+39 049 0123456" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Elena", LastName = "Gallo", DateOfBirth = new DateOnly(1970, 5, 3), Email = "elena.gallo@email.com", Phone = "+39 02 1122334" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Roberto", LastName = "Conti", DateOfBirth = new DateOnly(1995, 10, 20), Email = "roberto.conti@email.com", Phone = "+39 06 2233445" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Chiara", LastName = "De Luca", DateOfBirth = new DateOnly(1960, 3, 7), Email = "chiara.deluca@email.com", Phone = "+39 011 3344556" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Stefano", LastName = "Mancini", DateOfBirth = new DateOnly(1987, 7, 14), Email = "stefano.mancini@email.com", Phone = "+39 051 4455667" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Valentina", LastName = "Barbieri", DateOfBirth = new DateOnly(1998, 1, 1), Email = "valentina.barbieri@email.com", Phone = "+39 081 5566778" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Giuseppe", LastName = "Fontana", DateOfBirth = new DateOnly(1950, 11, 25), Email = "giuseppe.fontana@email.com", Phone = "+39 055 6677889" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Laura", LastName = "Santoro", DateOfBirth = new DateOnly(1993, 4, 9), Email = "laura.santoro@email.com", Phone = "+39 041 7788990" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Davide", LastName = "Marini", DateOfBirth = new DateOnly(1975, 8, 31), Email = "davide.marini@email.com", Phone = "+39 010 8899001" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Silvia", LastName = "Rinaldi", DateOfBirth = new DateOnly(2002, 6, 17), Email = "silvia.rinaldi@email.com", Phone = "+39 091 9900112" },
            new Patient { Id = Guid.NewGuid(), FirstName = "Matteo", LastName = "Caruso", DateOfBirth = new DateOnly(1968, 12, 22), Email = "matteo.caruso@email.com", Phone = "+39 049 0011223" },
        ];
    }

    public static List<Doctor> GenerateDoctors()
    {
        return
        [
            new Doctor { Id = Guid.NewGuid(), FirstName = "Antonio", LastName = "Verdi", Specialty = "Cardiology", Email = "a.verdi@hospital.com", Phone = "+39 02 5001001" },
            new Doctor { Id = Guid.NewGuid(), FirstName = "Claudia", LastName = "Moretti", Specialty = "Neurology", Email = "c.moretti@hospital.com", Phone = "+39 02 5001002" },
            new Doctor { Id = Guid.NewGuid(), FirstName = "Giovanni", LastName = "Lombardi", Specialty = "Orthopedics", Email = "g.lombardi@hospital.com", Phone = "+39 02 5001003" },
            new Doctor { Id = Guid.NewGuid(), FirstName = "Federica", LastName = "Barbieri", Specialty = "Dermatology", Email = "f.barbieri@hospital.com", Phone = "+39 02 5001004" },
            new Doctor { Id = Guid.NewGuid(), FirstName = "Simone", LastName = "Costa", Specialty = "Gastroenterology", Email = "s.costa@hospital.com", Phone = "+39 02 5001005" },
            new Doctor { Id = Guid.NewGuid(), FirstName = "Elisa", LastName = "Rizzo", Specialty = "Ophthalmology", Email = "e.rizzo@hospital.com", Phone = "+39 02 5001006" },
            new Doctor { Id = Guid.NewGuid(), FirstName = "Lorenzo", LastName = "Marchetti", Specialty = "Pulmonology", Email = "l.marchetti@hospital.com", Phone = "+39 02 5001007" },
            new Doctor { Id = Guid.NewGuid(), FirstName = "Martina", LastName = "Serra", Specialty = "Endocrinology", Email = "m.serra@hospital.com", Phone = "+39 02 5001008" },
            new Doctor { Id = Guid.NewGuid(), FirstName = "Fabio", LastName = "Pellegrini", Specialty = "Urology", Email = "f.pellegrini@hospital.com", Phone = "+39 02 5001009" },
            new Doctor { Id = Guid.NewGuid(), FirstName = "Roberta", LastName = "Fabbri", Specialty = "Oncology", Email = "r.fabbri@hospital.com", Phone = "+39 02 5001010" },
        ];
    }

    public static List<Exam> GenerateExams(List<Patient> patients, List<Doctor> doctors)
    {
        var baseDate = DateOnly.FromDateTime(DateTime.Today);
        var exams = new List<Exam>();

        AddExam(exams, patients[0], doctors[0], "Blood Test", baseDate.AddDays(-30), "Completed",
            "Cholesterol: 210 mg/dL, HDL: 55, LDL: 130. Slightly elevated.",
            "Patient advised dietary changes and follow-up in 3 months.");

        AddExam(exams, patients[0], doctors[0], "Skin Biopsy", baseDate.AddDays(14), "Scheduled",
            null, "Routine cardiac check-up for elevated cholesterol.");

        AddExam(exams, patients[1], doctors[1], "MRI Brain", baseDate.AddDays(-15), "Completed",
            "No abnormalities detected. Brain structures within normal limits.",
            "Patient reported recurring headaches; MRI ordered to rule out pathology.");

        AddExam(exams, patients[2], doctors[2], "X-Ray Chest", baseDate.AddDays(-45), "Completed",
            "No fractures or lesions. Lung fields clear.",
            "Follow-up after minor fall. No further action required.");

        AddExam(exams, patients[2], doctors[2], "Bone Density Scan", baseDate.AddDays(7), "Scheduled",
            null, "Bone density screening recommended due to age and family history.");

        AddExam(exams, patients[3], doctors[0], "ECG", baseDate.AddDays(-20), "Completed",
            "Normal sinus rhythm. No arrhythmia detected.",
            "Annual cardiac screening for patient with hypertension history.");

        AddExam(exams, patients[3], doctors[0], "Stress Test", baseDate.AddDays(10), "Scheduled",
            null, "Cardiac stress test to evaluate exercise tolerance.");

        AddExam(exams, patients[4], doctors[3], "Skin Biopsy", baseDate.AddDays(-10), "Completed",
            "Benign nevus confirmed. No malignancy.",
            "Biopsy of suspicious mole on left forearm.");

        AddExam(exams, patients[5], doctors[4], "Colonoscopy", baseDate.AddDays(-60), "Completed",
            "No polyps found. Colon mucosa appears healthy.",
            "Routine colonoscopy screening. Next in 5 years.");

        AddExam(exams, patients[5], doctors[4], "Blood Test", baseDate.AddDays(21), "Scheduled",
            null, "Follow-up blood panel for iron levels after gastro evaluation.");

        AddExam(exams, patients[6], doctors[5], "Eye Exam", baseDate.AddDays(-5), "Completed",
            "Visual acuity 20/25 both eyes. Mild astigmatism.",
            "Annual eye exam. Prescription updated.");

        AddExam(exams, patients[7], doctors[6], "Spirometry", baseDate.AddDays(-90), "Completed",
            "FEV1: 78% predicted. Mild obstructive pattern.",
            "Patient is a former smoker. Pulmonary function monitoring.");

        AddExam(exams, patients[7], doctors[6], "Spirometry", baseDate.AddDays(30), "Scheduled",
            null, "Follow-up spirometry to assess response to inhaler therapy.");

        AddExam(exams, patients[8], doctors[7], "Thyroid Panel", baseDate.AddDays(-25), "Completed",
            "TSH: 4.8 mIU/L, Free T4: 0.9 ng/dL. Borderline hypothyroid.",
            "Patient reports fatigue. Thyroid monitoring initiated.");

        AddExam(exams, patients[9], doctors[8], "Ultrasound", baseDate.AddDays(-40), "Completed",
            "Kidney ultrasound normal. No stones or obstruction.",
            "Evaluation for recurrent urinary tract infections.");

        AddExam(exams, patients[9], doctors[8], "Urinalysis", baseDate.AddDays(5), "Scheduled",
            null, "Urinalysis to check for persistent infection.");

        AddExam(exams, patients[10], doctors[9], "Mammography", baseDate.AddDays(-35), "Completed",
            "No suspicious masses identified. BIRADS 1.",
            "Routine mammography screening.");

        AddExam(exams, patients[11], doctors[1], "CT Scan", baseDate.AddDays(-8), "Completed",
            "CT Head normal. No hemorrhage or mass effect.",
            "Patient experienced dizziness and transient visual disturbance.");

        AddExam(exams, patients[12], doctors[0], "Blood Test", baseDate.AddDays(-50), "Completed",
            "HbA1c: 6.1%, Glucose: 108 mg/dL. Pre-diabetic range.",
            "Comprehensive metabolic panel. Dietary counseling recommended.");

        AddExam(exams, patients[12], doctors[7], "Thyroid Panel", baseDate.AddDays(15), "Scheduled",
            null, "Thyroid and metabolic panel follow-up for pre-diabetes management.");

        AddExam(exams, patients[13], doctors[2], "X-Ray Chest", baseDate.AddDays(-12), "Completed",
            "Mild degenerative changes in lumbar spine. No acute findings.",
            "Patient with chronic lower back pain.");

        AddExam(exams, patients[14], doctors[3], "Skin Biopsy", baseDate.AddDays(-3), "Cancelled",
            null, "Cancelled by patient due to scheduling conflict. To be rescheduled.");

        AddExam(exams, patients[14], doctors[3], "Eye Exam", baseDate.AddDays(20), "Scheduled",
            null, "Dermatology referral for persistent skin rash near eye area.");

        AddExam(exams, patients[15], doctors[9], "Blood Test", baseDate.AddDays(-100), "Completed",
            "WBC: 5200/uL, RBC: 4.5M/uL, Platelets: 220K. All within range.",
            "Routine blood work for oncology follow-up.");

        AddExam(exams, patients[15], doctors[9], "CT Scan", baseDate.AddDays(3), "Scheduled",
            null, "CT scan for ongoing oncology monitoring.");

        AddExam(exams, patients[16], doctors[5], "Eye Exam", baseDate.AddDays(-18), "Completed",
            "Intraocular pressure: 18 mmHg. Optic nerve healthy.",
            "Glaucoma screening. No signs of disease.");

        AddExam(exams, patients[17], doctors[4], "Colonoscopy", baseDate.AddDays(25), "Scheduled",
            null, "Colonoscopy scheduled for screening based on age and family history.");

        AddExam(exams, patients[18], doctors[7], "Thyroid Panel", baseDate.AddDays(-7), "Completed",
            "TSH: 2.1 mIU/L, Free T4: 1.2 ng/dL. Normal thyroid function.",
            "Routine thyroid panel. No abnormalities.");

        AddExam(exams, patients[19], doctors[6], "Spirometry", baseDate.AddDays(-55), "Completed",
            "FEV1: 92% predicted. Normal lung function.",
            "Spirometry test for pre-operative clearance.");

        AddExam(exams, patients[19], doctors[0], "ECG", baseDate.AddDays(12), "Scheduled",
            null, "Pre-operative cardiac evaluation. ECG and consultation.");

        return exams;
    }

    private static void AddExam(List<Exam> exams, Patient patient, Doctor doctor,
        string type, DateOnly scheduledDate, string status, string? results, string? notes)
    {
        exams.Add(new Exam
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Type = type,
            ScheduledDate = scheduledDate,
            Status = status,
            Results = results,
            Notes = notes,
        });
    }
}
