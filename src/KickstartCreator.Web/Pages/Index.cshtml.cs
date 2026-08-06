using KickstartCreator.Web.Models;
using KickstartCreator.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KickstartCreator.Web.Pages;

public sealed class IndexModel(IKickstartGenerationService generationService) : PageModel
{
    [BindProperty]
    public KickstartFormModel Form { get; set; } = new();

    public IReadOnlyList<string> GenerationErrors { get; private set; } = [];

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var outcome = await generationService.GenerateAsync(Form, cancellationToken);
        if (!outcome.Success)
        {
            GenerationErrors = outcome.Errors;
            return Page();
        }

        return RedirectToPage("Result", new { id = outcome.ArtifactId });
    }
}
