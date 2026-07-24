using System.Text.Json.Serialization;

namespace MainBackend.Models;

public class GenerateExercisesResponse
{
    [JsonPropertyName("mcq")]
    public required GeneratedMcqResponse Mcq { get; set; }

    [JsonPropertyName("fillBlank")]
    public required GeneratedFillBlankResponse FillBlank { get; set; }

    [JsonPropertyName("matchingPair")]
    public required GeneratedMatchingPairResponse MatchingPair { get; set; }
}

public class GeneratedMcqResponse
{
    [JsonPropertyName("question")]
    public required string Question { get; set; }

    [JsonPropertyName("choices")]
    public List<string> Choices { get; set; } = [];

    [JsonPropertyName("correctChoiceIndex")]
    public int CorrectChoiceIndex { get; set; }

    [JsonPropertyName("explanation")]
    public required string Explanation { get; set; }
}

public class GeneratedFillBlankResponse
{
    [JsonPropertyName("text")]
    public required string Text { get; set; }

    [JsonPropertyName("acceptedAnswers")]
    public List<string> AcceptedAnswers { get; set; } = [];

    [JsonPropertyName("explanation")]
    public required string Explanation { get; set; }
}

public class GeneratedMatchingPairResponse
{
    [JsonPropertyName("left")]
    public required string Left { get; set; }

    [JsonPropertyName("right")]
    public required string Right { get; set; }

    [JsonPropertyName("relation")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MatchingRelationType Relation { get; set; }
}
