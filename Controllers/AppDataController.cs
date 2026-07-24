using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MainBackend.Services;

namespace MainBackend.Controllers;

[ApiController]
[Route("api/app-data")]
public class AppDataController : ControllerBase
{
    private readonly IAppDataService _appDataService;
    private readonly DocumentDataService _documentDataService;

    public AppDataController(
        IAppDataService appDataService,
        DocumentDataService documentDataService)
    {
        _appDataService = appDataService;
        _documentDataService = documentDataService;
    }

    [HttpGet("majors")]
    public async Task<IActionResult> GetMajors()
    {
        var majors = await _appDataService.GetMajors();

        return Ok(majors);
    }

    [Authorize]
    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses()
    {
        var courses = await _appDataService.GetCourses();

        return Ok(courses.Select(course => new
        {
            course.CourseId,
            course.CourseCode,
            course.CourseName
        }));
    }

    [Authorize]
    [HttpGet("documents/{documentId}")]
    public async Task<IActionResult> GetDocumentById(int documentId)
    {
        var document = await _documentDataService.GetDocumentById(documentId);

        if (document == null)
        {
            return NotFound(new { message = "Document not found" });
        }

        return Ok(new
        {
            document.DocumentId,
            document.DocumentTitle,
            document.FileUrl
        });
    }

    [HttpGet("cohorts")]
    public IActionResult GetCohorts()
    {
        var cohorts = new[]
        {
        "K42", "K43", "K44", "K45", "K46",
        "K47", "K48", "K49", "K50", "K51", "K52"
    };

        return Ok(cohorts);
    }
}
