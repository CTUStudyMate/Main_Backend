using Microsoft.AspNetCore.Mvc;
using MainBackend.Services;
using MainBackend.Models;
using Microsoft.AspNetCore.Authorization;
namespace MainBackend.Controllers;

using System.Security.Claims;
using System.Text.Json;
using MainBackend.Models.DTOs.VerifiableQa;

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

        VerifiedAnswerDto? verifiedAnswer = null;

        if (aiMessage.VerifiableQa is not null && aiMessage.VerifiableQa.ApprovedAnswer is not null
            && aiMessage.VerifiableQa.Status == VerifiableQaStatus.Approved)
        {
            using var document = JsonDocument.Parse(aiMessage.VerifiableQa.ApprovedAnswer);
            var root = document.RootElement;

            var citations = new Dictionary<string, JsonElement>();
            foreach (var citation in root.GetProperty("citations").EnumerateObject())
            {
                var citationElement = citation.Value;
                var citationType = citationElement.GetProperty("type").GetString();

                object citationDto = citationType switch
                {
                    "text" => new TextVerifiedCitationDto
                    {
                        MarkNumber = citationElement.GetProperty("markNumber").GetInt32(),
                        DocId = citationElement.GetProperty("docId").GetString()!,
                        Evidences = citationElement
                            .GetProperty("evidences")
                            .EnumerateArray()
                            .Select(evidence => evidence.GetString()!)
                            .ToList()
                    },
                    "image" => new ImageVerifiedCitationDto
                    {
                        MarkNumber = citationElement.GetProperty("markNumber").GetInt32(),
                        DocId = citationElement.GetProperty("docId").GetString()!,
                        ImageId = citationElement.GetProperty("imageId").GetString()!
                    },
                    _ => throw new JsonException(
                        $"Unsupported verified citation type: {citationType}")
                };

                citations[citation.Name] = JsonSerializer
                    .SerializeToElement(citationDto);
            }


            verifiedAnswer = new VerifiedAnswerDto
            {
                EditedAnswer = root.GetProperty("editedAnswer").GetString()!,
                Citations = citations,
                CitationMap = root.GetProperty("citationMap")
                .EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => property.Value.GetInt32())
            };
        }

        return Ok(new MessageToFrontend
        {
            MessageId = aiMessage.MessageId,
            ChatId = aiMessage.ChatId,
            Content = aiMessage.Content,
            CreatedAt = aiMessage.CreatedAt ?? DateTime.UtcNow,
            SenderType = aiMessage.SenderType.ToString().ToLower(),
            MessageSegments = aiMessage.MessageSegments,
            IsVerify = aiMessage.VerifiableQa?.Status == VerifiableQaStatus.Approved,
            VerifiedAnswer = verifiedAnswer
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
