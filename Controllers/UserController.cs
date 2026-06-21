using Microsoft.AspNetCore.Mvc;
using MainBackend.Services;
using MainBackend.Models;

namespace MainBackend.Controllers;

[ApiController]
[Route("api/users")]
public class UserController: ControllerBase
{
    private readonly UserService _userService;
    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        var user = await _userService.CreateUserAsync(request);
        return Ok(new CreateUserResponse
        {
            Name = user.Name,
            Username = user.Username,
            Role = request.Role
        });
    }
}
