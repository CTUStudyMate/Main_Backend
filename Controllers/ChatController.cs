using Microsoft.AspNetCore.Mvc;
using MainBackend.Services;
using MainBackend.Models;
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

    [HttpPost]
    public async Task<IActionResult> CreateChat(CreateChatRequest request)
    {
        var chat = await _chatService.CreateChatAsync(
            request.UserId,
            request.Title
        );

        return Ok(new ChatResponse
        {
            ChatId = chat.ChatId,
            ChatTitle = chat.ChatTitle,
            CreatedAt = chat.CreatedAt
        });
    }
}