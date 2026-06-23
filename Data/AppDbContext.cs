using Microsoft.EntityFrameworkCore;
using MainBackend.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }


    public DbSet<User> Users { get; set; }
    public DbSet<Chat> Chats { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>()
            .Property(x => x.MessageSegments)
            .HasColumnType("jsonb");

        base.OnModelCreating(modelBuilder);
    }
}