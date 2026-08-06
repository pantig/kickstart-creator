using KickstartCreator.Web.Models;

namespace KickstartCreator.Web.Services;

public sealed record KickstartGenerationOutcome(bool Success, IReadOnlyList<string> Errors, Guid? ArtifactId);

public interface IKickstartGenerationService
{
    /// <summary>
    /// Validates the form, resolves/generates/hashes the three secrets, renders
    /// the kickstart, builds the OEMDRV ISO+IMG, and persists everything. On
    /// success the caller should redirect to the result page for
    /// <see cref="KickstartGenerationOutcome.ArtifactId"/> - that is where any
    /// auto-generated plaintext passwords are shown (once).
    /// </summary>
    Task<KickstartGenerationOutcome> GenerateAsync(KickstartFormModel form, CancellationToken cancellationToken = default);
}
