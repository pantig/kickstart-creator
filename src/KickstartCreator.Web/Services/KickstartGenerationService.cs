using KickstartCreator.Web.Models;
using KickstartCreator.Web.Validation;
using Microsoft.Extensions.Options;

namespace KickstartCreator.Web.Services;

/// <summary>
/// Orchestrates one full generation: validate -&gt; resolve/generate/hash secrets
/// -&gt; render template -&gt; build OEMDRV media -&gt; persist. Runs synchronously
/// within a single request - every step here is sub-second (small files,
/// fast CLI tools), so no background job/queue is warranted.
/// </summary>
public sealed class KickstartGenerationService(
    KickstartFormValidator formValidator,
    IPasswordGeneratorService passwordGenerator,
    IPasswordHashingService passwordHashingService,
    IGrubPasswordHashingService grubPasswordHashingService,
    IKickstartTemplateRenderer templateRenderer,
    IRemovableMediaBuilder mediaBuilder,
    IArtifactStore artifactStore,
    IOptions<PasswordPolicyOptions> passwordPolicyOptions) : IKickstartGenerationService
{
    public async Task<KickstartGenerationOutcome> GenerateAsync(KickstartFormModel form, CancellationToken cancellationToken = default)
    {
        var validation = formValidator.Validate(form);
        if (!validation.IsValid)
        {
            return new KickstartGenerationOutcome(false, validation.Errors, null);
        }

        var parsed = validation.Parsed!;
        var generatedLength = passwordPolicyOptions.Value.GeneratedLength;

        var admin = await ResolvePasswordAsync(form.AdminPasswordMode, form.AdminPassword, generatedLength, cancellationToken);
        var root = await ResolvePasswordAsync(form.RootPasswordMode, form.RootPassword, generatedLength, cancellationToken);
        var grub = ResolveGrubPassword(form.GrubPasswordMode, form.GrubPassword, generatedLength);

        var adminGroupsCsv = string.Join(',', ValidationPatterns.MandatoryAdminGroups.Concat(parsed.AdminExtraGroups));

        var templateModel = new KickstartTemplateModel
        {
            RhelVersionLabel = form.RhelVersion.GetLabel(),
            GeneratedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            DiskSelectionMode = form.DiskSelectionMode == DiskSelectionMode.Manual ? "manual" : "interactive",
            DiskById = form.DiskSelectionMode == DiskSelectionMode.Manual ? form.DiskById : null,
            Hostname = form.Hostname,
            NetworkMode = form.NetworkMode == NetworkMode.Dhcp ? "dhcp" : "static",
            StaticIp = parsed.NormalizedStaticIp,
            StaticNetmask = parsed.NormalizedStaticNetmask,
            StaticGateway = parsed.NormalizedStaticGateway,
            StaticDns = parsed.NormalizedStaticDns,
            SshAllowedCidrs = parsed.SshAllowedCidrs,
            SshPort = form.SshPort,
            AdminUsername = form.AdminUsername,
            AdminUid = form.AdminUid,
            AdminGroupsCsv = adminGroupsCsv,
            AdminPasswordHash = admin.Hash,
            RootPasswordHash = root.Hash,
            GrubPasswordHash = grub.Hash,
            Timezone = form.Timezone,
            NtpServers = parsed.NtpServers,
        };

        var renderedKickstart = templateRenderer.Render(form.RhelVersion, templateModel);

        var workspace = artifactStore.CreateWorkspace();
        await File.WriteAllTextAsync(workspace.StagingKickstartPath, renderedKickstart, cancellationToken);
        await File.WriteAllTextAsync(workspace.FinalKickstartPath, renderedKickstart, cancellationToken);

        await mediaBuilder.BuildIsoAsync(workspace.StagingKickstartPath, workspace.IsoPath, cancellationToken);
        await mediaBuilder.BuildFatImageAsync(workspace.StagingKickstartPath, workspace.ImgPath, cancellationToken);

        var artifactSet = new GeneratedArtifactSet
        {
            Id = workspace.Id,
            CreatedUtc = DateTimeOffset.UtcNow,
            DirectoryPath = workspace.DirectoryPath,
            Hostname = form.Hostname,
            KickstartFileName = Path.GetFileName(workspace.FinalKickstartPath),
            IsoFileName = Path.GetFileName(workspace.IsoPath),
            ImgFileName = Path.GetFileName(workspace.ImgPath),
        };

        var plaintextSecrets = new Dictionary<string, string>();
        if (admin.Plaintext is not null)
        {
            plaintextSecrets["admin"] = admin.Plaintext;
        }

        if (root.Plaintext is not null)
        {
            plaintextSecrets["root"] = root.Plaintext;
        }

        if (grub.Plaintext is not null)
        {
            plaintextSecrets["grub"] = grub.Plaintext;
        }

        artifactSet.SetPlaintextSecrets(plaintextSecrets);
        artifactStore.Register(artifactSet);

        return new KickstartGenerationOutcome(true, [], workspace.Id);
    }

    private async Task<(string? Plaintext, string Hash)> ResolvePasswordAsync(
        PasswordSourceMode mode, string? manualValue, int generatedLength, CancellationToken cancellationToken)
    {
        if (mode == PasswordSourceMode.Generate)
        {
            var generated = passwordGenerator.Generate(generatedLength);
            var hash = await passwordHashingService.HashAsync(generated, cancellationToken);
            return (generated, hash);
        }

        // KickstartFormValidator already rejected an empty manualValue for Manual mode.
        var hashForManual = await passwordHashingService.HashAsync(manualValue!, cancellationToken);
        return (null, hashForManual);
    }

    private (string? Plaintext, string Hash) ResolveGrubPassword(PasswordSourceMode mode, string? manualValue, int generatedLength)
    {
        if (mode == PasswordSourceMode.Generate)
        {
            var generated = passwordGenerator.Generate(generatedLength);
            return (generated, grubPasswordHashingService.Hash(generated));
        }

        return (null, grubPasswordHashingService.Hash(manualValue!));
    }
}
