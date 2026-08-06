using System.Text.RegularExpressions;

namespace KickstartCreator.Web.Validation;

/// <summary>
/// Centralized allow-list patterns. Fields rendered into the kickstart land in a
/// file that mixes kickstart directives with literal bash (%pre/%post heredocs) -
/// there is no "escape" equivalent for that context, so every free-form field is
/// validated against a strict allow-list here rather than escaped.
/// </summary>
public static partial class ValidationPatterns
{
    // RFC 1123 hostname: dot-separated labels, 1-63 chars each, alnum + hyphen,
    // no leading/trailing hyphen. Overall length (<=253) is checked separately.
    [GeneratedRegex(@"^[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$")]
    public static partial Regex Hostname();

    // useradd/groupadd-compatible Linux account/group name.
    [GeneratedRegex(@"^[a-z_][a-z0-9_-]{0,31}$")]
    public static partial Regex LinuxAccountName();

    // The /dev/disk/by-id/... shape used throughout the kickstart template.
    [GeneratedRegex(@"^/dev/disk/by-id/[A-Za-z0-9_.:+-]+$")]
    public static partial Regex DiskById();

    // Timezone charset allow-list; actual IANA validity is cross-checked
    // separately via ITimeZoneCatalog.
    [GeneratedRegex(@"^[A-Za-z0-9_+/-]+$")]
    public static partial Regex TimezoneCharset();

    /// <summary>Account names that must never be used for the generated admin user.</summary>
    public static readonly HashSet<string> ReservedAccountNames = new(StringComparer.Ordinal)
    {
        "root", "bin", "daemon", "adm", "lp", "sync", "shutdown", "halt", "mail",
        "operator", "games", "ftp", "nobody", "systemd-network", "dbus", "polkitd",
        "sshd", "chrony", "rpc", "sssd", "sshusers", "wheel",
    };

    /// <summary>Groups that are always force-included for the admin user - not user-selectable as "extra".</summary>
    public static readonly string[] MandatoryAdminGroups = ["wheel", "sshusers"];
}
