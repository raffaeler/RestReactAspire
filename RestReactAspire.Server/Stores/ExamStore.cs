using System.Collections.Concurrent;
using RestReactAspire.Server.Models;

namespace RestReactAspire.Server.Stores;

public class ExamStore
{
    private readonly ConcurrentDictionary<Guid, Exam> _exams = new();

    public IReadOnlyList<Exam> GetAll() => [.. _exams.Values];

    public IReadOnlyList<Exam> GetByPatientId(Guid patientId) =>
        [.. _exams.Values.Where(e => e.PatientId == patientId)];

    public IReadOnlyList<Exam> GetByDoctorId(Guid doctorId) =>
        [.. _exams.Values.Where(e => e.DoctorId == doctorId)];

    public Exam? GetById(Guid id) => _exams.GetValueOrDefault(id);

    public Exam Add(CreateExamRequest request)
    {
        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            Type = request.Type,
            ScheduledDate = request.ScheduledDate,
            Status = request.Status,
            Results = request.Results,
            Notes = request.Notes
        };
        _exams[exam.Id] = exam;
        return exam;
    }

    public Exam? Update(Guid id, UpdateExamRequest request)
    {
        if (!_exams.TryGetValue(id, out var existing))
            return null;

        var updated = new Exam
        {
            Id = id,
            PatientId = existing.PatientId,
            DoctorId = request.DoctorId,
            Type = request.Type,
            ScheduledDate = request.ScheduledDate,
            Status = request.Status,
            Results = request.Results,
            Notes = request.Notes
        };
        _exams[id] = updated;
        return updated;
    }

    public Exam? AssignDoctor(Guid id, Guid? doctorId)
    {
        if (!_exams.TryGetValue(id, out var existing))
            return null;

        var updated = new Exam
        {
            Id = existing.Id,
            PatientId = existing.PatientId,
            DoctorId = doctorId,
            Type = existing.Type,
            ScheduledDate = existing.ScheduledDate,
            Status = existing.Status,
            Results = existing.Results,
            Notes = existing.Notes
        };
        _exams[id] = updated;
        return updated;
    }

    public bool Delete(Guid id) => _exams.TryRemove(id, out _);
}
