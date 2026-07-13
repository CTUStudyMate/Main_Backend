using MainBackend.Models;
using Microsoft.EntityFrameworkCore;


namespace MainBackend.Services;

public class UniDataService : IAppDataService
{
    private readonly AppDbContext _db;

    public UniDataService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Major>> GetMajors()
    {
        return await _db.Majors
            .OrderBy(major => major.MajorCode)
            .ToListAsync();
    }
}
