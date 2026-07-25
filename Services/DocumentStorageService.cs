namespace MainBackend.Services;

public sealed class DocumentStorageService
{
    private readonly string _uploadRoot;

    public DocumentStorageService(
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        var configuredRoot = configuration["Storage:UploadRoot"] ?? "storage/uploads";
        _uploadRoot = Path.GetFullPath(
            Path.Combine(environment.ContentRootPath, configuredRoot));
    }

    public string CreateStorageKey(int userId, int documentId) =>
        $"{userId}/{documentId}/original.pdf";

    public string GetFullPath(string storageKey)
    {
        var rootWithSeparator = _uploadRoot + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(
            Path.Combine(_uploadRoot, storageKey.Replace('/', Path.DirectorySeparatorChar)));

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Document storage key resolves outside the upload directory.");
        }

        return fullPath;
    }

    public void EnsureDirectoryFor(string storageKey)
    {
        var directory = Path.GetDirectoryName(GetFullPath(storageKey));
        if (directory is null)
        {
            throw new InvalidOperationException("Could not resolve the document storage directory.");
        }

        Directory.CreateDirectory(directory);
    }
}
