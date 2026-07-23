namespace MainBackend.Models;

// response model to frontend to display the qas in lecturer's UI
public class GetVerifiableQasResponse
{
    public int Total { get; set; }
    public List<PendingQa> RelatedQa { get; set; } = new();
    public List<PendingQa> UndefinedCourseQa { get; set; } = new();
}
