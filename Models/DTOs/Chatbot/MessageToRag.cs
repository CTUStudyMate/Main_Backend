using System.Text.Json.Serialization;
namespace MainBackend.Models;
public class MessageToRag
{
    [JsonPropertyName("message_id")]
    public Guid MessageId { get; set; }

    [JsonPropertyName("content")]
    public required string Content { get; set; }

    [JsonPropertyName("sender_type")]
    public required string SenderType { get; set; } // user | assistant

}