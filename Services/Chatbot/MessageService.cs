using System.Net.Http.Json;
using System.Text.Json;
using MainBackend.Models;
using Microsoft.EntityFrameworkCore;
using MainBackend.Configurations;
using Microsoft.Extensions.Options;
using MainBackend.Common.Exceptions;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using MainBackend.Models.DTOs.VerifiableQa;

namespace MainBackend.Services;

public class MessageService
{
    private readonly AppDbContext _context;
    private readonly RagEngineOptions _ragOptions;
    private readonly HttpClient _httpClient;
    private readonly IEmbeddingService _embeddingService;

    public MessageService(AppDbContext context, HttpClient httpClient, IOptions<RagEngineOptions> ragOptions, IEmbeddingService embeddingService)
    {
        _context = context;
        _httpClient = httpClient;
        _ragOptions = ragOptions.Value;
        _embeddingService = embeddingService;
    }
    public async Task<Message> RespondQueryAsync(RespondUserRequest request, int userId)
    {
        // check db for cached answer and return if exist
        // if no valid cached:
        // get messages in chat, get user_id
        // request rag engine with query and messages
        // insert user message and ai message to db
        // return ai message

        var currentChat = await _context.Chats
            .FirstOrDefaultAsync(c => c.ChatId == request.ChatId)
            ?? throw new KeyNotFoundException($"No chat found with id {request.ChatId}");

        if (currentChat.UserId != userId) throw new UnauthorizedAccessException("Chat not owned by user");

        List<int>? validDocumentIds = null;
        var hasSelectedPersonalDocuments = false;
        if (request.DocumentIds is not null)
        {
            var requestedDocumentIds = request.DocumentIds
                .Where(documentId => documentId > 0)
                .Distinct()
                .ToList();

            var validDocuments = await _context.Documents
                .AsNoTracking()
                .Where(document =>
                    requestedDocumentIds.Contains(document.DocumentId) &&
                    document.Visibility == DocumentVisibility.Active &&
                    document.ProcessingStatus == DocumentProcessingStatus.Ready &&
                    (
                        document.SourceType == DocumentSourceType.System ||
                        (
                            document.SourceType == DocumentSourceType.User &&
                            document.UserId == userId
                        )
                    ))
                .Select(document => new
                {
                    document.DocumentId,
                    document.SourceType,
                })
                .ToListAsync();

            validDocumentIds = validDocuments
                .Select(document => document.DocumentId)
                .ToList();
            hasSelectedPersonalDocuments = validDocuments.Any(document =>
                document.SourceType == DocumentSourceType.User);
        }

        // create user message
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


        // check for cache
        var requestMessage = request.Content;
        VerifiableQa? cachedVerifiableQa = null;
        if (!hasSelectedPersonalDocuments)
        {
            try
            {
                cachedVerifiableQa = await FindCachedVerifiableQaAsync(requestMessage);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                cachedVerifiableQa = null;
            }
        }

        if (cachedVerifiableQa is not null)
        {
            Console.WriteLine("Cached hit.");
            var cachedAiMessageCreatedAt = DateTime.UtcNow;
            var cachedAiMessage  = new Message
            {
                MessageId = Guid.NewGuid(),
                Content = string.Empty,
                SenderType = MessageSenderType.Assistant,
                CreatedAt = cachedAiMessageCreatedAt,
                ChatId = request.ChatId,
                UserId = userId,
                VerifiableQaId = cachedVerifiableQa.VerifiableQaId,
            };

            await _context.Messages.AddAsync(cachedAiMessage );
            currentChat.LastMessageAt = cachedAiMessageCreatedAt;
            await _context.SaveChangesAsync();

            await _context.Entry(cachedAiMessage)
                            .Reference(message => message.VerifiableQa)
                            .LoadAsync();
            return cachedAiMessage ;

        }

        // no cache, go to RAG
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

        var payload = new Dictionary<string, object?>
        {
            ["messages"] = ragMessages,
            ["query"] = request.Content,
        };

        if (validDocumentIds is not null)
        {
            payload["document_ids"] = validDocumentIds;
        }
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

        var aiMessageCreatedAt = DateTime.UtcNow;
        var AIMessage = new Message
        {
            MessageId = Guid.NewGuid(),
            Content = result.Content,
            MessageSegments = result.Segments,
            SenderType = MessageSenderType.Assistant,
            CreatedAt = aiMessageCreatedAt,
            ChatId = request.ChatId,
            UserId = userId
        };

        await _context.Messages.AddAsync(AIMessage);

        currentChat.LastMessageAt = aiMessageCreatedAt;
        await _context.SaveChangesAsync();

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
                SourceMessageId = AIMessage.MessageId,
                UserId = userId,
                OriginalQuestion = request.Content,
                RewrittenQuestion = result.RewrittenQuestion,
                GeneratedAnswer = result.Segments.Count > 0
                    ? JsonSerializer.Serialize(result.Segments)
                    : result.Content,
                Status = VerifiableQaStatus.Pending,
                ApprovedAnswer = null,
                Embedding = null,
                CreatedAt = now,
                UpdatedAt = now
            };

