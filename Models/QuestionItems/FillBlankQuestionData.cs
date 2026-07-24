namespace MainBackend.Models.QuestionItems;

public class BlankDefinition
{
    public required string Id { get; set; }
    public List<string> AcceptedAnswers { get; set; } = [];
    public bool CaseSensitive { get; set; } = false;
    public bool TrimWhitespace { get; set; } = true;
}

public class FillBlankQuestionData
{
    public int Version { get; set; } = 1;
    public required string Instruction { get; set; }
    public required string Question { get; set; }
    public List<BlankDefinition> Blanks { get; set; } = [];
    public required string Explanation { get; set; }
}
