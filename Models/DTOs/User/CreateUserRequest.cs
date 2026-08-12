namespace MainBackend.Models;
public class CreateUserRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public required string Role { get; set; }
    public int? MajorId { get; set; }
    public string? Cohort { get; set; }
    public List<int> CourseIds { get; set; } = [];
}
