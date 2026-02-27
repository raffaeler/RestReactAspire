using RestReactAspire.Server.Models;
using RestReactAspire.Server.Stores;

namespace RestReactAspire.Server.Tests;

public class DoctorStoreTests
{
    private readonly DoctorStore _store = new();

    private static CreateDoctorRequest MakeRequest() =>
        new("John", "Smith", "Cardiology", "john.smith@hospital.com", "555-1234");

    [Fact]
    public void GetAll_ReturnsEmpty_WhenNoDoctors()
    {
        var result = _store.GetAll();
        Assert.Empty(result);
    }

    [Fact]
    public void Add_CreatesDoctor_WithGeneratedId()
    {
        var request = MakeRequest();
        var doctor = _store.Add(request);

        Assert.NotEqual(Guid.Empty, doctor.Id);
        Assert.Equal("John", doctor.FirstName);
        Assert.Equal("Smith", doctor.LastName);
        Assert.Equal("Cardiology", doctor.Specialty);
        Assert.Equal("john.smith@hospital.com", doctor.Email);
        Assert.Equal("555-1234", doctor.Phone);
    }

    [Fact]
    public void GetById_ReturnsDoctor_WhenExists()
    {
        var doctor = _store.Add(MakeRequest());

        var retrieved = _store.GetById(doctor.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(doctor.Id, retrieved.Id);
        Assert.Equal("John", retrieved.FirstName);
    }

    [Fact]
    public void GetById_ReturnsNull_WhenNotExists()
    {
        var result = _store.GetById(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void Update_ReturnsUpdatedDoctor_WhenExists()
    {
        var doctor = _store.Add(MakeRequest());

        var updateRequest = new UpdateDoctorRequest("Jane", "Doe", "Neurology", "jane.doe@hospital.com", "555-5678");
        var updated = _store.Update(doctor.Id, updateRequest);

        Assert.NotNull(updated);
        Assert.Equal(doctor.Id, updated.Id);
        Assert.Equal("Jane", updated.FirstName);
        Assert.Equal("Doe", updated.LastName);
        Assert.Equal("Neurology", updated.Specialty);
        Assert.Equal("jane.doe@hospital.com", updated.Email);
        Assert.Equal("555-5678", updated.Phone);
    }

    [Fact]
    public void Update_ReturnsNull_WhenNotExists()
    {
        var request = new UpdateDoctorRequest("Jane", "Doe", "Neurology", "jane@hospital.com", "555-5678");
        var result = _store.Update(Guid.NewGuid(), request);
        Assert.Null(result);
    }

    [Fact]
    public void Delete_ReturnsTrue_WhenExists()
    {
        var doctor = _store.Add(MakeRequest());

        Assert.True(_store.Delete(doctor.Id));
        Assert.Null(_store.GetById(doctor.Id));
    }

    [Fact]
    public void Delete_ReturnsFalse_WhenNotExists()
    {
        Assert.False(_store.Delete(Guid.NewGuid()));
    }

    [Fact]
    public void GetAll_ReturnsAllDoctors_AfterMultipleAdds()
    {
        _store.Add(new CreateDoctorRequest("John", "Smith", "Cardiology", "john@hospital.com", "555-0001"));
        _store.Add(new CreateDoctorRequest("Jane", "Doe", "Neurology", "jane@hospital.com", "555-0002"));
        _store.Add(new CreateDoctorRequest("Bob", "Jones", "Orthopedics", "bob@hospital.com", "555-0003"));

        var all = _store.GetAll();
        Assert.Equal(3, all.Count);
    }
}
