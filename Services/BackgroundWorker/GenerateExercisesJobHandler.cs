using System.Text.Json;
using System.Text.Json.Serialization;
using MainBackend.Data;
using MainBackend.Models;
using MainBackend.Models.BackgroundWorker;
using MainBackend.Models.QuestionItems;
using MainBackend.Services;
using MainBackend.Services.RagEngine;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace MainBackend.Services.BackgroundWorker;

public class GenerateExercisesJobHandler : IBackgroundJobHandler
{
    private static readonly JsonSerializerOptions QuestionDataJsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

    private readonly ExerciseGenerationRagClient _ragClient;
    private readonly IEmbeddingService _embeddingService;

    public GenerateExercisesJobHandler(
        ExerciseGenerationRagClient ragClient,
        IEmbeddingService embeddingService)
    {
        _ragClient = ragClient;
        _embeddingService = embeddingService;
    }

    public BackgroundJobType JobType => BackgroundJobType.GenerateExercises;

    public async Task HandleAsync(
        BackgroundJob job,
        CancellationToken cancellationToken,
        AppDbContext db)
    {
        // Flow:
        // 1. Load CuratedQa and course context.
        // 2. Ask the RAG Engine to generate MCQ, FillBlank, and MatchingPair.
        // 3. Build entities and generate an embedding for the new MatchingPair.
        // 4. Find compatible pairs, then create a Matching question when four pairs exist.
        // 5. Persist generated data, complete the background job, and write a log.

        var curatedQa = await GetCuratedQaAsync(job, db, cancellationToken);
        var request = CreateRagRequest(curatedQa);

        var generatedExercises = await _ragClient.GenerateAsync(
            request,
            cancellationToken);

        var timeline = new BackgroundJobTimestampSequence();
        var mcqQuestionItem = CreateMcqQuestionItem(
            curatedQa,
            generatedExercises,
            timeline.Next());
        var fillBlankQuestionItem = CreateFillBlankQuestionItem(
            curatedQa,
            generatedExercises,
            timeline.Next());
        var matchingPair = CreateMatchingPair(
            curatedQa,
            generatedExercises,
            timeline.Next());

        await GenerateMatchingPairEmbeddingAsync(
            matchingPair,
            curatedQa,
            cancellationToken);

        var compatiblePairs = await FindCompatibleMatchingPairsAsync(
            matchingPair,
            curatedQa,
            db,
            cancellationToken);

        var matchingPairs = SelectMatchingPairs(matchingPair, compatiblePairs);

        if (matchingPairs.Count < 4)
        {
            await PersistAndCompleteAsync(
                db,
                job,
                timeline,
                [mcqQuestionItem, fillBlankQuestionItem],
                matchingPair,
                $"Created MCQ and FillBlank QuestionItems and saved MatchingPair " +
                $"{matchingPair.MatchingPairId}. Matching was not created because only " +
                $"{matchingPairs.Count} of 4 required pairs are available.",
                cancellationToken);
            return;
        }

        var matchingQuestionItem = CreateMatchingQuestionItem(
            curatedQa,
            matchingPairs,
            timeline.Next());

        await PersistAndCompleteAsync(
            db,
            job,
            timeline,
            [mcqQuestionItem, fillBlankQuestionItem, matchingQuestionItem],
            matchingPair,
            $"Created MCQ, FillBlank, and Matching QuestionItems and saved " +
            $"MatchingPair {matchingPair.MatchingPairId}.",
            cancellationToken);
    }

    private static async Task<CuratedQa> GetCuratedQaAsync(
        BackgroundJob job,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (job.SourceEntityType != BackgroundJobSourceEntityType.CuratedQa ||
            !job.CuratedQaId.HasValue)
        {
            throw new InvalidOperationException(
                $"Generate exercises job {job.JobId} must reference a CuratedQa.");
        }

        var curatedQa = await db.CuratedQas
            .AsNoTracking()
            .Include(qa => qa.VerifiableQa)
                .ThenInclude(verifiableQa => verifiableQa.Courses)
            .FirstOrDefaultAsync(
                qa => qa.CuratedQaId == job.CuratedQaId.Value,
                cancellationToken);

        return curatedQa ?? throw new InvalidOperationException(
            $"Cannot generate exercises because CuratedQa {job.CuratedQaId.Value} was not found.");
    }

