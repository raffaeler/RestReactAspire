namespace RestReactAspire.Server.Models;

public class Exam
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public required string Type { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public required string Status { get; set; }
    public string? Results { get; set; }
    public string? Notes { get; set; }
}
