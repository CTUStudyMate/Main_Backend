using MainBackend.Models;
namespace MainBackend.Services;

public interface IAppDataService
{
    Task<List<Major>> GetMajors();
    Task<List<Course>> GetCourses();
    Task<Document?> GetDocumentById(int documentId);
}
