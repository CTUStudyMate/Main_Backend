using Microsoft.AspNetCore.Mvc;
using MainBackend.Services;
using MainBackend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MainBackend.Services.ChatTitleGeneration;
namespace MainBackend.Controllers;

[ApiController]
[Route("api/chats")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;
    private readonly IChatTitleJobQueue _chatTitleJobQueue;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        ChatService chatService,
        IChatTitleJobQueue chatTitleJobQueue,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _chatTitleJobQueue = chatTitleJobQueue;
        _logger = logger;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateChat(
        [FromBody] CreateChatRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        if (request.ChatId == Guid.Empty)
        {
            return BadRequest(new
            {
                code = "chat:invalid_chat_id",
                cause = "chatId must be a valid, non-empty UUID."
            });
        }

        if (string.IsNullOrWhiteSpace(request.TitleSource))
        {
            return BadRequest(new
            {
                code = "chat:invalid_title_source",
                cause = "titleSource must be a non-empty string."
            });
        }

        try
        {
            var result = await _chatService.CreateChatAsync(
                userId,
                request.ChatId,
                cancellationToken);

            if (!result.BelongsToUser)
            {
                return Conflict(new
                {
                    code = "chat:id_conflict",
                    cause = "The chatId is already associated with another user."
                });
            }

            // Requeue a marker chat on an idempotent retry as well. This lets a
            // retry recover title generation after an application restart.
            if (result.Chat.ChatTitle == ChatService.GeneratingTitleMarker)
            {
                await _chatTitleJobQueue.EnqueueAsync(
                    new ChatTitleJob(
                        result.Chat.ChatId,
                        userId,
                        request.TitleSource),
                    cancellationToken);
            }

            var response = ToResponse(result.Chat);
            return result.Created
                ? StatusCode(StatusCodes.Status201Created, response)
                : Ok(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unable to create chat {ChatId} for user {UserId}.",
                request.ChatId,
                userId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    code = "chat:create_failed",
                    cause = "Unable to create the chat."
                });
        }
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

    [Authorize]
    [HttpGet("{chatId}")]
    public async Task<IActionResult> GetChatById([FromRoute] Guid chatId)
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user id claim")
        );

        var chat = await _chatService.GetChatByIdAsync(userId, chatId);

        if (chat == null)
        {
            return NotFound(new { message = "Chat not found" });
        }

        return Ok(new ChatResponse
        {
            ChatId = chat.ChatId,
            ChatTitle = chat.ChatTitle,
            CreatedAt = chat.CreatedAt,
            LastMessageAt = chat.LastMessageAt
        });
    }

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            out userId);
    }

    private static ChatResponse ToResponse(Chat chat)
    {
        return new ChatResponse
        {
            ChatId = chat.ChatId,
            ChatTitle = chat.ChatTitle,
            CreatedAt = chat.CreatedAt,
            LastMessageAt = chat.LastMessageAt
        };
    }
}
