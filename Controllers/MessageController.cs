using Microsoft.AspNetCore.Mvc;
using MainBackend.Services;
using MainBackend.Models;
namespace MainBackend.Controllers;

[ApiController]
[Route("api/messages")]
public class MessageController: ControllerBase
{
    private readonly MessageService _messageService;
    public MessageController(MessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpPost]
    public async Task<IActionResult> RespondwithAIMessage(RespondUserRequest request)
    {
        var aiMessage = await _messageService.RespondQueryAsync(
            request.ChatId,
            request.Query
        );

        return Ok(new MessageToFrontend
        {
           MessageId = aiMessage.MessageId,
           ChatId = aiMessage.ChatId,
           Content = aiMessage.Content,
           CreatedAt = aiMessage.CreatedAt,
           SenderType = aiMessage.SenderType,
           MessageSegments = aiMessage.MessageSegments
        });
    }
}