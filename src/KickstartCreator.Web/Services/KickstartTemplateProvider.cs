using KickstartCreator.Web.Models;
using Microsoft.AspNetCore.Hosting;

namespace KickstartCreator.Web.Services;

/// <summary>
/// Maps a target RHEL version to a template file. Every version maps to the
/// same rhel9.ks.sbn today - no confirmed version-specific differences yet -
/// but adding a differentiated variant later is a one-line map change plus a
/// new .sbn file, no other code changes required.
/// </summary>
public sealed class KickstartTemplateProvider : IKickstartTemplateProvider
{
    private static readonly Dictionary<RhelVersionOption, string> TemplateFilesByVersion = new()
    {
        [RhelVersionOption.Rhel94] = "rhel9.ks.sbn",
        [RhelVersionOption.Rhel96] = "rhel9.ks.sbn",
        [RhelVersionOption.Rhel98] = "rhel9.ks.sbn",
    };

    public KickstartTemplateProvider(IWebHostEnvironment environment)
    {
        TemplatesDirectory = Path.Combine(environment.ContentRootPath, "Templates");
    }

    public string TemplatesDirectory { get; }

    public string GetTemplatePath(RhelVersionOption version)
    {
        if (!TemplateFilesByVersion.TryGetValue(version, out var fileName))
        {
            throw new ArgumentOutOfRangeException(nameof(version), version, "No template mapped for this RHEL version.");
        }

        return Path.Combine(TemplatesDirectory, fileName);
    }
}
