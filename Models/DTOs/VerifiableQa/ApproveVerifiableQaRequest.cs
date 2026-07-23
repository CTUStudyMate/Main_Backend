namespace MainBackend.Models;

// recieve from frontend to update the approvedAnswer field in database
public class ApproveVerifiableQaRequest
{
    public required string ApprovedAnswer { get; set; }
}
