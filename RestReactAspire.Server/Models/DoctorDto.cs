namespace RestReactAspire.Server.Models;

public record CreateDoctorRequest(
    string FirstName,
    string LastName,
    string Specialty,
    string Email,
    string Phone);

public record UpdateDoctorRequest(
    string FirstName,
    string LastName,
    string Specialty,
    string Email,
    string Phone);

public record DoctorResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Specialty,
    string Email,
    string Phone,
    IReadOnlyList<Link> Links);

public record DoctorListResponse(
    IReadOnlyList<DoctorResponse> Items,
    IReadOnlyList<Link> Links);

public record AssignDoctorRequest(Guid? DoctorId);
