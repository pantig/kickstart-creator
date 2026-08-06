namespace KickstartCreator.Web.Models;

/// <summary>
/// Whether a secret (root/user/GRUB password) is typed in by the operator or
/// generated server-side. Generated secrets are shown once on the result page
/// and never persisted in plaintext - see <see cref="GeneratedArtifactSet"/>.
/// </summary>
public enum PasswordSourceMode
{
    Manual,
    Generate,
}
