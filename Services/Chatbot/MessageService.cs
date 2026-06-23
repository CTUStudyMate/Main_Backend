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
    public async Task<Message> RespondQueryAsync(Guid chatId, string query)
    {
        // get messages in chat, get user_id
        // request rag engine with query and messages
        // insert user message and ai message to db
        // return ai message

        var messages = await _context.Messages
                            .Where(m => m.ChatId == chatId)
                            .OrderByDescending(m => m.CreatedAt)
                            .Take(50)
                            .OrderBy(m => m.CreatedAt).ToListAsync();

        var currentChat = await _context.Chats
            .FirstOrDefaultAsync(c => c.ChatId == chatId)
            ?? throw new KeyNotFoundException($"No chat found with id {chatId}");

        var userId = currentChat.UserId;

        var ragMessages = messages.Select(m => new MessageToRag
        {
            Content = m.Content,
            SenderType = m.SenderType,
            MessageId = m.MessageId
        }).ToList();

        var payload = new
        {
            messages = ragMessages,
            query = query
        };
        var response = await _httpClient.PostAsJsonAsync(
            $"{_ragOptions.BaseUrl}/chat",
            payload
        );

        if (!response.IsSuccessStatusCode)
        {
            throw new RagException("RAG engine failed");
        }

        var result = await response.Content.ReadFromJsonAsync<RagResponse>();

        if (result == null)
        {
            throw new RagException("RAG returned null body");
        }

        var UserMessage = new Message
        {
            MessageId = Guid.NewGuid(),
            Content = query,
            SenderType = "user",
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            ChatId = chatId,
            MessageSegments = new List<RagSegment>{}
        };

        await _context.Messages.AddAsync(UserMessage);

        var AIMessage = new Message
        {
            MessageId = Guid.NewGuid(),
            Content = result.Content,
            MessageSegments = result.Segments,
            SenderType = "assistant",
            CreatedAt = DateTime.UtcNow,
            ChatId = chatId,
            UserId = userId
        };

        await _context.Messages.AddAsync(AIMessage);
        await _context.SaveChangesAsync();

        return AIMessage;
    }
}