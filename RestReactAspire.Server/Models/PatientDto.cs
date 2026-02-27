namespace RestReactAspire.Server.Models;

public record CreatePatientRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Email,
    string Phone);

public record UpdatePatientRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Email,
    string Phone);

public record PatientResponse(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Email,
    string Phone,
    IReadOnlyList<Link> Links);

public record PatientListResponse(
    IReadOnlyList<PatientResponse> Items,
    PaginationInfo Pagination,
    SortInfo Sort,
    IReadOnlyList<Link> Links);

public record ApiRootResponse(IReadOnlyList<Link> Links);
