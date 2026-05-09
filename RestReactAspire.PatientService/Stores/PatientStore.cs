using LiteDB;
using RestReactAspire.PatientService.Models;

namespace RestReactAspire.PatientService.Stores;

public class PatientStore
{
    private readonly ILiteCollection<Patient> _patients;

    public PatientStore(ILiteDatabase database)
    {
        _patients = database.GetCollection<Patient>("patients");
    }

    public IReadOnlyList<Patient> GetAll() => [.. _patients.FindAll()];

    public (IReadOnlyList<Patient> Items, int TotalCount) GetPaged(int page, int pageSize, string sortBy = "lastName", string sortDirection = "asc")
    {
        var totalCount = _patients.Count();
        var items = ApplySort(_patients.FindAll(), sortBy, sortDirection)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, totalCount);
    }

    public (IReadOnlyList<Patient> Items, int TotalCount) SearchPaged(string search, int page, int pageSize, string sortBy = "lastName", string sortDirection = "asc")
    {
        var lowerSearch = search.ToLowerInvariant();
        var all = _patients.FindAll()
            .Where(p => p.FirstName.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || p.LastName.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || p.Email.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || p.Phone.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var totalCount = all.Count;
        var items = ApplySort(all, sortBy, sortDirection)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, totalCount);
    }

    private static IEnumerable<Patient> ApplySort(IEnumerable<Patient> patients, string sortBy, string sortDirection)
    {
        var descending = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return sortBy.ToLowerInvariant() switch
        {
            "firstname" => descending ? patients.OrderByDescending(p => p.FirstName) : patients.OrderBy(p => p.FirstName),
            "dateofbirth" => descending ? patients.OrderByDescending(p => p.DateOfBirth) : patients.OrderBy(p => p.DateOfBirth),
            "email" => descending ? patients.OrderByDescending(p => p.Email) : patients.OrderBy(p => p.Email),
            "phone" => descending ? patients.OrderByDescending(p => p.Phone) : patients.OrderBy(p => p.Phone),
            _ => descending
                ? patients.OrderByDescending(p => p.LastName).ThenByDescending(p => p.FirstName)
                : patients.OrderBy(p => p.LastName).ThenBy(p => p.FirstName),
        };
    }

    public Patient? GetById(Guid id) => _patients.FindById(id);

    public Patient Add(CreatePatientRequest request)
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            Email = request.Email,
            Phone = request.Phone
        };
        _patients.Insert(patient);
        return patient;
    }

    public Patient Add(Patient patient)
    {
        _patients.Insert(patient);
        return patient;
    }

    public Patient? Update(Guid id, UpdatePatientRequest request)
    {
        if (_patients.FindById(id) is null)
            return null;

        var updated = new Patient
        {
            Id = id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            Email = request.Email,
            Phone = request.Phone
        };
        _patients.Update(updated);
        return updated;
    }

    public bool Delete(Guid id) => _patients.Delete(id);

    public int DeleteAll() => _patients.DeleteAll();

    public void InsertBulk(IEnumerable<Patient> patients) => _patients.InsertBulk(patients);
}
