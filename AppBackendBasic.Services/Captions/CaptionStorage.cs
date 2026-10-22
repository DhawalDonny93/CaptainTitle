using Microsoft.Extensions.Options;

namespace AppBackendBasic.Services.Captions;

public interface ICaptionStorage
{
    Task PutAsync(string path, byte[] data, string contentType, CancellationToken ct = default);
    Task PutFileAsync(string path, string sourceFile, string contentType, CancellationToken ct = default);
    /// <summary>Opens a stored object for reading (seekable) together with its content type.</summary>
    Task<(Stream Stream, string ContentType)> OpenAsync(string path, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
}

/// <summary>Local-disk object storage (port of storage_service.py), with path-traversal protection.</summary>
public class LocalCaptionStorage : ICaptionStorage
{
    private readonly string _root;

    public LocalCaptionStorage(IOptions<CaptionOptions> options)
    {
        _root = Path.GetFullPath(options.Value.StorageRoot);
        Directory.CreateDirectory(_root);
    }

    private string Resolve(string path)
    {
        var full = Path.GetFullPath(Path.Combine(_root, path));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Invalid storage path");
        return full;
    }

    private static string MetaPath(string target) => target + ".meta";

    public async Task PutAsync(string path, byte[] data, string contentType, CancellationToken ct = default)
    {
        var target = Resolve(path);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await File.WriteAllBytesAsync(target, data, ct);
        await File.WriteAllTextAsync(MetaPath(target), string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType, ct);
    }

    public async Task PutFileAsync(string path, string sourceFile, string contentType, CancellationToken ct = default)
    {
        var target = Resolve(path);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(sourceFile, target, overwrite: true);
        await File.WriteAllTextAsync(MetaPath(target), string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType, ct);
    }

    public Task<(Stream Stream, string ContentType)> OpenAsync(string path, CancellationToken ct = default)
    {
        var target = Resolve(path);
        if (!File.Exists(target)) throw new FileNotFoundException(path);
        var meta = MetaPath(target);
        var contentType = File.Exists(meta) ? File.ReadAllText(meta).Trim() : "application/octet-stream";
        Stream stream = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult((stream, contentType));
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var target = Resolve(path);
        if (File.Exists(target)) File.Delete(target);
        if (File.Exists(MetaPath(target))) File.Delete(MetaPath(target));
        return Task.CompletedTask;
    }
}
