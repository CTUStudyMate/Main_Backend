using System.Text.Json;
using System.Net.Http.Json;
using MainBackend.Data;
using MainBackend.Configurations;
using MainBackend.Models;
using MainBackend.Models.BackgroundWorker;
using MainBackend.Models.DTOs.VerifiableQa;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MainBackend.Services.BackgroundWorker;

public class GenerateCuratedQaJobHandler : IBackgroundJobHandler
{
    private readonly HttpClient _httpClient;
    private readonly RagEngineOptions _ragOptions;

    public BackgroundJobType JobType => BackgroundJobType.GenerateCuratedQa;

    public GenerateCuratedQaJobHandler(
        HttpClient httpClient,
        IOptions<RagEngineOptions> ragEngineOptions)
    {
        _httpClient = httpClient;
        _ragOptions = ragEngineOptions.Value;
    }

    public async Task HandleAsync(
        BackgroundJob job,
        CancellationToken cancellationToken,
        AppDbContext db)
    {
        var verifiableQa = await db.VerifiableQas
                                    .AsNoTracking()
                                    .Include(qa => qa.Courses)
                                    .FirstOrDefaultAsync(
                                        qa => qa.VerifiableQaId == job.VerifiableQaId,
                                        cancellationToken);

        if (verifiableQa is null)
        {
            throw new InvalidOperationException(
                $"Cannot generate curated QA because VerifiableQa {job.VerifiableQaId} was not found.");
        }

        if (verifiableQa.Status != VerifiableQaStatus.Approved)
        {
            throw new InvalidOperationException(
                $"Cannot generate curated QA because VerifiableQa {verifiableQa.VerifiableQaId} is not approved.");
        }

        if (string.IsNullOrWhiteSpace(verifiableQa.ApprovedAnswer))
        {
            throw new InvalidOperationException(
                $"Cannot generate curated QA because VerifiableQa {verifiableQa.VerifiableQaId} has no approved answer.");
        }

        var verifiableQaToRagEngine = new VerifiableQaToRagEngine
        {
            VerifiableQaId = verifiableQa.VerifiableQaId,
            OriginalQuestion = verifiableQa.OriginalQuestion,
            RewrittenQuestion = verifiableQa.RewrittenQuestion,
            GeneratedAnswer = verifiableQa.GeneratedAnswer,
            ApprovedAnswer = DeserializeApprovedAnswer(verifiableQa.ApprovedAnswer),
            CreatedAt = verifiableQa.CreatedAt,
            UpdatedAt = verifiableQa.UpdatedAt,
            Courses = verifiableQa.Courses
                .Select(course => new VerifiableQaCourseToRagEngine
                {
                    CourseId = course.CourseId,
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName
                })
                .ToList()
        };

        var endpoint = $"{_ragOptions.BaseUrl.TrimEnd('/')}/generate-curated-qas";
        using var response = await _httpClient.PostAsJsonAsync(
            endpoint,
            verifiableQaToRagEngine,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Rag Engine failed to generate curated QAs. " +
                $"Status: {(int)response.StatusCode} {response.ReasonPhrase}. " +
                $"Response: {errorBody}");
        }

        var curatedQaBatch = await response.Content
            .ReadFromJsonAsync<CuratedQaBatchResponse>(cancellationToken);

        if (curatedQaBatch is null || curatedQaBatch.CuratedQas.Count == 0)
        {
            throw new InvalidOperationException(
                "Rag Engine returned no curated QAs.");
        }

        // main flow: call rag engine to get curated qas, 
        //            insert qas to db, 
        //            complete current job, log
        //            create jobs generating excercises for each qa and log for each job

        var timeline = new BackgroundJobTimestampSequence();
        var curatedQas = new List<CuratedQa>();

        foreach (var qa in curatedQaBatch.CuratedQas)
        {
            var curatedQa = new CuratedQa
            {
                CuratedQuestion = qa.Question,
                CuratedAnswer = qa.Answer,
                CreatedAt = timeline.Next(),
                VerifiableQaId = verifiableQa.VerifiableQaId
            };

            curatedQas.Add(curatedQa);
            db.CuratedQas.Add(curatedQa);
        }

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
            Message = $"Background job {job.JobId} completed: {job.Type}. " +
                      $"Created {curatedQas.Count} curated QAs for VerifiableQa {verifiableQa.VerifiableQaId}.",
            CreatedAt = timeline.Next()
        });

        await db.SaveChangesAsync(cancellationToken);

        foreach (var curatedQa in curatedQas)
        {
            var exerciseJobCreatedAt = timeline.Next();

            var exerciseJob = new BackgroundJob
            {
                JobId = Guid.NewGuid(),
                Type = BackgroundJobType.GenerateExercises,
                Status = BackgroundJobStatus.Pending,
                SourceEntityType = BackgroundJobSourceEntityType.CuratedQa,
                CuratedQaId = curatedQa.CuratedQaId,
                AttemptCount = 0,
                MaxAttempts = 5,
                CreatedAt = exerciseJobCreatedAt,
                UpdatedAt = exerciseJobCreatedAt
            };

            db.BackgroundJobs.Add(exerciseJob);
            db.BackgroundJobLogs.Add(new BackgroundJobLog
            {
                BackgroundJobLogId = Guid.NewGuid(),
                JobId = exerciseJob.JobId,
                Level = BackgroundJobLogLevel.Information,
                EventType = BackgroundJobLogEventType.JobCreated,
                AttemptNumber = exerciseJob.AttemptCount,
                Message = $"Generate exercises job created for CuratedQa {curatedQa.CuratedQaId}.",
                CreatedAt = timeline.Next()
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static VerifiedAnswerDto DeserializeApprovedAnswer(string approvedAnswer)
    {
        using var document = JsonDocument.Parse(approvedAnswer);
        var root = document.RootElement;

        var citations = new Dictionary<string, JsonElement>();
        foreach (var citation in root.GetProperty("citations").EnumerateObject())
        {
            var citationElement = citation.Value;
            var citationType = citationElement.GetProperty("type").GetString();

            object citationDto = citationType switch
            {
                "text" => new TextVerifiedCitationDto
                {
                    MarkNumber = citationElement.GetProperty("markNumber").GetInt32(),
                    DocId = citationElement.GetProperty("docId").GetString()!,
                    Evidences = citationElement
                        .GetProperty("evidences")
                        .EnumerateArray()
                        .Select(evidence => evidence.GetString()!)
                        .ToList()
                },
                "image" => new ImageVerifiedCitationDto
                {
                    MarkNumber = citationElement.GetProperty("markNumber").GetInt32(),
                    DocId = citationElement.GetProperty("docId").GetString()!,
                    ImageId = citationElement.GetProperty("imageId").GetString()!
                },
                _ => throw new JsonException(
                    $"Unsupported verified citation type: {citationType}")
            };

            citations[citation.Name] = JsonSerializer
                .SerializeToElement(citationDto);
        }

        return new VerifiedAnswerDto
        {
            EditedAnswer = root.GetProperty("editedAnswer").GetString()!,
            Citations = citations,
            CitationMap = root.GetProperty("citationMap")
                .EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => property.Value.GetInt32())
        };
    }
}
