using System.Net;
using System.Net.Sockets;
using KickstartCreator.Web.Models;
using KickstartCreator.Web.Services;

namespace KickstartCreator.Web.Validation;

/// <summary>
/// Authoritative validation for <see cref="KickstartFormModel"/>. Runs in
/// addition to (not instead of) plain DataAnnotations/ModelState checks; this
/// class owns every allow-list/format/cross-field rule and the one-time parsing
/// of list-shaped fields (CIDRs, extra groups, NTP servers).
/// </summary>
public sealed class KickstartFormValidator(ITimeZoneCatalog timeZoneCatalog)
{
    public KickstartFormValidationResult Validate(KickstartFormModel model)
    {
        var errors = new List<string>();

        if (!ValidationPatterns.Hostname().IsMatch(model.Hostname) || model.Hostname.Length > 253)
        {
            errors.Add("Hostname ma niepoprawny format (RFC 1123).");
        }

        if (!ValidationPatterns.DiskById().IsMatch(model.DiskById))
        {
            errors.Add("Identyfikator dysku musi mieć postać /dev/disk/by-id/... (dozwolone znaki: litery, cyfry, '_.:+-').");
        }

        if (!ValidationPatterns.LinuxAccountName().IsMatch(model.AdminUsername))
        {
            errors.Add("Nazwa użytkownika musi pasować do wzorca kont linuksowych (małe litery/cyfry/_/-, zaczyna się od litery lub '_').");
        }
        else if (ValidationPatterns.ReservedAccountNames.Contains(model.AdminUsername))
        {
            errors.Add($"Nazwa użytkownika '{model.AdminUsername}' jest zarezerwowana i nie może być użyta.");
        }

        var extraGroups = ParseAndValidateGroups(model.AdminExtraGroupsRaw, errors);

        string? normalizedIp = null, normalizedNetmask = null, normalizedGateway = null, normalizedDns = null;
        if (model.NetworkMode == NetworkMode.Static)
        {
            normalizedIp = ValidateRequiredIp(model.StaticIp, "adres IP", errors);
            normalizedNetmask = ValidateRequiredIp(model.StaticNetmask, "maska sieci", errors);
            normalizedGateway = ValidateRequiredIp(model.StaticGateway, "bramę", errors);
            normalizedDns = ValidateRequiredIp(model.StaticDns, "serwer DNS", errors);
        }

        var cidrs = ParseAndValidateCidrs(model.SshAllowedCidrsRaw, errors);

        var ntpServers = ParseAndValidateNtpServers(model.NtpServersRaw, errors);

        if (!ValidationPatterns.TimezoneCharset().IsMatch(model.Timezone) || !timeZoneCatalog.IsKnown(model.Timezone))
        {
            errors.Add($"Nieznana strefa czasowa: '{model.Timezone}'.");
        }

        ValidatePasswordMode(model.AdminPasswordMode, model.AdminPassword, "hasła użytkownika", errors);
        ValidatePasswordMode(model.RootPasswordMode, model.RootPassword, "hasła root", errors);
        ValidatePasswordMode(model.GrubPasswordMode, model.GrubPassword, "hasła GRUB", errors);

        if (errors.Count > 0)
        {
            return KickstartFormValidationResult.Failure(errors);
        }

        var parsed = new KickstartFormParsedValues(
            SshAllowedCidrs: cidrs,
            AdminExtraGroups: extraGroups,
            NtpServers: ntpServers,
            NormalizedStaticIp: normalizedIp,
            NormalizedStaticNetmask: normalizedNetmask,
            NormalizedStaticGateway: normalizedGateway,
            NormalizedStaticDns: normalizedDns);

        return KickstartFormValidationResult.Success(parsed);
    }

    private static void ValidatePasswordMode(PasswordSourceMode mode, string? value, string label, List<string> errors)
    {
        if (mode == PasswordSourceMode.Manual && string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"Wybrano ręczne podanie {label}, ale pole jest puste.");
        }
    }

    private static List<string> ParseAndValidateGroups(string raw, List<string> errors)
    {
        var result = new List<string>();
        foreach (var candidate in SplitNonEmpty(raw, ','))
        {
            var group = candidate.Trim();
            if (ValidationPatterns.MandatoryAdminGroups.Contains(group))
            {
                // Already force-included; silently ignore rather than erroring, so
                // re-typing "wheel" isn't a validation failure.
                continue;
            }

            if (!ValidationPatterns.LinuxAccountName().IsMatch(group))
            {
                errors.Add($"Nieprawidłowa nazwa grupy: '{group}'.");
                continue;
            }

            if (!result.Contains(group, StringComparer.Ordinal))
            {
                result.Add(group);
            }
        }

        return result;
    }

    private static string? ValidateRequiredIp(string? value, string label, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || !IPAddress.TryParse(value.Trim(), out var ip))
        {
            errors.Add($"Podaj poprawny {label} (tryb sieci: static).");
            return null;
        }

        // Re-serialize rather than passing the raw user string through, so the
        // template only ever sees a value that has round-tripped through IPAddress.
        return ip.ToString();
    }

    private static List<string> ParseAndValidateCidrs(string raw, List<string> errors)
    {
        var result = new List<string>();
        var lines = SplitNonEmpty(raw, '\n', '\r');

        foreach (var line in lines)
        {
            var candidate = line.Trim();
            var parts = candidate.Split('/', 2);
            if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var ip))
            {
                errors.Add($"Nieprawidłowy CIDR: '{candidate}' (oczekiwano adres/maska, np. 10.0.0.1/32).");
                continue;
            }

            var maxPrefix = ip.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
            if (!int.TryParse(parts[1], out var prefix) || prefix < 0 || prefix > maxPrefix)
            {
                errors.Add($"Nieprawidłowy prefiks w CIDR: '{candidate}'.");
                continue;
            }

            result.Add($"{ip}/{prefix}");
        }

        if (result.Count == 0)
        {
            errors.Add("Podaj co najmniej jeden dozwolony CIDR źródłowy dla SSH.");
        }

        return result;
    }

    private static List<string> ParseAndValidateNtpServers(string raw, List<string> errors)
    {
        var result = new List<string>();
        foreach (var line in SplitNonEmpty(raw, '\n', '\r'))
        {
            var candidate = line.Trim();
            var isValid = ValidationPatterns.Hostname().IsMatch(candidate) || IPAddress.TryParse(candidate, out _);
            if (!isValid)
            {
                errors.Add($"Nieprawidłowy serwer NTP: '{candidate}'.");
                continue;
            }

            result.Add(candidate);
        }

        if (result.Count == 0)
        {
            errors.Add("Podaj co najmniej jeden serwer NTP.");
        }

        return result;
    }

    private static IEnumerable<string> SplitNonEmpty(string raw, params char[] separators)
        => raw.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
