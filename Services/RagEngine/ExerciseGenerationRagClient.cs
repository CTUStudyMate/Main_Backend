using System.Net.Http.Json;
using MainBackend.Configurations;
using MainBackend.Models;
using Microsoft.Extensions.Options;

namespace MainBackend.Services.RagEngine;

public class ExerciseGenerationRagClient
{
    private readonly HttpClient _httpClient;
    private readonly RagEngineOptions _ragOptions;

    public ExerciseGenerationRagClient(
        HttpClient httpClient,
        IOptions<RagEngineOptions> ragEngineOptions)
    {
        _httpClient = httpClient;
        _ragOptions = ragEngineOptions.Value;
    }

    public async Task<GenerateExercisesResponse> GenerateAsync(
        CuratedQaToRagEngine request,
        CancellationToken cancellationToken)
    {
        var endpoint = $"{_ragOptions.BaseUrl.TrimEnd('/')}/generate-exercises";

        using var response = await _httpClient.PostAsJsonAsync(
            endpoint,
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content
                .ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Rag Engine failed to generate exercises. " +
                $"Status: {(int)response.StatusCode} {response.ReasonPhrase}. " +
                $"Response: {errorBody}");
        }

        return await response.Content.ReadFromJsonAsync<GenerateExercisesResponse>(
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "Rag Engine returned an empty generate exercises response.");
    }
}
