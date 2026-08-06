using KickstartCreator.Web.Models;

namespace KickstartCreator.Web.Services;

public interface IKickstartTemplateProvider
{
    /// <summary>Absolute path to the main .sbn template file for the given RHEL version.</summary>
    string GetTemplatePath(RhelVersionOption version);

    /// <summary>Directory containing all templates (including partials), used to resolve Scriban `include`.</summary>
    string TemplatesDirectory { get; }
}
