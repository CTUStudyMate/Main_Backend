using Microsoft.EntityFrameworkCore;
using MainBackend.Models;
using MainBackend.Models.BackgroundWorker;
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
    public DbSet<MatchingPair> MatchingPairs { get; set; }
    public DbSet<QuestionResponse> QuestionResponses { get; set; }
    public DbSet<QuestionItem> QuestionItems { get; set; }
    public DbSet<BackgroundJob> BackgroundJobs { get; set; }
    public DbSet<BackgroundJobLog> BackgroundJobLogs { get; set; }

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

        modelBuilder.Entity<Document>()
            .Property(x => x.ProcessingStatus)
            .HasConversion(
                status => ToSnakeCase(status),
                status => ParseSnakeCaseEnum<DocumentProcessingStatus>(status));

        modelBuilder.Entity<VerifiableQa>()
            .Property(x => x.Status)
            .HasConversion(
                status => status.ToString().ToLower(),
                status => Enum.Parse<VerifiableQaStatus>(status, true));

        modelBuilder.Entity<VerifiableQa>()
            .Property(x => x.Embedding)
            .HasColumnType("vector(1536)");

        modelBuilder.Entity<VerifiableQa>()
            .HasIndex(x => x.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasStorageParameter("m", 16)
            .HasStorageParameter("ef_construction", 64);

        modelBuilder.Entity<BackgroundJob>()
            .HasKey(job => job.JobId);

        modelBuilder.Entity<BackgroundJob>()
            .Property(x => x.Type)
            .HasConversion(
                value => ToSnakeCase(value),
                value => ParseBackgroundJobType(value));

        modelBuilder.Entity<BackgroundJob>()
            .Property(x => x.Status)
            .HasConversion(
                value => ToSnakeCase(value),
                value => ParseBackgroundJobStatus(value));

        modelBuilder.Entity<BackgroundJob>()
            .Property(x => x.SourceEntityType)
            .HasConversion(
                value => ToSnakeCase(value),
                value => ParseBackgroundJobSourceEntityType(value));

        modelBuilder.Entity<BackgroundJob>()
            .Property(x => x.BusinessData)
            .HasColumnType("jsonb");

        modelBuilder.Entity<BackgroundJobLog>()
            .Property(x => x.Level)
            .HasConversion(
                value => ToSnakeCase(value),
                value => ParseBackgroundJobLogLevel(value));

        modelBuilder.Entity<BackgroundJobLog>()
            .Property(x => x.EventType)
            .HasConversion(
                value => ToSnakeCase(value),
                value => ParseBackgroundJobLogEventType(value));

        modelBuilder.Entity<BackgroundJobLog>()
            .HasOne(log => log.Job)
            .WithMany()
            .HasForeignKey(log => log.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BackgroundJob>()
            .HasOne(job => job.VerifiableQa)
            .WithMany()
            .HasForeignKey(job => job.VerifiableQaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BackgroundJob>()
            .HasOne(job => job.CuratedQa)
            .WithMany()
            .HasForeignKey(job => job.CuratedQaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BackgroundJob>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_BackgroundJobs_ValidSourceEntity",
                """
                ("SourceEntityType" = 'verifiable_qa'
                    AND "VerifiableQaId" IS NOT NULL
                    AND "CuratedQaId" IS NULL)
                OR
                ("SourceEntityType" = 'curated_qa'
                    AND "CuratedQaId" IS NOT NULL
                    AND "VerifiableQaId" IS NULL)
                """));

        modelBuilder.Entity<User>()
            .HasMany(x => x.Courses)
            .WithMany(x => x.Lecturers)
            .UsingEntity("LecturerCourses");

        modelBuilder.Entity<Document>()
            .HasMany(x => x.Courses)
            .WithMany()
            .UsingEntity("DocumentCourses");

        modelBuilder.Entity<VerifiableQa>()
            .HasMany(x => x.Courses)
            .WithMany()
            .UsingEntity("VerifiableQaCourses");

        modelBuilder.Entity<VerifiableQa>()
            .HasOne(x => x.SourceMessage)
            .WithOne()
            .HasForeignKey<VerifiableQa>(x => x.SourceMessageId);

        modelBuilder.Entity<VerifiableQa>()
            .HasOne(x => x.ApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Message>()
            .HasOne(x => x.VerifiableQa)
            .WithMany()
            .HasForeignKey(x => x.VerifiableQaId)
            .OnDelete(DeleteBehavior.Restrict);

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

        modelBuilder.Entity<MatchingPair>()
            .HasOne(pair => pair.CuratedQa)
            .WithMany()
            .HasForeignKey(pair => pair.CuratedQaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MatchingPair>()
            .Property(pair => pair.RelationType)
            .HasConversion(
                value => ToSnakeCase(value),
                value => ParseSnakeCaseEnum<MatchingRelationType>(value));

        modelBuilder.Entity<MatchingPair>()
            .Property(pair => pair.Embedding)
            .HasColumnType("vector(1536)");

        modelBuilder.Entity<MatchingPair>()
            .HasIndex(pair => pair.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasStorageParameter("m", 16)
            .HasStorageParameter("ef_construction", 64);

        base.OnModelCreating(modelBuilder);
    }

    private static string ToSnakeCase<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        return JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());
    }

    private static BackgroundJobType ParseBackgroundJobType(string value)
    {
        return ParseSnakeCaseEnum<BackgroundJobType>(value);
    }

    private static BackgroundJobStatus ParseBackgroundJobStatus(string value)
    {
        return ParseSnakeCaseEnum<BackgroundJobStatus>(value);
    }

    private static BackgroundJobSourceEntityType ParseBackgroundJobSourceEntityType(string value)
    {
        return ParseSnakeCaseEnum<BackgroundJobSourceEntityType>(value);
    }

    private static BackgroundJobLogLevel ParseBackgroundJobLogLevel(string value)
    {
        return ParseSnakeCaseEnum<BackgroundJobLogLevel>(value);
    }

    private static BackgroundJobLogEventType ParseBackgroundJobLogEventType(string value)
    {
        return ParseSnakeCaseEnum<BackgroundJobLogEventType>(value);
    }

    private static TEnum ParseSnakeCaseEnum<TEnum>(string value)
        where TEnum : struct, Enum
    {
        return Enum.Parse<TEnum>(value.Replace("_", string.Empty), ignoreCase: true);
    }
}
