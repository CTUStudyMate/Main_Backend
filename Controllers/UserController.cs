using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MainBackend.Services;
using MainBackend.Models;

namespace MainBackend.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/users")]
public class UserController: ControllerBase
{
    private readonly UserService _userService;
    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.CreateUserAsync(request, cancellationToken);
        return Ok(new CreateUserResponse
        {
            Name = user.Name,
            Username = user.Username,
            Role = request.Role
        });
    }
    
}
