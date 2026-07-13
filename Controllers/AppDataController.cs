using Microsoft.AspNetCore.Mvc;
using MainBackend.Services;

namespace MainBackend.Controllers;

[ApiController]
[Route("api/app-data")]
public class AppDataController : ControllerBase
{
    private readonly IAppDataService _appDataService;

    public AppDataController(IAppDataService appDataService)
    {
        _appDataService = appDataService;
    }

    [HttpGet("majors")]
    public async Task<IActionResult> GetMajors()
    {
        var majors = await _appDataService.GetMajors();

        return Ok(majors);
    }

    [HttpGet("cohorts")]
    public async Task<IActionResult> GetCohorts()
    {
        var cohorts = new[]
        {
        "K42", "K43", "K44", "K45", "K46",
        "K47", "K48", "K49", "K50", "K51", "K52"
    };

        return Ok(cohorts);
    }
}
