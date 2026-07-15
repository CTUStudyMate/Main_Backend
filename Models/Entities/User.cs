namespace MainBackend.Models;

public enum UserRole
{
    Student,
    Lecturer,
    Admin
}

public class User
{
    public int UserId { get; set; }
    public int? MajorId { get; set; }
    public required string Username { get; set; } 
    public required string Password { get; set; } = string.Empty;
    public required string Email { get; set; }
    public required string Name { get; set; }
    public UserRole Role { get; set; }
    public string? Cohort { get; set; }
    public required string AccountStatus { get; set; }

    // Navigation property
    public Major? Major { get; set; }
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<Chat> Chats { get; set; } = new List<Chat>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
