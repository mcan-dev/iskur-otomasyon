using IskurAuto.Core.Interfaces;

namespace IskurAuto.API.Infrastructure;

public class LocalDocumentStore : IDocumentStore
{
    private readonly string _root;

    public LocalDocumentStore(string root)
    {
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken ct = default)
    {
        var storedName = $"{Guid.NewGuid():N}{extension}";
        await using var target = File.Create(Path.Combine(_root, storedName));
        await content.CopyToAsync(target, ct);
        return storedName;
    }

    public Task<Stream?> OpenAsync(string storedName, CancellationToken ct = default)
    {
        // Yalnızca kendi ürettiğimiz düz dosya adları kabul edilir; yol gezintisi (../) engellenir.
        if (storedName != Path.GetFileName(storedName))
            return Task.FromResult<Stream?>(null);

        var path = Path.Combine(_root, storedName);
        Stream? stream = File.Exists(path) ? File.OpenRead(path) : null;
        return Task.FromResult(stream);
    }
}