    private static CuratedQaToRagEngine CreateRagRequest(CuratedQa curatedQa)
    {
        var courseNames = curatedQa.VerifiableQa.Courses
            .Select(course => course.CourseName)
            .Where(courseName => !string.IsNullOrWhiteSpace(courseName))
            .Distinct()
            .ToList();

        if (courseNames.Count == 0)
        {
            throw new InvalidOperationException(
                $"Cannot generate exercises because CuratedQa {curatedQa.CuratedQaId} has no course context.");
        }

        return new CuratedQaToRagEngine
        {
            CuratedQaId = curatedQa.CuratedQaId,
            Question = curatedQa.CuratedQuestion,
            Answer = curatedQa.CuratedAnswer,
            CourseNames = courseNames
        };
    }

    private static QuestionItem CreateMcqQuestionItem(
        CuratedQa curatedQa,
        GenerateExercisesResponse generatedExercises,
        DateTime now)
    {
        return new QuestionItem
        {
            QuestionItemId = Guid.NewGuid(),
            CuratedQaId = curatedQa.CuratedQaId,
            Type = QuestionItemType.Mcq,
            QuestionData = JsonSerializer.Serialize(
                new McqQuestionData
                {
                    Instruction = "Choose the best answer.",
                    Question = generatedExercises.Mcq.Question,
                    SelectionMode = McqSelectionMode.Single,
                    Choices = generatedExercises.Mcq.Choices
                        .Select((choice, index) => new McqChoice
                        {
                            Id = $"choice_{index + 1}",
                            Text = choice
                        })
                        .ToList(),
                    CorrectChoiceIds =
                    [
                        $"choice_{generatedExercises.Mcq.CorrectChoiceIndex + 1}"
                    ],
                    Explanation = generatedExercises.Mcq.Explanation
                },
                QuestionDataJsonOptions),
            IsEnabled = true,
            CreatedAt = now
        };
    }

    private static QuestionItem CreateFillBlankQuestionItem(
        CuratedQa curatedQa,
        GenerateExercisesResponse generatedExercises,
        DateTime now)
    {
        return new QuestionItem
        {
            QuestionItemId = Guid.NewGuid(),
            CuratedQaId = curatedQa.CuratedQaId,
            Type = QuestionItemType.FillBlank,
            QuestionData = JsonSerializer.Serialize(
                new FillBlankQuestionData
                {
                    Instruction = "Fill in the blank.",
                    Question = generatedExercises.FillBlank.Text.Replace(
                        "___",
                        "{{blank_1}}",
                        StringComparison.Ordinal),
                    Blanks =
                    [
                        new BlankDefinition
                        {
                            Id = "blank_1",
                            AcceptedAnswers = generatedExercises.FillBlank.AcceptedAnswers
                                .ToList()
                        }
                    ],
                    Explanation = generatedExercises.FillBlank.Explanation
                },
                QuestionDataJsonOptions),
            IsEnabled = true,
            CreatedAt = now
        };
    }

    private static MatchingPair CreateMatchingPair(
        CuratedQa curatedQa,
        GenerateExercisesResponse generatedExercises,
        DateTime now)
    {
        return new MatchingPair
        {
            MatchingPairId = Guid.NewGuid(),
            CuratedQaId = curatedQa.CuratedQaId,
            LeftText = generatedExercises.MatchingPair.Left,
            RightText = generatedExercises.MatchingPair.Right,
            RelationType = generatedExercises.MatchingPair.Relation,
            CreatedAt = now
        };
    }

    private async Task GenerateMatchingPairEmbeddingAsync(
        MatchingPair matchingPair,
        CuratedQa curatedQa,
        CancellationToken cancellationToken)
    {
        var matchingPairEmbeddingInput = $"""
            Matching pair:
            Left: {matchingPair.LeftText}
            Right: {matchingPair.RightText}

            Knowledge context:
            Question: {curatedQa.CuratedQuestion}
            Answer: {curatedQa.CuratedAnswer}
            """;

        var embedding = await _embeddingService.GenerateEmbeddingAsync(
            matchingPairEmbeddingInput,
            cancellationToken);

        matchingPair.Embedding = new Vector(embedding);
    }

    private static async Task<List<MatchingPair>> FindCompatibleMatchingPairsAsync(
        MatchingPair matchingPair,
        CuratedQa curatedQa,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var courseIds = curatedQa.VerifiableQa.Courses
            .Select(course => course.CourseId)
            .ToList();

        return await db.MatchingPairs
            .AsNoTracking()
            .Where(pair =>
                pair.CuratedQaId != curatedQa.CuratedQaId &&
                pair.RelationType == matchingPair.RelationType &&
                pair.LeftText.ToLower() != matchingPair.LeftText.ToLower() &&
                pair.RightText.ToLower() != matchingPair.RightText.ToLower() &&
                pair.Embedding != null &&
                pair.CuratedQa.VerifiableQa.Courses.Any(
                    course => courseIds.Contains(course.CourseId)))
            .OrderBy(pair => pair.Embedding!.CosineDistance(matchingPair.Embedding!))
            .Take(7)
            .ToListAsync(cancellationToken);
    }

