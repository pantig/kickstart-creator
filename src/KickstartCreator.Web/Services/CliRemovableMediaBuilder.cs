namespace KickstartCreator.Web.Services;

/// <summary>
/// Shells out to xorriso (ISO9660) and mkfs.vfat/mtools (FAT), the same tooling
/// the wider Linux ecosystem already validates OEMDRV auto-detection against.
/// Requires xorriso, dosfstools and mtools in the container image (see Dockerfile).
/// </summary>
public sealed class CliRemovableMediaBuilder(IProcessRunner processRunner) : IRemovableMediaBuilder
{
    private const string VolumeLabel = "OEMDRV";
    private const int FatImageSizeMiB = 16;

    public async Task BuildIsoAsync(string kickstartFilePath, string outputIsoPath, CancellationToken cancellationToken = default)
    {
        var stagingDir = Path.GetDirectoryName(kickstartFilePath);
        if (string.IsNullOrEmpty(stagingDir))
        {
            throw new ArgumentException("Kickstart file path must have a parent directory.", nameof(kickstartFilePath));
        }

        var result = await processRunner.RunAsync(
            "xorriso",
            [
                "-as", "genisoimage",
                "-o", outputIsoPath,
                "-V", VolumeLabel,
                "-J", "-R",
                stagingDir,
            ],
            cancellationToken: cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"xorriso failed building OEMDRV.iso (exit {result.ExitCode}): {result.StandardError}");
        }
    }

    public async Task BuildFatImageAsync(string kickstartFilePath, string outputImgPath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(outputImgPath))
        {
            File.Delete(outputImgPath);
        }

        // Pre-allocate a small, generously-sized image so mkfs.vfat's FAT-type
        // auto-detection doesn't hit FAT12/16/32 minimum-size edge cases; FAT16
        // is forced explicitly below to sidestep FAT32's larger minimum too.
        await using (var fs = new FileStream(outputImgPath, FileMode.CreateNew, FileAccess.Write))
        {
            fs.SetLength(FatImageSizeMiB * 1024L * 1024L);
        }

        var mkfsResult = await processRunner.RunAsync(
            "mkfs.vfat",
            ["-F", "16", "-n", VolumeLabel, outputImgPath],
            cancellationToken: cancellationToken);

        if (mkfsResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"mkfs.vfat failed building OEMDRV.img (exit {mkfsResult.ExitCode}): {mkfsResult.StandardError}");
        }

        var kickstartFileName = Path.GetFileName(kickstartFilePath);
        var mcopyResult = await processRunner.RunAsync(
            "mcopy",
            ["-i", outputImgPath, kickstartFilePath, $"::{kickstartFileName}"],
            cancellationToken: cancellationToken);

        if (mcopyResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"mcopy failed copying ks.cfg into OEMDRV.img (exit {mcopyResult.ExitCode}): {mcopyResult.StandardError}");
        }
    }
}
