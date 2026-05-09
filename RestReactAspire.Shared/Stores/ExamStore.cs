using LiteDB;
using RestReactAspire.Shared.Models;

namespace RestReactAspire.Shared.Stores;

public class ExamStore
{
    private readonly ILiteCollection<Exam> _exams;

    public ExamStore(ILiteDatabase database)
    {
        _exams = database.GetCollection<Exam>("exams");
    }

    public IReadOnlyList<Exam> GetAll() => [.. _exams.FindAll()];

    public (IReadOnlyList<Exam> Items, int TotalCount) GetPaged(int page, int pageSize, string sortBy = "scheduledDate", string sortDirection = "asc")
    {
        var totalCount = _exams.Count();
        var items = ApplySort(_exams.FindAll(), sortBy, sortDirection)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, totalCount);
    }

    public (IReadOnlyList<Exam> Items, int TotalCount) SearchPaged(string search, int page, int pageSize, string sortBy = "scheduledDate", string sortDirection = "asc")
    {
        var lowerSearch = search.ToLowerInvariant();
        var all = _exams.FindAll()
            .Where(e => e.Type.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || e.Status.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || (e.Results != null && e.Results.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase))
                     || (e.Notes != null && e.Notes.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase))
                     || e.ScheduledDate.ToString("yyyy-MM-dd").Contains(lowerSearch, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var totalCount = all.Count;
        var items = ApplySort(all, sortBy, sortDirection)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, totalCount);
    }

    public IReadOnlyList<Exam> GetByPatientId(Guid patientId) =>
        [.. _exams.Find(e => e.PatientId == patientId)];

    public (IReadOnlyList<Exam> Items, int TotalCount) GetByPatientIdPaged(Guid patientId, int page, int pageSize, string sortBy = "scheduledDate", string sortDirection = "asc")
    {
        var all = _exams.Find(e => e.PatientId == patientId).ToList();
        var totalCount = all.Count;
        var items = ApplySort(all, sortBy, sortDirection)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, totalCount);
    }

    public (IReadOnlyList<Exam> Items, int TotalCount) SearchByPatientIdPaged(Guid patientId, string search, int page, int pageSize, string sortBy = "scheduledDate", string sortDirection = "asc")
    {
        var lowerSearch = search.ToLowerInvariant();
        var all = _exams.Find(e => e.PatientId == patientId).ToList()
            .Where(e => e.Type.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || e.Status.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || (e.Results != null && e.Results.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase))
                     || (e.Notes != null && e.Notes.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase))
                     || e.ScheduledDate.ToString("yyyy-MM-dd").Contains(lowerSearch, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var totalCount = all.Count;
        var items = ApplySort(all, sortBy, sortDirection)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, totalCount);
    }

    public IReadOnlyList<Exam> GetByDoctorId(Guid doctorId) =>
        [.. _exams.Find(e => e.DoctorId == doctorId)];

    public (IReadOnlyList<Exam> Items, int TotalCount) GetByDoctorIdPaged(Guid doctorId, int page, int pageSize, string sortBy = "scheduledDate", string sortDirection = "asc")
    {
        var all = _exams.Find(e => e.DoctorId == doctorId).ToList();
        var totalCount = all.Count;
        var items = ApplySort(all, sortBy, sortDirection)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, totalCount);
    }

    public (IReadOnlyList<Exam> Items, int TotalCount) SearchByDoctorIdPaged(Guid doctorId, string search, int page, int pageSize, string sortBy = "scheduledDate", string sortDirection = "asc")
    {
        var lowerSearch = search.ToLowerInvariant();
        var all = _exams.Find(e => e.DoctorId == doctorId).ToList()
            .Where(e => e.Type.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || e.Status.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                     || (e.Results != null && e.Results.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase))
                     || (e.Notes != null && e.Notes.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase))
                     || e.ScheduledDate.ToString("yyyy-MM-dd").Contains(lowerSearch, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var totalCount = all.Count;
        var items = ApplySort(all, sortBy, sortDirection)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, totalCount);
    }

    private static IEnumerable<Exam> ApplySort(IEnumerable<Exam> exams, string sortBy, string sortDirection)
    {
        var descending = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return sortBy.ToLowerInvariant() switch
        {
            "type" => descending ? exams.OrderByDescending(e => e.Type) : exams.OrderBy(e => e.Type),
            "status" => descending ? exams.OrderByDescending(e => e.Status) : exams.OrderBy(e => e.Status),
            "results" => descending ? exams.OrderByDescending(e => e.Results) : exams.OrderBy(e => e.Results),
            _ => descending ? exams.OrderByDescending(e => e.ScheduledDate) : exams.OrderBy(e => e.ScheduledDate),
        };
    }

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
            ScheduledTime = request.ScheduledTime,
            DurationMinutes = request.DurationMinutes,
            Status = request.Status,
            Results = request.Results,
            Notes = request.Notes
        };
        _exams.Insert(exam);
        return exam;
    }

    public Exam Add(Exam exam)
    {
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
            ScheduledTime = request.ScheduledTime,
            DurationMinutes = request.DurationMinutes,
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
            ScheduledTime = existing.ScheduledTime,
            DurationMinutes = existing.DurationMinutes,
            Status = existing.Status,
            Results = existing.Results,
            Notes = existing.Notes
        };
        _exams.Update(updated);
        return updated;
    }

    public bool Delete(Guid id) => _exams.Delete(id);

    public int DeleteAll() => _exams.DeleteAll();

    public void InsertBulk(IEnumerable<Exam> exams) => _exams.InsertBulk(exams);
}
