using System.Net.Http.Json;
using System.Text.Json;
using MainBackend.Configurations;
using MainBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MainBackend.Services;

public class ChatService
{
    private readonly AppDbContext _context;
    private readonly RagEngineOptions _ragOptions;
    private readonly HttpClient _httpClient;

    public ChatService(AppDbContext context, HttpClient httpClient, IOptions<RagEngineOptions> ragOptions)
    {
        _context = context;
        _httpClient = httpClient;
        _ragOptions = ragOptions.Value;
    }

    public async Task<Chat> CreateChatAsync(int userId, string title)
    {
        var chat = new Chat
        {
            ChatId = Guid.NewGuid(),
            ChatTitle = title,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        _context.Chats.Add(chat);
        await _context.SaveChangesAsync();

        return chat;
    }

    public async Task<Message> ResponseQueryAsync(Guid chatId, string query)
    {
        var messages = await _context.Messages
                            .Where(m => m.ChatId == chatId)
                            .OrderBy(m => m.CreatedAt)
                            .Take(50).ToListAsync();

        var payload = new
        {
            messages = messages,
            query = query
        };
        var response = await _httpClient.PostAsJsonAsync(
            $"{_ragOptions.BaseUrl}/api/rag",
            payload
        );

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("RAG engine failed");
        }

        var result = await response.Content.ReadFromJsonAsync<RagResponse>();

        if (result == null)
        {
            throw new Exception("RAG returned null body");
        }

        var AIMessage = new Message
        {
            MessageId = Guid.NewGuid(),
            Content = result.Content,
            MessageSegments = JsonSerializer.Serialize(result.Segments),
            SenderType = "assistant",
            CreatedAt = DateTime.UtcNow
        };

        await _context.Messages.AddAsync(AIMessage);
        await _context.SaveChangesAsync();

        return AIMessage;
    }

}