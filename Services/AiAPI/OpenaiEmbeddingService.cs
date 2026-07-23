using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
namespace MainBackend.Services;

public class OpenaiEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;
    public OpenaiEmbeddingService(IOptions<OpenAIOptions> options)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException(
                "Environment variable OPENAI_API_KEY is missing.");

        var embeddingModel = options.Value.EmbeddingModel;

        _embeddingClient = new EmbeddingClient(
            model: embeddingModel,
            apiKey: apiKey
        );
    }
    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "Text must not be empty.",
                nameof(text));
        }

        try
        {
            var response = await _embeddingClient.GenerateEmbeddingAsync(
                text.Trim(),
                cancellationToken: cancellationToken);

            return response.Value.ToFloats().ToArray();
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                "Network error while generating embedding.",
                ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                "Embedding request timed out.",
                ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Unexpected error while generating embedding.",
                ex);
        }
    }
}