            foreach (var course in relatedCourses)
            {
                verifiableQa.Courses.Add(course);
            }

            await _context.VerifiableQas.AddAsync(verifiableQa);
            await _context.SaveChangesAsync();

            AIMessage.VerifiableQa = verifiableQa;
            await _context.SaveChangesAsync();
        }

        return AIMessage;
    }

    private const double CacheSimilarityThreshold = 0.85;

    private async Task<VerifiableQa?> FindCachedVerifiableQaAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(
            question,
            cancellationToken);

        var queryVector = new Vector(embedding);

        var closestQa = await _context.VerifiableQas
            .AsNoTracking()
            .Where(qa =>
                qa.Status == VerifiableQaStatus.Approved &&
                qa.Embedding != null &&
                !string.IsNullOrWhiteSpace(qa.ApprovedAnswer))
            .OrderBy(qa => qa.Embedding!.CosineDistance(queryVector))
            .Select(qa => new
            {
                VerifiableQa = qa,
                CosineDistance = qa.Embedding!.CosineDistance(queryVector)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (closestQa is null)
        {
            return null;
        }

        var similarity = 1 - closestQa.CosineDistance;

        return similarity >= CacheSimilarityThreshold
            ? closestQa.VerifiableQa
            : null;
    }


    public async Task<List<MessageToFrontend>> GetMessagesByChatIdAsync(Guid chatId, int limit = 50)
    {
        try
        {
            var exists = await _context.Chats.AnyAsync(c => c.ChatId == chatId);
            if (!exists)
                throw new KeyNotFoundException($"Chat {chatId} not found.");

            var messages = await _context.Messages
                .AsNoTracking()
                .Include(m => m.VerifiableQa)
                .Where(m => m.ChatId == chatId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            return messages.Select(m => new MessageToFrontend
            {
                MessageId = m.MessageId,
                Content = m.Content,
                MessageSegments = m.MessageSegments,
                IsVerify = m.VerifiableQa?.Status == VerifiableQaStatus.Approved,
                CreatedAt = m.CreatedAt ?? DateTime.UtcNow,
                SenderType = m.SenderType.ToString().ToLower(),
                ChatId = m.ChatId,
                VerifiedAnswer = MapVerifiedAnswer(m.VerifiableQa)
            }).ToList();
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

    private static VerifiedAnswerDto? MapVerifiedAnswer(VerifiableQa? verifiableQa)
    {
        if (verifiableQa?.Status != VerifiableQaStatus.Approved ||
            string.IsNullOrWhiteSpace(verifiableQa.ApprovedAnswer))
        {
            return null;
        }

        using var document = JsonDocument.Parse(verifiableQa.ApprovedAnswer);
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

            citations[citation.Name] = JsonSerializer.SerializeToElement(citationDto);
        }

        return new VerifiedAnswerDto
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

}
