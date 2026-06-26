using Microsoft.AspNetCore.Mvc;
using MainBackend.Services;
using MainBackend.Models;
using MainBackend.Configurations;
namespace MainBackend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;
    public AuthController(IAuthService authService, IJwtService jwtService)
    {
        _authService = authService;
        _jwtService = jwtService;
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.Login(request);

        if (user == null)
            return Unauthorized();

        var token = _jwtService.GenerateToken(user);

        Response.Cookies.Append("access_token", token, CookieOptionsFactory.CreateAuthCookie());

        return Ok(user);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var registerResult = await _authService.Register(request);

        if (registerResult.Auth == null)
        {
            return Conflict(registerResult.Response);
        }

        var token = _jwtService.GenerateToken(registerResult.Auth);

        Response.Cookies.Append(
            "access_token",
            token,
            CookieOptionsFactory.CreateAuthCookie()
        );

        return Ok(registerResult.Response);
    }
}