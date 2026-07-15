using MainBackend.Models;
namespace MainBackend.Services;

public interface IVerifiableQaService
{
    Task<GetVerifiableQasResponse> GetVerifiableQasFromDB(User lecturer);
}
