using System.Text.Json;
using DotNetEnv;
using OpenAI.Embeddings;

Env.Load();

var experimentDirectory = Path.Combine(
    Directory.GetCurrentDirectory(),
    "Research",
    "EmbeddingSimilarity");
var questionsPath = Path.Combine(experimentDirectory, "questions.json");
var embeddingsPath = Path.Combine(experimentDirectory, "embeddings.json");
var resultsPath = Path.Combine(experimentDirectory, "pairwise-results.json");

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException(
        "Environment variable OPENAI_API_KEY is missing.");
var model = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL")
    ?? "text-embedding-3-small";

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

var questionsJson = await File.ReadAllTextAsync(questionsPath);
var input = JsonSerializer.Deserialize<QuestionsInput>(questionsJson, jsonOptions)
    ?? throw new InvalidOperationException("Unable to parse questions.json.");

var embeddingClient = new EmbeddingClient(model, apiKey);
var embeddedQuestions = new List<EmbeddedQuestion>();

foreach (var question in input.Questions)
{
    var response = await embeddingClient.GenerateEmbeddingAsync(question.Text);

    embeddedQuestions.Add(new EmbeddedQuestion(
        question.Id,
        question.Text,
        response.Value.ToFloats().ToArray()));
}

var embeddingsOutput = new EmbeddingsOutput(
    model,
    DateTime.UtcNow,
    embeddedQuestions);
var embeddingsJson = JsonSerializer.Serialize(embeddingsOutput, jsonOptions);
await File.WriteAllTextAsync(embeddingsPath, embeddingsJson);

Console.WriteLine(
    $"Generated embeddings for {embeddedQuestions.Count} questions: {embeddingsPath}");

var pairwiseResults = new List<PairwiseSimilarity>();
for (var leftIndex = 0; leftIndex < embeddedQuestions.Count - 1; leftIndex++)
{
    var leftQuestion = embeddedQuestions[leftIndex];

    for (var rightIndex = leftIndex + 1;
         rightIndex < embeddedQuestions.Count;
         rightIndex++)
    {
        var rightQuestion = embeddedQuestions[rightIndex];
        var cosineSimilarity = CalculateCosineSimilarity(
            leftQuestion.Embedding,
            rightQuestion.Embedding);

        pairwiseResults.Add(new PairwiseSimilarity(
            leftQuestion.Id,
            rightQuestion.Id,
            cosineSimilarity));
    }
}

var resultsJson = JsonSerializer.Serialize(pairwiseResults, jsonOptions);
await File.WriteAllTextAsync(resultsPath, resultsJson);

Console.WriteLine(
    $"Calculated {pairwiseResults.Count} pairwise similarities: {resultsPath}");

static double CalculateCosineSimilarity(float[] left, float[] right)
{
    if (left.Length != right.Length)
    {
        throw new InvalidOperationException("Embedding dimensions do not match.");
    }

    double dotProduct = 0;
    double leftMagnitudeSquared = 0;
    double rightMagnitudeSquared = 0;

    for (var index = 0; index < left.Length; index++)
    {
        dotProduct += left[index] * right[index];
        leftMagnitudeSquared += left[index] * left[index];
        rightMagnitudeSquared += right[index] * right[index];
    }

    if (leftMagnitudeSquared == 0 || rightMagnitudeSquared == 0)
    {
        throw new InvalidOperationException(
            "Cannot calculate cosine similarity for a zero vector.");
    }

    return dotProduct / Math.Sqrt(leftMagnitudeSquared * rightMagnitudeSquared);
}

public sealed class QuestionsInput
{
    public List<QuestionInput> Questions { get; set; } = [];
}

public sealed class QuestionInput
{
    public string Id { get; set; } = null!;
    public string Text { get; set; } = null!;
}

public sealed record EmbeddedQuestion(string Id, string Text, float[] Embedding);

public sealed record EmbeddingsOutput(
    string Model,
    DateTime GeneratedAtUtc,
    List<EmbeddedQuestion> Questions);

public sealed record PairwiseSimilarity(
    string LeftQuestionId,
    string RightQuestionId,
    double CosineSimilarity);
