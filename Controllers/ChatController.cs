using Microsoft.AspNetCore.Mvc;
using MainBackend.Services;
using MainBackend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace MainBackend.Controllers;

[ApiController]
[Route("api/chats")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;

    public ChatController(ChatService chatService)
    {
        _chatService = chatService;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateChat([FromQuery] CreateChatRequest request)
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user id claim")
        );
        var chat = await _chatService.CreateChatAsync(
            userId,
            request.Title
        );

        return Ok(new ChatResponse
        {
            ChatId = chat.ChatId,
            ChatTitle = chat.ChatTitle,
            CreatedAt = chat.CreatedAt
        });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetChats([FromQuery] GetChatsRequest request)
    {

        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user id claim")
        );

        var getChatsResult = await _chatService.GetChatsByUserId(
            userId,
            request
        );

        return Ok(getChatsResult);
    }

}