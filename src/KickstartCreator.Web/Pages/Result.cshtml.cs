using KickstartCreator.Web.Models;
using KickstartCreator.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace KickstartCreator.Web.Pages;

public sealed class ResultModel(IArtifactStore artifactStore, IOptions<ArtifactRetentionOptions> retentionOptions) : PageModel
{
    public bool Found { get; private set; }

    public GeneratedArtifactSet? ArtifactSet { get; private set; }

    public IReadOnlyDictionary<string, string> PlaintextSecrets { get; private set; } = new Dictionary<string, string>();

    public int RetentionHours => retentionOptions.Value.Hours;

    public IActionResult OnGet(Guid id)
    {
        if (!artifactStore.TryGet(id, out var artifactSet))
        {
            Found = false;
            return Page();
        }

        Found = true;
        ArtifactSet = artifactSet;
        PlaintextSecrets = artifactSet.PeekPlaintextSecrets();
        return Page();
    }
}
