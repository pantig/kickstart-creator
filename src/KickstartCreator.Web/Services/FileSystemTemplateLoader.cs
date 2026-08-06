using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace KickstartCreator.Web.Services;

/// <summary>Resolves Scriban `include "name.sbn"` calls against the Templates directory.</summary>
internal sealed class FileSystemTemplateLoader(string templatesDirectory) : ITemplateLoader
{
    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
        => Path.Combine(templatesDirectory, templateName);

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
        => File.ReadAllText(templatePath);

    public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
        => new(File.ReadAllText(templatePath));
}
