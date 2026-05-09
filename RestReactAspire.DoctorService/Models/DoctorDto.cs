namespace RestReactAspire.DoctorService.Models;

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
    PaginationInfo Pagination,
    SortInfo Sort,
    IReadOnlyList<Link> Links);

public record AssignDoctorRequest(Guid? DoctorId);

public record SeedResponse(
    int PatientsCreated,
    int DoctorsCreated,
    int ExamsCreated,
    IReadOnlyList<Link> Links);

public record ResetResponse(
    int PatientsDeleted,
    int DoctorsDeleted,
    int ExamsDeleted,
    IReadOnlyList<Link> Links);

public record StatsResponse(
    int PatientCount,
    int DoctorCount,
    int ExamCount,
    IReadOnlyList<Link> Links);
