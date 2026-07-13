using MainBackend.Models;
namespace MainBackend.Services;

public interface IAppDataService
{
    Task<List<Major>> GetMajors();
}