    private static List<MatchingPair> SelectMatchingPairs(
        MatchingPair matchingPair,
        IEnumerable<MatchingPair> compatiblePairs)
    {
        var selectedPairs = new List<MatchingPair> { matchingPair };
        var usedLeftTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            matchingPair.LeftText.Trim()
        };
        var usedRightTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            matchingPair.RightText.Trim()
        };

        foreach (var candidate in compatiblePairs)
        {
            var leftText = candidate.LeftText.Trim();
            var rightText = candidate.RightText.Trim();

            if (usedLeftTexts.Contains(leftText) ||
                usedRightTexts.Contains(rightText))
            {
                continue;
            }

            selectedPairs.Add(candidate);
            usedLeftTexts.Add(leftText);
            usedRightTexts.Add(rightText);
        }

        var matchingPairsForQuestion = new List<MatchingPair> { matchingPair };
        var randomCompatiblePairs = selectedPairs
            .Where(pair => pair.MatchingPairId != matchingPair.MatchingPairId)
            .OrderBy(_ => Random.Shared.Next())
            .Take(3);

        matchingPairsForQuestion.AddRange(randomCompatiblePairs);
        return matchingPairsForQuestion;
    }

    private static QuestionItem CreateMatchingQuestionItem(
        CuratedQa curatedQa,
        IReadOnlyCollection<MatchingPair> matchingPairs,
        DateTime now)
    {
        var matchingPairsWithIds = matchingPairs
            .Select((pair, index) => new
            {
                Pair = pair,
                LeftId = $"left_{index + 1}",
                RightId = $"right_{index + 1}"
            })
            .ToList();

        return new QuestionItem
        {
            QuestionItemId = Guid.NewGuid(),
            CuratedQaId = curatedQa.CuratedQaId,
            Type = QuestionItemType.Matching,
            QuestionData = JsonSerializer.Serialize(
                new MatchingQuestionData
                {
                    Instruction = "Match each concept with its corresponding description.",
                    Question = "Match the following concepts and descriptions.",
                    LeftItems = matchingPairsWithIds
                        .Select(item => new MatchingItem
                        {
                            Id = item.LeftId,
                            Text = item.Pair.LeftText
                        })
                        .ToList(),
                    RightItems = matchingPairsWithIds
                        .OrderBy(_ => Random.Shared.Next())
                        .Select(item => new MatchingItem
                        {
                            Id = item.RightId,
                            Text = item.Pair.RightText
                        })
                        .ToList(),
                    CorrectPairs = matchingPairsWithIds
                        .Select(item => new MatchingCorrectPair
                        {
                            LeftId = item.LeftId,
                            RightId = item.RightId
                        })
                        .ToList(),
                    SourceMatchingPairIds = matchingPairsWithIds
                        .Select(item => item.Pair.MatchingPairId)
                        .ToList()
                },
                QuestionDataJsonOptions),
            IsEnabled = true,
            CreatedAt = now
        };
    }

    private static async Task PersistAndCompleteAsync(
        AppDbContext db,
        BackgroundJob job,
        BackgroundJobTimestampSequence timeline,
        IEnumerable<QuestionItem> questionItems,
        MatchingPair matchingPair,
        string completionMessage,
        CancellationToken cancellationToken)
    {
        db.QuestionItems.AddRange(questionItems);
        db.MatchingPairs.Add(matchingPair);

        job.Status = BackgroundJobStatus.Completed;
        job.UpdatedAt = timeline.Next();
        job.NextAttemptAt = null;
        job.CompletedAt = timeline.Next();

        db.BackgroundJobLogs.Add(new BackgroundJobLog
        {
            BackgroundJobLogId = Guid.NewGuid(),
            JobId = job.JobId,
            Level = BackgroundJobLogLevel.Information,
            EventType = BackgroundJobLogEventType.Completed,
            AttemptNumber = job.AttemptCount,
            Message = $"Background job {job.JobId} completed: {job.Type}. {completionMessage}",
            CreatedAt = timeline.Next()
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
