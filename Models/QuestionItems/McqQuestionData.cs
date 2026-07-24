namespace MainBackend.Models.QuestionItems;

public enum McqSelectionMode
{
    Single,
    Multiple
}

public class McqChoice
{
    public required string Id { get; set; }
    public required string Text { get; set; }
}

public class McqQuestionData
{
    public int Version { get; set; } = 1;
    public required string Instruction { get; set; }
    public required string Question { get; set; }
    public McqSelectionMode SelectionMode { get; set; }
    public List<McqChoice> Choices { get; set; } = [];
    public List<string> CorrectChoiceIds { get; set; } = [];
    public required string Explanation { get; set; }
}
