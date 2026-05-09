using LiteDB;
using RestReactAspire.DoctorService.Models;

namespace RestReactAspire.DoctorService.Stores;

public class DoctorStore
{
    private readonly ILiteCollection<Doctor> _doctors;

    public DoctorStore(ILiteDatabase database)
    {
        _doctors = database.GetCollection<Doctor>("doctors");
    }

    public IReadOnlyList<Doctor> GetAll() => [.. _doctors.FindAll()];

    public (IReadOnlyList<Doctor> Items, int TotalCount) GetPaged(int page, int pageSize, string sortBy = "specialty", string sortDirection = "asc")
    {
        var totalCount = _doctors.Count();
        var items = ApplySort(_doctors.FindAll(), sortBy, sortDirection)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, totalCount);
    }

    public (IReadOnlyList<Doctor> Items, int TotalCount) SearchPaged(string search, int page, int pageSize, string sortBy = "specialty", string sortDirection = "asc")
    {
        var lowerSearch = search.ToLowerInvariant();
        var all = _doctors.FindAll()
            .Where(d => d.FirstName.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || d.LastName.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || d.Specialty.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || d.Email.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || d.Phone.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var totalCount = all.Count;
        var items = ApplySort(all, sortBy, sortDirection)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, totalCount);
    }

    private static IEnumerable<Doctor> ApplySort(IEnumerable<Doctor> doctors, string sortBy, string sortDirection)
    {
        var descending = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return sortBy.ToLowerInvariant() switch
        {
            "firstname" => descending ? doctors.OrderByDescending(d => d.FirstName) : doctors.OrderBy(d => d.FirstName),
            "lastname" => descending ? doctors.OrderByDescending(d => d.LastName) : doctors.OrderBy(d => d.LastName),
            "email" => descending ? doctors.OrderByDescending(d => d.Email) : doctors.OrderBy(d => d.Email),
            "phone" => descending ? doctors.OrderByDescending(d => d.Phone) : doctors.OrderBy(d => d.Phone),
            _ => descending
                ? doctors.OrderByDescending(d => d.Specialty).ThenByDescending(d => d.LastName)
                : doctors.OrderBy(d => d.Specialty).ThenBy(d => d.LastName),
        };
    }

    public Doctor? GetById(Guid id) => _doctors.FindById(id);

    public Doctor Add(CreateDoctorRequest request)
    {
        var doctor = new Doctor
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Specialty = request.Specialty,
            Email = request.Email,
            Phone = request.Phone
        };
        _doctors.Insert(doctor);
        return doctor;
    }

    public Doctor Add(Doctor doctor)
    {
        _doctors.Insert(doctor);
        return doctor;
    }

    public Doctor? Update(Guid id, UpdateDoctorRequest request)
    {
        if (_doctors.FindById(id) is null)
            return null;

        var updated = new Doctor
        {
            Id = id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Specialty = request.Specialty,
            Email = request.Email,
            Phone = request.Phone
        };
        _doctors.Update(updated);
        return updated;
    }

    public bool Delete(Guid id) => _doctors.Delete(id);

    public int DeleteAll() => _doctors.DeleteAll();

    public void InsertBulk(IEnumerable<Doctor> doctors) => _doctors.InsertBulk(doctors);
}
