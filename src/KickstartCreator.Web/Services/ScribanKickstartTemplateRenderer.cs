using KickstartCreator.Web.Models;
using Scriban;
using Scriban.Runtime;

namespace KickstartCreator.Web.Services;

/// <summary>
/// Renders KickstartTemplateModel (already validated + hashed - see the model's
/// doc comment) through the .sbn template. Performs no validation/escaping of
/// its own; that responsibility lives entirely upstream in
/// <see cref="Validation.KickstartFormValidator"/> and the hashing services.
/// </summary>
public sealed class ScribanKickstartTemplateRenderer(IKickstartTemplateProvider templateProvider) : IKickstartTemplateRenderer
{
    public string Render(RhelVersionOption version, KickstartTemplateModel model)
    {
        var templatePath = templateProvider.GetTemplatePath(version);
        var templateText = File.ReadAllText(templatePath);

        var template = Template.Parse(templateText, templatePath);
        if (template.HasErrors)
        {
            var messages = string.Join("; ", template.Messages);
            throw new InvalidOperationException($"Failed to parse kickstart template '{templatePath}': {messages}");
        }

        var scriptObject = new ScriptObject();
        // Explicit renamer: KickstartTemplateModel uses PascalCase C# properties
        // (DiskById, SshPort, ...) which must map to the snake_case names used
        // throughout the .sbn templates (disk_by_id, ssh_port, ...).
        scriptObject.Import(model, renamer: StandardMemberRenamer.Default);

        var context = new TemplateContext
        {
            TemplateLoader = new FileSystemTemplateLoader(templateProvider.TemplatesDirectory),
        };
        context.PushGlobal(scriptObject);

        var rendered = template.Render(context);

        if (rendered.Contains("{{", StringComparison.Ordinal) || rendered.Contains("}}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Rendered kickstart still contains unresolved template tokens - refusing to produce a broken ks.cfg.");
        }

        return rendered;
    }
}
