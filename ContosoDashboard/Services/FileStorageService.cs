using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public interface IFileStorageService
{
    Task UploadAsync(Stream content, string relativePath, string contentType, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadAsync(string relativePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IHostEnvironment environment, IOptions<DocumentFeatureOptions> options, ILogger<LocalFileStorageService> logger)
    {
        _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.StorageRoot));
        _logger = logger;
        Directory.CreateDirectory(_root);
    }

    public async Task UploadAsync(Stream content, string relativePath, string contentType, CancellationToken cancellationToken = default)
    {
        var path = Resolve(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(target, cancellationToken);
    }

    public Task<Stream?> DownloadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var path = Resolve(relativePath);
        if (!File.Exists(path)) return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var path = Resolve(relativePath);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string Resolve(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new ArgumentException("Storage paths must be non-empty relative paths.", nameof(relativePath));
        var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath));
        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Storage path traversal is not allowed.", nameof(relativePath));
        return fullPath;
    }
}
