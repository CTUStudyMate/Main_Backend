using Microsoft.AspNetCore.Mvc;
using MainBackend.Services;
using MainBackend.Models;
using Microsoft.AspNetCore.Authorization;
using MainBackend.Configurations;
using Microsoft.Extensions.Options;
namespace MainBackend.Controllers;


using System.Security.Claims;

[ApiController]
[Route("api/rag")]
public class RagDataController : ControllerBase
{

    public readonly HttpClient _httpClient;
    private readonly RagEngineOptions _ragOptions;

    public RagDataController(HttpClient httpClient, IOptions<RagEngineOptions> ragEngineOptions)
    {
        _httpClient = httpClient;
        _ragOptions = ragEngineOptions.Value;
    }
    [HttpGet("chunks/text")]
    public async Task<IActionResult> GetChunkText([FromQuery] List<string> chunkIds)
    {
        var query = string.Join("&", chunkIds.Select(id => $"chunk_ids={id}"));
        var response = await _httpClient.GetAsync(
            $"{_ragOptions.BaseUrl}/chunks/text?{query}");

        var content = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = contentType
        };
    }

    [HttpGet("chunks/images/{image_id}")]
    public async Task<IActionResult> GetChunkImage(string image_id)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_ragOptions.BaseUrl}/chunks/images/{image_id}"
            );

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(
                    (int)response.StatusCode,
                    await response.Content.ReadAsStringAsync()
                );
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync();

            var contentType = response.Content.Headers.ContentType?.MediaType
                              ?? "image/png";

            return File(imageBytes, contentType);
        }
        catch (HttpRequestException e)
        {
            return StatusCode(503, new
            {
                message = "Cannot connect to RAG Engine",
                detail = e.Message
            });
        }
    }
}