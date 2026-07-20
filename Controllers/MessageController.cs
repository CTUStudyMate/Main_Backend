using Microsoft.AspNetCore.Mvc;
using MainBackend.Services;
using MainBackend.Models;
using Microsoft.AspNetCore.Authorization;
namespace MainBackend.Controllers;

using System.Security.Claims;

[ApiController]
[Route("api/")]
public class MessageController : ControllerBase
{
    private readonly MessageService _messageService;
    public MessageController(MessageService messageService)
    {
        _messageService = messageService;
    }

    [Authorize]
    [HttpPost("chats/{chatId}/messages")]
    public async Task<IActionResult> RespondwithAIMessage(RespondUserRequest request)
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user id")
        );

        var aiMessage = await _messageService.RespondQueryAsync(request, userId);
    
        return Ok(new MessageToFrontend
        {
            MessageId = aiMessage.MessageId,
            ChatId = aiMessage.ChatId,
            Content = aiMessage.Content,
            CreatedAt = aiMessage.CreatedAt ?? DateTime.UtcNow,
            SenderType = aiMessage.SenderType.ToString().ToLower(),
            MessageSegments = aiMessage.MessageSegments,
            IsVerify = aiMessage.VerifiableQa?.Status == VerifiableQaStatus.Approved
        });
    }

    [Authorize]
    [HttpGet("chats/{chatId}/messages")]
    public async Task<IActionResult> GetMessagesByChatId(Guid chatId)
    {
        var messages = await _messageService.GetMessagesByChatIdAsync(chatId);
        return Ok(messages);
    }
}
