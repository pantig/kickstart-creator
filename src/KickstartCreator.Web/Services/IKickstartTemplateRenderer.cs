using KickstartCreator.Web.Models;

namespace KickstartCreator.Web.Services;

public interface IKickstartTemplateRenderer
{
    /// <summary>Renders the final ks.cfg text. Throws if the template has parse errors or unresolved tokens remain.</summary>
    string Render(RhelVersionOption version, KickstartTemplateModel model);
}
