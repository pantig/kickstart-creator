using System.ComponentModel.DataAnnotations;

namespace KickstartCreator.Web.Models;

/// <summary>
/// Raw values bound from the generator form. Only basic presence/length checks
/// live here as DataAnnotations (for quick ModelState/client-side feedback) -
/// the authoritative allow-list/format/cross-field validation is centralized in
/// <see cref="Validation.KickstartFormValidator"/>, invoked explicitly in
/// addition to ModelState so there is a single source of truth for the regexes.
/// </summary>
public sealed class KickstartFormModel
{
    [Required]
    public RhelVersionOption RhelVersion { get; set; } = RhelVersionOption.Rhel98;

    [Required, StringLength(253)]
    public string Hostname { get; set; } = string.Empty;

    [Required]
    public DiskSelectionMode DiskSelectionMode { get; set; } = DiskSelectionMode.Interactive;

    /// <summary>Only required/used when <see cref="DiskSelectionMode"/> is Manual.</summary>
    [StringLength(512)]
    public string? DiskById { get; set; }

    [Required]
    public NetworkMode NetworkMode { get; set; } = NetworkMode.Dhcp;

    [StringLength(64)]
    public string? StaticIp { get; set; }

    [StringLength(64)]
    public string? StaticNetmask { get; set; }

    [StringLength(64)]
    public string? StaticGateway { get; set; }

    [StringLength(64)]
    public string? StaticDns { get; set; }

    /// <summary>One CIDR per line, e.g. "10.41.202.11/32".</summary>
    [Required, StringLength(4096)]
    public string SshAllowedCidrsRaw { get; set; } = string.Empty;

    [Required, Range(1, 65535)]
    public int SshPort { get; set; } = 9022;

    [Required, StringLength(32)]
    public string AdminUsername { get; set; } = string.Empty;

    [Required, Range(1000, 60000)]
    public int AdminUid { get; set; } = 9001;

    /// <summary>Comma-separated additional groups beyond the mandatory wheel,sshusers.</summary>
    [StringLength(512)]
    public string AdminExtraGroupsRaw { get; set; } = string.Empty;

    [Required]
    public PasswordSourceMode AdminPasswordMode { get; set; } = PasswordSourceMode.Generate;

    [StringLength(256)]
    public string? AdminPassword { get; set; }

    [Required]
    public PasswordSourceMode RootPasswordMode { get; set; } = PasswordSourceMode.Generate;

    [StringLength(256)]
    public string? RootPassword { get; set; }

    [Required]
    public PasswordSourceMode GrubPasswordMode { get; set; } = PasswordSourceMode.Generate;

    [StringLength(256)]
    public string? GrubPassword { get; set; }

    [Required, StringLength(64)]
    public string Timezone { get; set; } = "Europe/Warsaw";

    /// <summary>One NTP server per line.</summary>
    [Required, StringLength(1024)]
    public string NtpServersRaw { get; set; } = "tempus1.gum.gov.pl\ntempus2.gum.gov.pl";
}
