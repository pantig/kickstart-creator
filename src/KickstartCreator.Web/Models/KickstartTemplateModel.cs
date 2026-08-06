namespace KickstartCreator.Web.Models;

/// <summary>
/// Fully validated and already-hashed values handed to the Scriban renderer.
/// The template performs no validation or escaping of its own - every value
/// here has already passed <see cref="Validation.KickstartFormValidator"/> and,
/// for the three password fields, is a hash (SHA-512 crypt or GRUB PBKDF2), never
/// plaintext. Property names are matched to snake_case template variables by
/// Scriban's default member renamer (e.g. <see cref="DiskById"/> -&gt; disk_by_id).
/// </summary>
public sealed class KickstartTemplateModel
{
    public required string RhelVersionLabel { get; init; }

    public required string GeneratedAtUtc { get; init; }

    /// <summary>Either "interactive" (operator picks the disk at %pre time) or "manual" (baked-in DiskById).</summary>
    public required string DiskSelectionMode { get; init; }

    /// <summary>Only set when <see cref="DiskSelectionMode"/> is "manual".</summary>
    public string? DiskById { get; init; }

    public required string Hostname { get; init; }

    /// <summary>Either "dhcp" or "static".</summary>
    public required string NetworkMode { get; init; }

    public string? StaticIp { get; init; }

    public string? StaticNetmask { get; init; }

    public string? StaticGateway { get; init; }

    public string? StaticDns { get; init; }

    public required IReadOnlyList<string> SshAllowedCidrs { get; init; }

    public required int SshPort { get; init; }

    public required string AdminUsername { get; init; }

    public required int AdminUid { get; init; }

    /// <summary>Comma-separated, always includes "wheel,sshusers" plus any extra groups.</summary>
    public required string AdminGroupsCsv { get; init; }

    /// <summary>SHA-512 crypt hash ($6$...), never plaintext.</summary>
    public required string AdminPasswordHash { get; init; }

    /// <summary>SHA-512 crypt hash ($6$...), never plaintext.</summary>
    public required string RootPasswordHash { get; init; }

    /// <summary>GRUB PBKDF2 hash (grub.pbkdf2.sha512....), never plaintext.</summary>
    public required string GrubPasswordHash { get; init; }

    public required string Timezone { get; init; }

    public required IReadOnlyList<string> NtpServers { get; init; }
}
