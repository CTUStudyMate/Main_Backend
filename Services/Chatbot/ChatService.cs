using MainBackend.Models;
using Microsoft.Extensions.Options;
using MainBackend.Common.Exceptions;
using Microsoft.EntityFrameworkCore;


namespace MainBackend.Services;

public class ChatService
{
    public const string GeneratingTitleMarker = "[SYSTEM:GENERATING_TITLE]";

    private readonly AppDbContext _context;

    public ChatService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CreateChatResult> CreateChatAsync(
        int userId,
        Guid chatId,
        CancellationToken cancellationToken = default)
    {
        var existingChat = await _context.Chats
            .AsNoTracking()
            .FirstOrDefaultAsync(
                chat => chat.ChatId == chatId,
                cancellationToken);

        if (existingChat is not null)
        {
            return new CreateChatResult(
                existingChat,
                Created: false,
                BelongsToUser: existingChat.UserId == userId);
        }

        var now = DateTime.UtcNow;
        var chat = new Chat
        {
            ChatId = chatId,
            ChatTitle = GeneratingTitleMarker,
            CreatedAt = now,
            UserId = userId,
            LastMessageAt = now
        };

        _context.Chats.Add(chat);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return new CreateChatResult(
                chat,
                Created: true,
                BelongsToUser: true);
        }
        catch (DbUpdateException)
        {
            // A concurrent retry can insert the same client-generated id between
            // the read above and SaveChanges. Resolve that race as idempotency.
            _context.Entry(chat).State = EntityState.Detached;

            var concurrentlyCreatedChat = await _context.Chats
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    existing => existing.ChatId == chatId,
                    cancellationToken);

            if (concurrentlyCreatedChat is null)
            {
                throw;
            }

            return new CreateChatResult(
                concurrentlyCreatedChat,
                Created: false,
                BelongsToUser: concurrentlyCreatedChat.UserId == userId);
        }
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

    public async Task<Chat?> GetChatByIdAsync(int userId, Guid chatId)
    {
        return await _context.Chats
            .FirstOrDefaultAsync(c => c.ChatId == chatId && c.UserId == userId);
    }

}

public sealed record CreateChatResult(
    Chat Chat,
    bool Created,
    bool BelongsToUser);
