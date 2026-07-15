namespace MainBackend.Models;

public class GetVerifiableQasResponse
{
    public int Total { get; set; }
    public List<PendingQa> RelatedQa { get; set; } = new();
    public List<PendingQa> UndefinedCourseQa { get; set; } = new();
}
