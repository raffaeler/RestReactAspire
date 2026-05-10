using RestReactAspire.PatientService.Models;

namespace RestReactAspire.PatientService.Data;

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

    private static readonly string[] AreaCodes =
    [
        "+39 02", "+39 06", "+39 011", "+39 051", "+39 081",
        "+39 055", "+39 041", "+39 010", "+39 091", "+39 049",
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

            var guidBytes = new byte[16];
            rng.NextBytes(guidBytes);
            var id = new Guid(guidBytes);

            patients.Add(new Patient
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = new DateOnly(year, month, day),
                Email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant().Replace(" ", "")}_{i}@email.com",
                Phone = $"{areaCode} {phoneNumber}",
            });
        }

        return patients;
    }
}
