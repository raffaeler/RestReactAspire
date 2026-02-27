using System.Collections.Concurrent;
using RestReactAspire.Server.Models;

namespace RestReactAspire.Server.Stores;

public class PatientStore
{
    private readonly ConcurrentDictionary<Guid, Patient> _patients = new();

    public IReadOnlyList<Patient> GetAll() => [.. _patients.Values];

    public Patient? GetById(Guid id) => _patients.GetValueOrDefault(id);

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
        _patients[patient.Id] = patient;
        return patient;
    }

    public Patient? Update(Guid id, UpdatePatientRequest request)
    {
        if (!_patients.ContainsKey(id))
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
        _patients[id] = updated;
        return updated;
    }

    public bool Delete(Guid id) => _patients.TryRemove(id, out _);
}
