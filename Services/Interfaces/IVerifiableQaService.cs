using MainBackend.Models;
namespace MainBackend.Services;

public interface IVerifiableQaService
{
    Task<GetVerifiableQasResponse> GetVerifiableQasFromDB(User lecturer);
    Task<PendingQa?> GetVerifiableQaByIdFromDB(int verifiableQaId, User lecturer);
    Task<PendingQa?> ApproveVerifiableQaAsync(
        int verifiableQaId,
        string approvedAnswer,
        User lecturer);
}
