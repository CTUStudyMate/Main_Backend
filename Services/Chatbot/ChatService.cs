using MainBackend.Models;
using Microsoft.Extensions.Options;
using MainBackend.Common.Exceptions;
using Microsoft.EntityFrameworkCore;


namespace MainBackend.Services;

public class ChatService
{
    private readonly AppDbContext _context;

    public ChatService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Chat> CreateChatAsync(int userId, string title)
    {
        var chat = new Chat
        {
            ChatId = Guid.NewGuid(),
            ChatTitle = title,
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            LastMessageAt = DateTime.UtcNow
        };

        _context.Chats.Add(chat);
        await _context.SaveChangesAsync();

        return chat;
    }

    public async Task<GetChatsResponse> GetChatsByUserId(int userId, GetChatsRequest request)
    {
        var extendedLimit = request.Limit + 1;

        IQueryable<Chat> query = _context.Chats
            .Where(c => c.UserId == userId);

        // =========================
        // STARTING AFTER (newer than cursor)
        // =========================
        if (request.StartingAfter is not null)
        {
            if (!Guid.TryParse(request.StartingAfter, out var startChatId))
                throw new ArgumentException("Invalid StartingAfter chatId");

            var cursorChat = await _context.Chats
                .FirstOrDefaultAsync(c => c.ChatId == startChatId)
                ?? throw new KeyNotFoundException("Cursor chat not found");

            query = query.Where(c => c.LastMessageAt > cursorChat.LastMessageAt);
        }

        // =========================
        // ENDING BEFORE (older than cursor)
        // =========================
        else if (request.EndingBefore is not null)
        {
            if (!Guid.TryParse(request.EndingBefore, out var endChatId))
                throw new ArgumentException("Invalid EndingBefore chatId");

            var cursorChat = await _context.Chats
                .FirstOrDefaultAsync(c => c.ChatId == endChatId)
                ?? throw new KeyNotFoundException("Cursor chat not found");

            query = query.Where(c => c.LastMessageAt < cursorChat.LastMessageAt);
        }

        // =========================
        // CORE QUERY
        // =========================
        var chats = await query
            .OrderByDescending(c => c.LastMessageAt)
            .Take(extendedLimit)
            .ToListAsync();

        // =========================
        // HAS MORE
        // =========================
        var hasMore = chats.Count > request.Limit;

        if (hasMore)
            chats = chats.Take(request.Limit).ToList();

        // =========================
        // RESPONSE
        // =========================
        return new GetChatsResponse
        {
            Chats = chats.Select(c => new ChatResponse
            {
                ChatId = c.ChatId,
                ChatTitle = c.ChatTitle,
                CreatedAt = c.CreatedAt,
                LastMessageAt = c.LastMessageAt
            }).ToList(),
            HasMore = hasMore
        };
    }

}