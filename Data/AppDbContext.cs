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
    public DbSet<Major> Majors { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<VerifiableQa> VerifiableQas { get; set; }
    public DbSet<ReviewSession> ReviewSessions { get; set; }
    public DbSet<CuratedQa> CuratedQas { get; set; }
    public DbSet<QuestionResponse> QuestionResponses { get; set; }
    public DbSet<QuestionItem> QuestionItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<User>()
            .Property(x => x.Role)
            .HasConversion(
                role => role.ToString().ToLower(),
                role => Enum.Parse<UserRole>(role, true));

        modelBuilder.Entity<Document>()
            .Property(x => x.SourceType)
            .HasConversion(
                sourceType => sourceType.ToString().ToLower(),
                sourceType => Enum.Parse<DocumentSourceType>(sourceType, true));

        modelBuilder.Entity<Document>()
            .Property(x => x.Visibility)
            .HasConversion(
                visibility => visibility.ToString().ToLower(),
                visibility => Enum.Parse<DocumentVisibility>(visibility, true));

        modelBuilder.Entity<VerifiableQa>()
            .Property(x => x.Status)
            .HasConversion(
                status => status.ToString().ToLower(),
                status => Enum.Parse<VerifiableQaStatus>(status, true));

        modelBuilder.Entity<VerifiableQa>()
            .Property(x => x.Embedding)
            .HasColumnType("vector(1536)");

        modelBuilder.Entity<Message>()
            .HasOne(x => x.VerifiableQa)
            .WithOne(x => x.Message)
            .HasForeignKey<VerifiableQa>(x => x.MessageId);

        modelBuilder.Entity<Message>()
            .Property(x => x.SenderType)
            .HasConversion(
                senderType => senderType.ToString().ToLower(),
                senderType => Enum.Parse<MessageSenderType>(senderType, true));

        modelBuilder.Entity<Message>()
            .Property(x => x.MessageSegments)
            .HasColumnType("jsonb");

        modelBuilder.Entity<QuestionResponse>()
            .Property(x => x.StudentAnswer)
            .HasColumnType("jsonb");

        modelBuilder.Entity<QuestionItem>()
            .Property(x => x.Type)
            .HasConversion(
                type => type.ToString().ToLower(),
                type => Enum.Parse<QuestionItemType>(type, true));

        modelBuilder.Entity<QuestionItem>()
            .Property(x => x.QuestionData)
            .HasColumnType("jsonb");

        base.OnModelCreating(modelBuilder);
    }
}
