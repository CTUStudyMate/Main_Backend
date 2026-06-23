using MainBackend.Models;
using Microsoft.Extensions.Options;
using MainBackend.Common.Exceptions;


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
            UserId = userId
        };

        _context.Chats.Add(chat);
        await _context.SaveChangesAsync();

        return chat;
    }

    

}