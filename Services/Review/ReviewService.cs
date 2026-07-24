using System.Text.Json;
using System.Text.Json.Serialization;
using MainBackend.Models;
using MainBackend.Models.QuestionItems;
using Microsoft.EntityFrameworkCore;

namespace MainBackend.Services;

public class ReviewService
{
    private static readonly JsonSerializerOptions QuestionDataJsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

    private readonly AppDbContext _context;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(
        AppDbContext context,
        ILogger<ReviewService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ReviewSessionToFrontend> GetReviewAsync(
        int userId,
        Guid reviewSessionId,
        CancellationToken cancellationToken = default)
    {
        var reviewSession = await _context.ReviewSessions
            .AsNoTracking()
            .Include(session => session.Course)
            .Include(session => session.QuestionResponses)
                .ThenInclude(response => response.QuestionItem)
            .FirstOrDefaultAsync(
                session =>
                    session.ReviewSessionId == reviewSessionId &&
                    session.UserId == userId,
                cancellationToken);

        if (reviewSession is null)
        {
            throw new KeyNotFoundException("Review session not found.");
        }

        return new ReviewSessionToFrontend
        {
            ReviewSessionId = reviewSession.ReviewSessionId,
            CourseId = reviewSession.CourseId,
            CourseCode = reviewSession.Course?.CourseCode,
            CourseName = reviewSession.Course?.CourseName,
            TotalQuestions = reviewSession.TotalQuestions,
            CreatedAt = reviewSession.CreatedAt,
            IsCompleted = reviewSession.IsCompleted,
            QuestionResponses = reviewSession.QuestionResponses
                .Select(response => new QuestionResponseToFrontend
                {
                    QuestionResponseId = response.QuestionResponseId,
                    StudentAnswer = response.StudentAnswer,
                    IsCorrect = response.IsCorrect,
                    QuestionItem = MapQuestionItemToFrontend(response.QuestionItem)
                })
                .ToList()
        };
    }

    public async Task<ReviewSessionToFrontend> RetakeReviewAsync(
        int userId,
        Guid reviewSessionId,
        CancellationToken cancellationToken = default)
    {
        var sourceReview = await _context.ReviewSessions
            .AsNoTracking()
            .Include(session => session.QuestionResponses)
                .ThenInclude(response => response.QuestionItem)
            .FirstOrDefaultAsync(
                session =>
                    session.ReviewSessionId == reviewSessionId &&
                    session.UserId == userId,
                cancellationToken);

        if (sourceReview is null)
        {
            throw new KeyNotFoundException("Review session not found.");
        }

        var now = DateTime.UtcNow;
        var reviewSession = new ReviewSession
        {
            ReviewSessionId = Guid.NewGuid(),
            UserId = userId,
            CourseId = sourceReview.CourseId,
            TotalQuestions = sourceReview.QuestionResponses.Count,
            CorrectAnswers = null,
            CreatedAt = now,
            CompletedAt = null,
            IsCompleted = false
        };
        var questionResponses = sourceReview.QuestionResponses
            .Select(sourceResponse => new QuestionResponse
            {
                QuestionResponseId = Guid.NewGuid(),
                QuestionItemId = sourceResponse.QuestionItemId,
                ReviewSessionId = reviewSession.ReviewSessionId,
                StudentAnswer = "null",
                IsCorrect = null
            })
            .ToList();

        _context.ReviewSessions.Add(reviewSession);
        _context.QuestionResponses.AddRange(questionResponses);
        await _context.SaveChangesAsync(cancellationToken);

        return new ReviewSessionToFrontend
        {
            ReviewSessionId = reviewSession.ReviewSessionId,
            CourseId = reviewSession.CourseId,
            TotalQuestions = reviewSession.TotalQuestions,
            CreatedAt = reviewSession.CreatedAt,
            IsCompleted = false,
            QuestionResponses = questionResponses
                .Join(
                    sourceReview.QuestionResponses,
                    response => response.QuestionItemId,
                    sourceResponse => sourceResponse.QuestionItemId,
                    (response, sourceResponse) => new QuestionResponseToFrontend
                    {
                        QuestionResponseId = response.QuestionResponseId,
                        StudentAnswer = response.StudentAnswer,
                        IsCorrect = null,
                        QuestionItem = MapQuestionItemToFrontend(sourceResponse.QuestionItem)
                    })
                .ToList()
        };
    }

    public async Task<ReviewSessionToFrontend> RequestReviewCreationAsync(
        int userId,
        CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var curatedQas = await _context.CuratedQas
                .AsNoTracking()
                .Where(curatedQa => curatedQa.VerifiableQa.Courses
                    .Any(course => course.CourseId == request.CourseId))
                .ToListAsync(cancellationToken);

            if (curatedQas.Count > request.QuestionCount)
            {
                for (var index = 0; index < request.QuestionCount; index++)
                {
                    var randomIndex = Random.Shared.Next(index, curatedQas.Count);

                    (curatedQas[index], curatedQas[randomIndex]) =
                        (curatedQas[randomIndex], curatedQas[index]);
                }

                curatedQas = curatedQas
                    .Take(request.QuestionCount)
                    .ToList();
            }

            var selectedQuestionTypes = curatedQas
                .Select(curatedQa => new
                {
                    curatedQa.CuratedQaId,
                    QuestionType = (QuestionItemType)Random.Shared.Next(3)
                })
                .ToList();

            var selectedCuratedQaIds = selectedQuestionTypes
                .Select(selection => selection.CuratedQaId)
                .ToList();

            var questionItems = await _context.QuestionItems
                .AsNoTracking()
                .Where(questionItem =>
                    questionItem.IsEnabled &&
                    selectedCuratedQaIds.Contains(questionItem.CuratedQaId))
                .ToListAsync(cancellationToken);

            var selectedQuestionItems = selectedQuestionTypes
                .Join(
                    questionItems,
                    selection => new
                    {
                        selection.CuratedQaId,
                        selection.QuestionType
                    },
                    questionItem => new
                    {
                        questionItem.CuratedQaId,
                        QuestionType = questionItem.Type
                    },
                    (_, questionItem) => questionItem)
                .ToList();

            if (selectedQuestionItems.Count == 0)
            {
                throw new InvalidOperationException(
                    "No enabled question items are available for the requested review.");
            }

            var now = DateTime.UtcNow;
            var reviewSession = new ReviewSession
            {
                ReviewSessionId = Guid.NewGuid(),
                UserId = userId,
                CourseId = request.CourseId,
                TotalQuestions = selectedQuestionItems.Count,
                CorrectAnswers = null,
                CreatedAt = now,
                CompletedAt = null,
                IsCompleted = false
            };

            var questionResponses = selectedQuestionItems
                .Select(questionItem => new QuestionResponse
                {
                    QuestionResponseId = Guid.NewGuid(),
                    QuestionItemId = questionItem.QuestionItemId,
                    ReviewSessionId = reviewSession.ReviewSessionId,
                    StudentAnswer = "null",
                    IsCorrect = null
                })
                .ToList();

            _context.ReviewSessions.Add(reviewSession);
            _context.QuestionResponses.AddRange(questionResponses);

            await _context.SaveChangesAsync(cancellationToken);

            return new ReviewSessionToFrontend
            {
                ReviewSessionId = reviewSession.ReviewSessionId,
                CourseId = reviewSession.CourseId,
                TotalQuestions = reviewSession.TotalQuestions,
                CreatedAt = reviewSession.CreatedAt,
                IsCompleted = reviewSession.IsCompleted,
                QuestionResponses = questionResponses
                    .Join(
                        selectedQuestionItems,
                        questionResponse => questionResponse.QuestionItemId,
                        questionItem => questionItem.QuestionItemId,
                        (questionResponse, questionItem) => new QuestionResponseToFrontend
                        {
                            QuestionResponseId = questionResponse.QuestionResponseId,
                            StudentAnswer = questionResponse.StudentAnswer,
                            QuestionItem = MapQuestionItemToFrontend(questionItem)
                        })
                    .ToList()
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "Unable to create the review. Please try again later.",
                exception);
        }
    }

