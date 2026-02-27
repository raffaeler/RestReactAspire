namespace RestReactAspire.Server.Models;

public class Doctor
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Specialty { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
}
