namespace KickstartCreator.Web.Models;

/// <summary>
/// In-memory record for one generation result: where its files live on disk,
/// and - only until the first download - any auto-generated plaintext
/// passwords. The plaintext secrets are intentionally never written to disk or
/// logged; see the "show once, purge after download starts" flow in
/// <see cref="Services.IKickstartGenerationService"/> / ArtifactEndpoints.
/// </summary>
public sealed class GeneratedArtifactSet
{
    private readonly object _secretsLock = new();
    private Dictionary<string, string>? _plaintextSecrets;

    public required Guid Id { get; init; }

    public required DateTimeOffset CreatedUtc { get; init; }

    public required string DirectoryPath { get; init; }

    public required string Hostname { get; init; }

    public required string KickstartFileName { get; init; }

    public required string IsoFileName { get; init; }

    public required string ImgFileName { get; init; }

    public void SetPlaintextSecrets(IReadOnlyDictionary<string, string> secrets)
    {
        lock (_secretsLock)
        {
            _plaintextSecrets = secrets.Count == 0 ? null : new Dictionary<string, string>(secrets);
        }
    }

    /// <summary>Snapshot of still-available plaintext secrets, without clearing them.</summary>
    public IReadOnlyDictionary<string, string> PeekPlaintextSecrets()
    {
        lock (_secretsLock)
        {
            return _plaintextSecrets is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(_plaintextSecrets);
        }
    }

    /// <summary>
    /// Permanently clears the in-memory plaintext secrets. Called by the artifact
    /// download endpoint the first time any file for this id is requested.
    /// </summary>
    public void ClearPlaintextSecrets()
    {
        lock (_secretsLock)
        {
            _plaintextSecrets = null;
        }
    }

    public bool HasPlaintextSecrets
    {
        get
        {
            lock (_secretsLock)
            {
                return _plaintextSecrets is { Count: > 0 };
            }
        }
    }
}
