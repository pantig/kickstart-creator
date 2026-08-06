using KickstartCreator.Web.Models;

namespace KickstartCreator.Web.Services;

public interface IArtifactStore
{
    string RootPath { get; }

    ArtifactWorkspace CreateWorkspace();

    void Register(GeneratedArtifactSet artifactSet);

    bool TryGet(Guid id, out GeneratedArtifactSet artifactSet);

    void Remove(Guid id);

    /// <summary>Deletes any artifact directory (indexed or orphaned after a restart) older than <paramref name="maxAge"/>.</summary>
    void DeleteExpired(TimeSpan maxAge);
}