    public async Task PauseReviewAsync(
        int userId,
        PauseReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var reviewSession = await _context.ReviewSessions
            .Include(session => session.QuestionResponses)
            .FirstOrDefaultAsync(
                session =>
                    session.ReviewSessionId == request.ReviewSessionId &&
                    session.UserId == userId,
                cancellationToken);

        if (reviewSession is null)
        {
            throw new KeyNotFoundException("Review session not found.");
        }

        if (reviewSession.IsCompleted)
        {
            throw new ArgumentException("A completed review cannot be paused.");
        }

        var responsesById = reviewSession.QuestionResponses
            .ToDictionary(response => response.QuestionResponseId);

        foreach (var snapshot in request.QuestionResponses)
        {
            if (!responsesById.TryGetValue(
                    snapshot.QuestionResponseId,
                    out var questionResponse))
            {
                throw new ArgumentException(
                    "A question response does not belong to this review session.");
            }

            questionResponse.StudentAnswer = snapshot.StudentAnswer;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SubmitReviewResult> SubmitReviewAsync(
        int userId,
        SubmitReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var reviewSession = await _context.ReviewSessions
            .Include(session => session.QuestionResponses)
                .ThenInclude(response => response.QuestionItem)
            .FirstOrDefaultAsync(
                session =>
                    session.ReviewSessionId == request.ReviewSessionId &&
                    session.UserId == userId,
                cancellationToken);

        if (reviewSession is null)
        {
            throw new KeyNotFoundException("Review session not found.");
        }

        if (reviewSession.IsCompleted)
        {
            throw new ArgumentException("This review has already been submitted.");
        }

        var requestResponseIds = request.QuestionResponses
            .Select(response => response.QuestionResponseId)
            .ToList();

        if (requestResponseIds.Distinct().Count() != requestResponseIds.Count)
        {
            throw new ArgumentException("Question response IDs must not be duplicated.");
        }

        var sessionResponseIds = reviewSession.QuestionResponses
            .Select(response => response.QuestionResponseId)
            .ToHashSet();

        if (requestResponseIds.Any(id => !sessionResponseIds.Contains(id)))
        {
            throw new ArgumentException(
                "A submitted question response does not belong to this review session.");
        }

        var submittedResponsesById = request.QuestionResponses
            .ToDictionary(response => response.QuestionResponseId);

        foreach (var questionResponse in reviewSession.QuestionResponses)
        {
            if (!submittedResponsesById.TryGetValue(
                    questionResponse.QuestionResponseId,
                    out var submittedResponse))
            {
                questionResponse.StudentAnswer = "null";
                questionResponse.IsCorrect = false;
                continue;
            }

            if (submittedResponse.StudentAnswer.ValueKind == JsonValueKind.Undefined)
            {
                throw new ArgumentException(
                    "Each submitted question response must include studentAnswer.");
            }

            questionResponse.StudentAnswer = submittedResponse.StudentAnswer.GetRawText();

            if (questionResponse.QuestionItem.Type == QuestionItemType.Mcq)
            {
                questionResponse.IsCorrect = GradeMcq(
                    questionResponse.QuestionItem,
                    submittedResponse.StudentAnswer);
            }
            else if (questionResponse.QuestionItem.Type == QuestionItemType.FillBlank)
            {
                questionResponse.IsCorrect = GradeFillBlank(
                    questionResponse.QuestionItem,
                    submittedResponse.StudentAnswer);
            }
            else if (questionResponse.QuestionItem.Type == QuestionItemType.Matching)
            {
                questionResponse.IsCorrect = GradeMatching(
                    questionResponse.QuestionItem,
                    submittedResponse.StudentAnswer);
            }
        }

        if (reviewSession.QuestionResponses.Any(response => response.IsCorrect is null))
        {
            throw new InvalidOperationException(
                "Unable to grade every question in the review session.");
        }

        var completedAt = DateTime.UtcNow;
        var correctAnswers = reviewSession.QuestionResponses
            .Count(response => response.IsCorrect == true);

        reviewSession.CorrectAnswers = correctAnswers;
        reviewSession.IsCompleted = true;
        reviewSession.CompletedAt = completedAt;

        await _context.SaveChangesAsync(cancellationToken);

        return new SubmitReviewResult
        {
            ReviewSessionId = reviewSession.ReviewSessionId,
            TotalQuestions = reviewSession.TotalQuestions,
            CorrectAnswers = correctAnswers,
            CompletedAt = completedAt,
            QuestionResults = reviewSession.QuestionResponses
                .Select(response => new SubmittedQuestionResult
                {
                    QuestionResponseId = response.QuestionResponseId,
                    IsCorrect = response.IsCorrect!.Value
                })
                .ToList()
        };
    }

    private static bool GradeMcq(
        QuestionItem questionItem,
        JsonElement studentAnswer)
    {
        if (studentAnswer.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var answer = JsonSerializer.Deserialize<McqStudentAnswer>(
            studentAnswer.GetRawText(),
            QuestionDataJsonOptions);

        if (answer is null)
        {
            return false;
        }

        var questionData = DeserializeQuestionData<McqQuestionData>(questionItem);
        var selectedChoiceIds = answer.SelectedChoiceIds;

        if (questionData.SelectionMode == McqSelectionMode.Single &&
            selectedChoiceIds.Count != 1)
        {
            return false;
        }

        if (selectedChoiceIds.Distinct().Count() != selectedChoiceIds.Count)
        {
            return false;
        }

        return selectedChoiceIds
            .ToHashSet()
            .SetEquals(questionData.CorrectChoiceIds);
    }

    private sealed class McqStudentAnswer
    {
        public List<string> SelectedChoiceIds { get; set; } = [];
    }

    private static bool GradeFillBlank(
        QuestionItem questionItem,
        JsonElement studentAnswer)
    {
        if (studentAnswer.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var answer = JsonSerializer.Deserialize<FillBlankStudentAnswer>(
            studentAnswer.GetRawText(),
            QuestionDataJsonOptions);

        if (answer is null)
        {
            return false;
        }

        var questionData = DeserializeQuestionData<FillBlankQuestionData>(
            questionItem);

        if (answer.Answers.Count != questionData.Blanks.Count ||
            answer.Answers.Select(item => item.BlankId).Distinct().Count() !=
            answer.Answers.Count)
        {
            return false;
        }

        var answersByBlankId = answer.Answers
            .ToDictionary(item => item.BlankId);

        foreach (var blank in questionData.Blanks)
        {
            if (!answersByBlankId.TryGetValue(blank.Id, out var submittedBlank) ||
                submittedBlank.Answer is null)
            {
                return false;
            }

            var submittedValue = NormalizeBlankAnswer(
                submittedBlank.Answer,
                blank.TrimWhitespace);

            var isAccepted = blank.AcceptedAnswers.Any(acceptedAnswer =>
                string.Equals(
                    submittedValue,
                    NormalizeBlankAnswer(acceptedAnswer, blank.TrimWhitespace),
                    blank.CaseSensitive
                        ? StringComparison.Ordinal
                        : StringComparison.OrdinalIgnoreCase));

            if (!isAccepted)
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizeBlankAnswer(string value, bool trimWhitespace)
    {
        return trimWhitespace ? value.Trim() : value;
    }

    private sealed class FillBlankStudentAnswer
    {
        public List<SubmittedBlankAnswer> Answers { get; set; } = [];
    }

    private sealed class SubmittedBlankAnswer
    {
        public required string BlankId { get; set; }
        public string? Answer { get; set; }
    }

    private static bool GradeMatching(
        QuestionItem questionItem,
        JsonElement studentAnswer)
    {
        if (studentAnswer.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var answer = JsonSerializer.Deserialize<MatchingStudentAnswer>(
            studentAnswer.GetRawText(),
            QuestionDataJsonOptions);

        if (answer is null)
        {
            return false;
        }

        var questionData = DeserializeQuestionData<MatchingQuestionData>(
            questionItem);

        if (answer.Pairs.Count != questionData.CorrectPairs.Count ||
            answer.Pairs.Any(pair => pair.LeftId is null || pair.RightId is null))
        {
            return false;
        }

        var submittedPairs = answer.Pairs
            .Select(pair => (pair.LeftId!, pair.RightId!))
            .ToList();

        if (submittedPairs.Distinct().Count() != submittedPairs.Count ||
            submittedPairs.Select(pair => pair.Item1).Distinct().Count() !=
            submittedPairs.Count ||
            submittedPairs.Select(pair => pair.Item2).Distinct().Count() !=
            submittedPairs.Count)
        {
            return false;
        }

        var correctPairs = questionData.CorrectPairs
            .Select(pair => (pair.LeftId, pair.RightId))
            .ToHashSet();

        return submittedPairs
            .ToHashSet()
            .SetEquals(correctPairs);
    }

    private sealed class MatchingStudentAnswer
    {
        public List<SubmittedMatchingPair> Pairs { get; set; } = [];
    }

    private sealed class SubmittedMatchingPair
    {
        public string? LeftId { get; set; }
        public string? RightId { get; set; }
    }

    private static QuestionItemToFrontend MapQuestionItemToFrontend(
        QuestionItem questionItem)
    {
        return new QuestionItemToFrontend
        {
            QuestionItemId = questionItem.QuestionItemId,
            Type = questionItem.Type,
            QuestionData = questionItem.Type switch
            {
                QuestionItemType.Mcq => MapMcqQuestionData(
                    DeserializeQuestionData<McqQuestionData>(questionItem)),

                QuestionItemType.FillBlank => MapFillBlankQuestionData(
                    DeserializeQuestionData<FillBlankQuestionData>(questionItem)),

                QuestionItemType.Matching => MapMatchingQuestionData(
                    DeserializeQuestionData<MatchingQuestionData>(questionItem)),

                _ => throw new InvalidOperationException(
                    $"Unsupported question type: {questionItem.Type}.")
            }
        };
    }

    private static T DeserializeQuestionData<T>(QuestionItem questionItem)
    {
        return JsonSerializer.Deserialize<T>(
            questionItem.QuestionData,
            QuestionDataJsonOptions)
            ?? throw new InvalidOperationException(
                $"Question data for {questionItem.QuestionItemId} is invalid.");
    }

    private static McqQuestionDataToFrontend MapMcqQuestionData(
        McqQuestionData questionData)
    {
        return new McqQuestionDataToFrontend
        {
            Version = questionData.Version,
            Instruction = questionData.Instruction,
            Question = questionData.Question,
            SelectionMode = questionData.SelectionMode,
            Choices = questionData.Choices
        };
    }

    private static FillBlankQuestionDataToFrontend MapFillBlankQuestionData(
        FillBlankQuestionData questionData)
    {
        return new FillBlankQuestionDataToFrontend
        {
            Version = questionData.Version,
            Instruction = questionData.Instruction,
            Question = questionData.Question,
            Blanks = questionData.Blanks
                .Select(blank => new FillBlankDefinitionToFrontend
                {
                    Id = blank.Id,
                    CaseSensitive = blank.CaseSensitive,
                    TrimWhitespace = blank.TrimWhitespace
                })
                .ToList()
        };
    }

    private static MatchingQuestionDataToFrontend MapMatchingQuestionData(
        MatchingQuestionData questionData)
    {
        return new MatchingQuestionDataToFrontend
        {
            Version = questionData.Version,
            Instruction = questionData.Instruction,
            Question = questionData.Question,
            LeftItems = questionData.LeftItems,
            RightItems = questionData.RightItems
        };
    }
}
