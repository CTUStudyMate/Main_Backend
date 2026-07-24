namespace MainBackend.Services.ChatTitleGeneration;

public interface IChatTitleGenerator
{
    Task<string> GenerateTitleAsync(
        string titleSource,
        CancellationToken cancellationToken = default);
}
