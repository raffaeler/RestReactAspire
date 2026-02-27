using LiteDB;
using RestReactAspire.Server.Models;

namespace RestReactAspire.Server.Stores;

public class ExamStore
{
    private readonly ILiteCollection<Exam> _exams;

    public ExamStore(ILiteDatabase database)
    {
        _exams = database.GetCollection<Exam>("exams");
    }

    public IReadOnlyList<Exam> GetAll() => [.. _exams.FindAll()];

    public IReadOnlyList<Exam> GetByPatientId(Guid patientId) =>
        [.. _exams.Find(e => e.PatientId == patientId)];

    public IReadOnlyList<Exam> GetByDoctorId(Guid doctorId) =>
        [.. _exams.Find(e => e.DoctorId == doctorId)];

    public Exam? GetById(Guid id) => _exams.FindById(id);

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
        _exams.Insert(exam);
        return exam;
    }

    public Exam? Update(Guid id, UpdateExamRequest request)
    {
        var existing = _exams.FindById(id);
        if (existing is null)
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
        _exams.Update(updated);
        return updated;
    }

    public Exam? AssignDoctor(Guid id, Guid? doctorId)
    {
        var existing = _exams.FindById(id);
        if (existing is null)
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
        _exams.Update(updated);
        return updated;
    }

    public bool Delete(Guid id) => _exams.Delete(id);
}
