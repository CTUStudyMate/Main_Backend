using System.Net.Http.Json;
using MainBackend.Configurations;
using Microsoft.Extensions.Options;

namespace MainBackend.Services.RagEngine;

public sealed class DocumentRagClient
{
    private const string ServiceTokenHeader = "X-Internal-Service-Token";

    private readonly HttpClient _httpClient;
    private readonly RagEngineOptions _options;

    public DocumentRagClient(
        HttpClient httpClient,
        IOptions<RagEngineOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task QueueDocumentAsync(
        int documentId,
        Guid processingRunId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            string.IsNullOrWhiteSpace(_options.ServiceToken))
        {
            throw new InvalidOperationException("RAG engine URL or service token is not configured.");
        }

        var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/internal/documents/process";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new
            {
                document_id = documentId,
                processing_run_id = processingRunId,
            }),
        };
        request.Headers.Add(ServiceTokenHeader, _options.ServiceToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"RAG engine rejected document {documentId}: " +
                $"{(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }
    }
}
