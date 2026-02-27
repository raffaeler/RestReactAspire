using LiteDB;
using RestReactAspire.Server.Models;
using RestReactAspire.Server.Stores;

namespace RestReactAspire.Server.Tests;

public class PatientStoreTests : IDisposable
{
    private readonly ILiteDatabase _db;
    private readonly PatientStore _store;

    public PatientStoreTests()
    {
        LiteDbFactory.ConfigureMapper();
        _db = new LiteDatabase(":memory:");
        _store = new PatientStore(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void GetAll_ReturnsEmpty_WhenNoPatients()
    {
        var result = _store.GetAll();
        Assert.Empty(result);
    }

    [Fact]
    public void Add_CreatesPatient_WithGeneratedId()
    {
        var request = new CreatePatientRequest("John", "Doe", new DateOnly(1990, 1, 1), "john@example.com", "555-0100");
        var patient = _store.Add(request);

        Assert.NotEqual(Guid.Empty, patient.Id);
        Assert.Equal("John", patient.FirstName);
        Assert.Equal("Doe", patient.LastName);
        Assert.Equal(new DateOnly(1990, 1, 1), patient.DateOfBirth);
        Assert.Equal("john@example.com", patient.Email);
        Assert.Equal("555-0100", patient.Phone);
    }

    [Fact]
    public void GetById_ReturnsPatient_WhenExists()
    {
        var request = new CreatePatientRequest("Jane", "Smith", new DateOnly(1985, 6, 15), "jane@example.com", "555-0200");
        var created = _store.Add(request);

        var retrieved = _store.GetById(created.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("Jane", retrieved.FirstName);
    }

    [Fact]
    public void GetById_ReturnsNull_WhenNotExists()
    {
        var result = _store.GetById(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void Update_ReturnsUpdatedPatient_WhenExists()
    {
        var createRequest = new CreatePatientRequest("John", "Doe", new DateOnly(1990, 1, 1), "john@example.com", "555-0100");
        var created = _store.Add(createRequest);

        var updateRequest = new UpdatePatientRequest("John", "Updated", new DateOnly(1990, 1, 1), "john.updated@example.com", "555-0101");
        var updated = _store.Update(created.Id, updateRequest);

        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated", updated.LastName);
        Assert.Equal("john.updated@example.com", updated.Email);
    }

    [Fact]
    public void Update_ReturnsNull_WhenNotExists()
    {
        var request = new UpdatePatientRequest("John", "Doe", new DateOnly(1990, 1, 1), "john@example.com", "555-0100");
        var result = _store.Update(Guid.NewGuid(), request);
        Assert.Null(result);
    }

    [Fact]
    public void Delete_ReturnsTrue_WhenExists()
    {
        var request = new CreatePatientRequest("John", "Doe", new DateOnly(1990, 1, 1), "john@example.com", "555-0100");
        var created = _store.Add(request);

        Assert.True(_store.Delete(created.Id));
        Assert.Null(_store.GetById(created.Id));
    }

    [Fact]
    public void Delete_ReturnsFalse_WhenNotExists()
    {
        Assert.False(_store.Delete(Guid.NewGuid()));
    }

    [Fact]
    public void GetAll_ReturnsAllPatients_AfterMultipleAdds()
    {
        _store.Add(new CreatePatientRequest("Alice", "A", new DateOnly(1990, 1, 1), "alice@example.com", "555-0001"));
        _store.Add(new CreatePatientRequest("Bob", "B", new DateOnly(1991, 2, 2), "bob@example.com", "555-0002"));
        _store.Add(new CreatePatientRequest("Charlie", "C", new DateOnly(1992, 3, 3), "charlie@example.com", "555-0003"));

        var all = _store.GetAll();
        Assert.Equal(3, all.Count);
    }
}
