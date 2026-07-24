namespace MainBackend.Services.ChatTitleGeneration;

public sealed record ChatTitleJob(
    Guid ChatId,
    int UserId,
    string TitleSource);
