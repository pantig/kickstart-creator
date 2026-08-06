using System.Collections.Concurrent;
using KickstartCreator.Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KickstartCreator.Web.Services;

/// <summary>
/// Persists generated files under RootPath/{guid}/ on a Docker named volume, and
/// keeps an in-memory index (no database - this is a single-instance internal
/// tool). <see cref="DeleteExpired"/> sweeps by directory mtime so it still
/// cleans up orphaned directories left behind by a container restart, even
/// though the in-memory index itself is empty after one.
/// </summary>
public sealed class FileSystemArtifactStore : IArtifactStore
{
    private const string KickstartFileName = "ks.cfg";
    private const string IsoFileName = "OEMDRV.iso";
    private const string ImgFileName = "OEMDRV.img";

    private readonly ConcurrentDictionary<Guid, GeneratedArtifactSet> _index = new();
    private readonly ILogger<FileSystemArtifactStore> _logger;

    public FileSystemArtifactStore(IOptions<ArtifactStorageOptions> options, ILogger<FileSystemArtifactStore> logger)
    {
        RootPath = options.Value.RootPath;
        Directory.CreateDirectory(RootPath);
        _logger = logger;
    }

    public string RootPath { get; }

    public ArtifactWorkspace CreateWorkspace()
    {
        var id = Guid.NewGuid();
        var directoryPath = Path.Combine(RootPath, id.ToString("D"));
        var stagingDirectoryPath = Path.Combine(directoryPath, "staging");

        Directory.CreateDirectory(directoryPath);
        Directory.CreateDirectory(stagingDirectoryPath);

        return new ArtifactWorkspace(
            Id: id,
            DirectoryPath: directoryPath,
            StagingDirectoryPath: stagingDirectoryPath,
            StagingKickstartPath: Path.Combine(stagingDirectoryPath, KickstartFileName),
            FinalKickstartPath: Path.Combine(directoryPath, KickstartFileName),
            IsoPath: Path.Combine(directoryPath, IsoFileName),
            ImgPath: Path.Combine(directoryPath, ImgFileName));
    }

    public void Register(GeneratedArtifactSet artifactSet) => _index[artifactSet.Id] = artifactSet;

    public bool TryGet(Guid id, out GeneratedArtifactSet artifactSet) => _index.TryGetValue(id, out artifactSet!);

    public void Remove(Guid id) => _index.TryRemove(id, out _);

    public void DeleteExpired(TimeSpan maxAge)
    {
        if (!Directory.Exists(RootPath))
        {
            return;
        }

        var cutoffUtc = DateTime.UtcNow - maxAge;

        foreach (var dir in Directory.EnumerateDirectories(RootPath))
        {
            try
            {
                var lastWriteUtc = Directory.GetLastWriteTimeUtc(dir);
                if (lastWriteUtc >= cutoffUtc)
                {
                    continue;
                }

                Directory.Delete(dir, recursive: true);

                if (Guid.TryParse(Path.GetFileName(dir), out var id))
                {
                    Remove(id);
                }

                _logger.LogInformation("Removed expired artifact directory {Directory}", dir);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to evaluate/delete artifact directory {Directory}", dir);
            }
        }
    }
}
