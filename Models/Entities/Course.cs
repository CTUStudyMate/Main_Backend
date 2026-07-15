namespace MainBackend.Models;

public class Course
{
    public int CourseId { get; set; }

    public required string CourseCode { get; set; }

    public required string CourseName { get; set; }

    public ICollection<User> Lecturers { get; set; } = new List<User>();
}
