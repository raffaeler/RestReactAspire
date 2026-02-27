using LiteDB;
using RestReactAspire.Server.Models;

namespace RestReactAspire.Server.Stores;

public class PatientStore
{
    private readonly ILiteCollection<Patient> _patients;

    public PatientStore(ILiteDatabase database)
    {
        _patients = database.GetCollection<Patient>("patients");
    }

    public IReadOnlyList<Patient> GetAll() => [.. _patients.FindAll()];

    public (IReadOnlyList<Patient> Items, int TotalCount) GetPaged(int page, int pageSize)
    {
        var totalCount = _patients.Count();
        var items = _patients.FindAll().Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, totalCount);
    }

    public (IReadOnlyList<Patient> Items, int TotalCount) SearchPaged(string search, int page, int pageSize)
    {
        var lowerSearch = search.ToLowerInvariant();
        var all = _patients.FindAll()
            .Where(p => p.FirstName.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || p.LastName.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || p.Email.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || p.Phone.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var totalCount = all.Count;
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, totalCount);
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
}
