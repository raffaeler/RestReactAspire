namespace RestReactAspire.Server.Models;

public class Patient
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
}
