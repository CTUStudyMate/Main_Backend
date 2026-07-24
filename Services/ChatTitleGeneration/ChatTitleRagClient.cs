using System.Net.Http.Json;
using System.Text.Json;
using MainBackend.Configurations;
using MainBackend.Models;
using Microsoft.Extensions.Options;

namespace MainBackend.Services.ChatTitleGeneration;

/// <summary>
/// Boundary for chat-title generation in the RAG engine.
/// </summary>
public sealed class ChatTitleRagClient : IChatTitleGenerator
{
    private const int MaxRetryCount = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    private readonly HttpClient _httpClient;
    private readonly RagEngineOptions _ragOptions;
    private readonly ILogger<ChatTitleRagClient> _logger;

    public ChatTitleRagClient(
        HttpClient httpClient,
        IOptions<RagEngineOptions> ragOptions,
        ILogger<ChatTitleRagClient> logger)
    {
        _httpClient = httpClient;
        _ragOptions = ragOptions.Value;
        _logger = logger;
    }

    public async Task<string> GenerateTitleAsync(
        string titleSource,
        CancellationToken cancellationToken = default)
    {
        var endpoint =
            $"{_ragOptions.BaseUrl.TrimEnd('/')}/generate-chat-title";

        for (var retryCount = 0; retryCount <= MaxRetryCount; retryCount++)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(
                    endpoint,
                    new GenerateChatTitleRequest
                    {
                        Content = titleSource
                    },
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content
                        .ReadAsStringAsync(cancellationToken);

                    throw new HttpRequestException(
                        $"RAG title generation returned " +
                        $"{(int)response.StatusCode} {response.ReasonPhrase}. " +
                        $"Response: {errorBody}");
                }

                var generatedTitle = await response.Content
                    .ReadFromJsonAsync<string>(cancellationToken);

                if (string.IsNullOrWhiteSpace(generatedTitle))
                {
                    throw new JsonException(
                        "RAG title generation returned an empty title.");
                }

                return generatedTitle.Trim();
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (retryCount == MaxRetryCount)
                {
                    _logger.LogError(
                        exception,
                        "RAG chat-title generation failed after {AttemptCount} attempts. " +
                        "Using titleSource as the chat title.",
                        MaxRetryCount + 1);
                    break;
                }

                _logger.LogWarning(
                    exception,
                    "RAG chat-title generation attempt {AttemptNumber} failed. " +
                    "Retrying in {RetryDelaySeconds} second.",
                    retryCount + 1,
                    RetryDelay.TotalSeconds);

                await Task.Delay(RetryDelay, cancellationToken);
            }
        }

        return titleSource;
    }
}
