using LiteDB;

namespace RestReactAspire.Server.Models;

public class Exam
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public required string Type { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
    public int? DurationMinutes { get; set; }
    public required string Status { get; set; }
    public string? Results { get; set; }
    public string? Notes { get; set; }

    [BsonIgnore]
    public TimeOnly? EndTime => ScheduledTime.HasValue && DurationMinutes.HasValue
        ? ScheduledTime.Value.AddMinutes(DurationMinutes.Value)
        : null;
}
