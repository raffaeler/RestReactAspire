using LiteDB;
using RestReactAspire.Server.Models;

namespace RestReactAspire.Server.Stores;

public class DoctorStore
{
    private readonly ILiteCollection<Doctor> _doctors;

    public DoctorStore(ILiteDatabase database)
    {
        _doctors = database.GetCollection<Doctor>("doctors");
    }

    public IReadOnlyList<Doctor> GetAll() => [.. _doctors.FindAll()];

    public (IReadOnlyList<Doctor> Items, int TotalCount) GetPaged(int page, int pageSize)
    {
        var totalCount = _doctors.Count();
        var items = _doctors.FindAll().Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, totalCount);
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
}
