using MainBackend.Models.QuestionItems;

namespace MainBackend.Models;

public class QuestionItemToFrontend
{
    public Guid QuestionItemId { get; set; }
    public QuestionItemType Type { get; set; }
    public required object QuestionData { get; set; }
}

public class QuestionResponseToFrontend
{
    public Guid QuestionResponseId { get; set; }
    public required string StudentAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public required QuestionItemToFrontend QuestionItem { get; set; }
}

public class McqQuestionDataToFrontend
{
    public int Version { get; set; }
    public required string Instruction { get; set; }
    public required string Question { get; set; }
    public McqSelectionMode SelectionMode { get; set; }
    public List<McqChoice> Choices { get; set; } = [];
}

public class FillBlankDefinitionToFrontend
{
    public required string Id { get; set; }
    public bool CaseSensitive { get; set; }
    public bool TrimWhitespace { get; set; }
}

public class FillBlankQuestionDataToFrontend
{
    public int Version { get; set; }
    public required string Instruction { get; set; }
    public required string Question { get; set; }
    public List<FillBlankDefinitionToFrontend> Blanks { get; set; } = [];
}

public class MatchingQuestionDataToFrontend
{
    public int Version { get; set; }
    public required string Instruction { get; set; }
    public required string Question { get; set; }
    public List<MatchingItem> LeftItems { get; set; } = [];
    public List<MatchingItem> RightItems { get; set; } = [];
}
