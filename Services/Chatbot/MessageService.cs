using System.Net.Http.Json;
using System.Text.Json;
using MainBackend.Models;
using Microsoft.EntityFrameworkCore;
using MainBackend.Configurations;
using Microsoft.Extensions.Options;
using MainBackend.Common.Exceptions;

namespace MainBackend.Services;

public class MessageService
{
    private readonly AppDbContext _context;
    private readonly RagEngineOptions _ragOptions;
    private readonly HttpClient _httpClient;

    public MessageService(AppDbContext context, HttpClient httpClient, IOptions<RagEngineOptions> ragOptions)
    {
        _context = context;
        _httpClient = httpClient;
        _ragOptions = ragOptions.Value;
    }
    public async Task<Message> RespondQueryAsync(RespondUserRequest request, int userId)
    {
        // get messages in chat, get user_id
        // request rag engine with query and messages
        // insert user message and ai message to db
        // return ai message

        var currentChat = await _context.Chats
            .FirstOrDefaultAsync(c => c.ChatId == request.ChatId)
            ?? throw new KeyNotFoundException($"No chat found with id {request.ChatId}");

        if (currentChat.UserId != userId) throw new UnauthorizedAccessException("Chat not owned by user");
        var messages = await _context.Messages
                            .Where(m => m.ChatId == request.ChatId)
                            .OrderByDescending(m => m.CreatedAt)
                            .Take(20)
                            .OrderBy(m => m.CreatedAt).ToListAsync();


        var ragMessages = messages.Select(m => new MessageToRag
        {
            Content = m.Content,
            SenderType = m.SenderType.ToString().ToLower(),
            MessageId = m.MessageId
        }).ToList();

        var payload = new
        {
            messages = ragMessages,
            query = request.Content
        };
        var response = await _httpClient.PostAsJsonAsync(
            $"{_ragOptions.BaseUrl}/chat",
            payload
        );

        if (!response.IsSuccessStatusCode)
        {
            throw new RagException("RAG engine failed");
        }

        RagResponse? result = null;

        try
        {
            result = await response.Content.ReadFromJsonAsync<RagResponse>();
        }
        catch (JsonException ex)
        {
            throw new RagException(ex.Message);
        }

        if (result == null)
        {
            throw new RagException("RAG returned null body");
        }
        var now = DateTime.UtcNow;
        var UserMessage = new Message
        {
            MessageId = request.MessageId,
            Content = request.Content,
            SenderType = MessageSenderType.User,
            CreatedAt = now,
            UserId = userId,
            ChatId = request.ChatId,
            MessageSegments = new List<RagSegment> { } // user_segments is an empty list. Only AI needs segments
        };

        await _context.Messages.AddAsync(UserMessage);

        var AIMessage = new Message
        {
            MessageId = Guid.NewGuid(),
            Content = result.Content,
            MessageSegments = result.Segments,
            SenderType = MessageSenderType.Assistant,
            CreatedAt = now,
            ChatId = request.ChatId,
            UserId = userId
        };

        await _context.Messages.AddAsync(AIMessage);

        if (result.NeedVerify)
        {
            var documentIds = result.DocumentIds
                .Select(documentId => int.TryParse(documentId, out var parsedId) ? parsedId : (int?)null)
                .Where(documentId => documentId.HasValue)
                .Select(documentId => documentId!.Value)
                .Distinct()
                .ToList();

            var relatedDocuments = await _context.Documents
                .Include(document => document.Courses)
                .Where(document => documentIds.Contains(document.DocumentId))
                .ToListAsync();

            var relatedCourses = relatedDocuments
                .SelectMany(document => document.Courses)
                .GroupBy(course => course.CourseId)
                .OrderByDescending(group => group.Count())
                .Take(3)
                .Select(group => group.First())
                .ToList();

            var verifiableQa = new VerifiableQa
            {
                MessageId = AIMessage.MessageId,
                UserId = userId,
                OriginalQuestion = request.Content,
                RewrittenQuestion = result.RewrittenQuestion,
                // GeneratedAnswer = result.Content,
                GeneratedAnswer = result.Segments.Count > 0
                    ? JsonSerializer.Serialize(result.Segments)
                    : result.Content,
                Status = VerifiableQaStatus.Pending,
                ApprovedAnswer = null,
                Embedding = null,
                CreatedAt = now,
                UpdatedAt = now,
                Message = AIMessage
            };

            foreach (var course in relatedCourses)
            {
                verifiableQa.Courses.Add(course);
            }

            AIMessage.VerifiableQa = verifiableQa;
            await _context.VerifiableQas.AddAsync(verifiableQa);
        }

        currentChat.LastMessageAt = now;

        await _context.SaveChangesAsync();
        return AIMessage;
    }


    public async Task<List<MessageToFrontend>> GetMessagesByChatIdAsync(Guid chatId, int limit = 50)
    {
        try
        {
            var exists = await _context.Chats.AnyAsync(c => c.ChatId == chatId);
            if (!exists)
                throw new KeyNotFoundException($"Chat {chatId} not found.");

            return await _context.Messages
                .Where(m => m.ChatId == chatId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new MessageToFrontend
                {
                    MessageId = m.MessageId,
                    Content = m.Content,
                    MessageSegments = m.MessageSegments,
                    IsVerify = m.VerifiableQa != null &&
                        m.VerifiableQa.Status == VerifiableQaStatus.Approved,
                    CreatedAt = m.CreatedAt ?? DateTime.UtcNow,
                    SenderType = m.SenderType.ToString().ToLower(),
                    ChatId = m.ChatId
                })
                .ToListAsync();
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to get recent messages by chat id", ex);
        }
    }
}
