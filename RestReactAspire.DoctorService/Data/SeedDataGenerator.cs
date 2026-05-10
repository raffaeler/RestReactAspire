using RestReactAspire.DoctorService.Models;

namespace RestReactAspire.DoctorService.Data;

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

    public static List<Doctor> GenerateDoctors()
    {
        var rng = new Random(123);
        var doctors = new List<Doctor>(30);

        for (int i = 0; i < 30; i++)
        {
            var firstName = FirstNames[rng.Next(FirstNames.Length)];
            var lastName = LastNames[rng.Next(LastNames.Length)];
            var specialty = Specialties[i % Specialties.Length];

            var guidBytes = new byte[16];
            rng.NextBytes(guidBytes);
            var id = new Guid(guidBytes);

            doctors.Add(new Doctor
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                Specialty = specialty,
                Email = $"{firstName[0].ToString().ToLowerInvariant()}.{lastName.ToLowerInvariant().Replace(" ", "")}_{i}@hospital.com",
                Phone = $"+39 02 500{i + 1:D4}",
            });
        }

        return doctors;
    }
}
