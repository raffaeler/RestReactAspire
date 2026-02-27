using System.Collections.Concurrent;
using RestReactAspire.Server.Models;

namespace RestReactAspire.Server.Stores;

public class DoctorStore
{
    private readonly ConcurrentDictionary<Guid, Doctor> _doctors = new();

    public IReadOnlyList<Doctor> GetAll() => [.. _doctors.Values];

    public Doctor? GetById(Guid id) => _doctors.GetValueOrDefault(id);

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
        _doctors[doctor.Id] = doctor;
        return doctor;
    }

    public Doctor? Update(Guid id, UpdateDoctorRequest request)
    {
        if (!_doctors.ContainsKey(id))
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
        _doctors[id] = updated;
        return updated;
    }

    public bool Delete(Guid id) => _doctors.TryRemove(id, out _);
}
