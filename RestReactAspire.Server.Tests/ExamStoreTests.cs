using RestReactAspire.Server.Models;
using RestReactAspire.Server.Stores;

namespace RestReactAspire.Server.Tests;

public class ExamStoreTests
{
    private readonly ExamStore _store = new();

    private static CreateExamRequest MakeRequest(Guid patientId) =>
        new(patientId, null, "Blood Test", new DateOnly(2025, 6, 15), "Scheduled", null, null);

    [Fact]
    public void GetAll_ReturnsEmpty_WhenNoExams()
    {
        var result = _store.GetAll();
        Assert.Empty(result);
    }

    [Fact]
    public void Add_CreatesExam_WithGeneratedId()
    {
        var patientId = Guid.NewGuid();
        var request = MakeRequest(patientId);
        var exam = _store.Add(request);

        Assert.NotEqual(Guid.Empty, exam.Id);
        Assert.Equal(patientId, exam.PatientId);
        Assert.Equal("Blood Test", exam.Type);
        Assert.Equal(new DateOnly(2025, 6, 15), exam.ScheduledDate);
        Assert.Equal("Scheduled", exam.Status);
        Assert.Null(exam.Results);
        Assert.Null(exam.Notes);
    }

    [Fact]
    public void GetById_ReturnsExam_WhenExists()
    {
        var exam = _store.Add(MakeRequest(Guid.NewGuid()));

        var retrieved = _store.GetById(exam.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(exam.Id, retrieved.Id);
        Assert.Equal("Blood Test", retrieved.Type);
    }

    [Fact]
    public void GetById_ReturnsNull_WhenNotExists()
    {
        var result = _store.GetById(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void GetByPatientId_ReturnsOnlyMatchingExams()
    {
        var patient1 = Guid.NewGuid();
        var patient2 = Guid.NewGuid();
        _store.Add(new CreateExamRequest(patient1, null, "Blood Test", new DateOnly(2025, 6, 1), "Scheduled", null, null));
        _store.Add(new CreateExamRequest(patient1, null, "X-Ray", new DateOnly(2025, 6, 2), "Scheduled", null, null));
        _store.Add(new CreateExamRequest(patient2, null, "MRI", new DateOnly(2025, 6, 3), "Scheduled", null, null));

        var result = _store.GetByPatientId(patient1);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(patient1, e.PatientId));
    }

    [Fact]
    public void Update_ReturnsUpdatedExam_WhenExists()
    {
        var exam = _store.Add(MakeRequest(Guid.NewGuid()));

        var updateRequest = new UpdateExamRequest(null, "MRI", new DateOnly(2025, 7, 1), "Completed", "Normal", "Follow up in 6 months");
        var updated = _store.Update(exam.Id, updateRequest);

        Assert.NotNull(updated);
        Assert.Equal(exam.Id, updated.Id);
        Assert.Equal(exam.PatientId, updated.PatientId);
        Assert.Equal("MRI", updated.Type);
        Assert.Equal("Completed", updated.Status);
        Assert.Equal("Normal", updated.Results);
        Assert.Equal("Follow up in 6 months", updated.Notes);
    }

    [Fact]
    public void Update_ReturnsNull_WhenNotExists()
    {
        var request = new UpdateExamRequest(null, "MRI", new DateOnly(2025, 7, 1), "Completed", null, null);
        var result = _store.Update(Guid.NewGuid(), request);
        Assert.Null(result);
    }

    [Fact]
    public void Delete_ReturnsTrue_WhenExists()
    {
        var exam = _store.Add(MakeRequest(Guid.NewGuid()));

        Assert.True(_store.Delete(exam.Id));
        Assert.Null(_store.GetById(exam.Id));
    }

    [Fact]
    public void Delete_ReturnsFalse_WhenNotExists()
    {
        Assert.False(_store.Delete(Guid.NewGuid()));
    }

    [Fact]
    public void GetAll_ReturnsAllExams_AfterMultipleAdds()
    {
        var patientId = Guid.NewGuid();
        _store.Add(new CreateExamRequest(patientId, null, "Blood Test", new DateOnly(2025, 6, 1), "Scheduled", null, null));
        _store.Add(new CreateExamRequest(patientId, null, "X-Ray", new DateOnly(2025, 6, 2), "Completed", "Clear", null));
        _store.Add(new CreateExamRequest(Guid.NewGuid(), null, "MRI", new DateOnly(2025, 6, 3), "Cancelled", null, "Patient rescheduled"));

        var all = _store.GetAll();
        Assert.Equal(3, all.Count);
    }
}
