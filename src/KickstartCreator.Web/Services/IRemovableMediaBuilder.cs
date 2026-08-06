namespace KickstartCreator.Web.Services;

/// <summary>
/// Builds OEMDRV-labeled removable media (ISO9660 + FAT) containing just ks.cfg,
/// for Anaconda's automatic OEMDRV auto-detection. Never touches the stock RHEL
/// installation ISO - the media this produces is meant to be attached as a
/// SECOND disk/CD/USB alongside it.
/// </summary>
public interface IRemovableMediaBuilder
{
    /// <summary>
    /// <paramref name="kickstartFilePath"/> must be the only file in its parent
    /// directory - that whole directory becomes the media's root filesystem.
    /// </summary>
    Task BuildIsoAsync(string kickstartFilePath, string outputIsoPath, CancellationToken cancellationToken = default);

    Task BuildFatImageAsync(string kickstartFilePath, string outputImgPath, CancellationToken cancellationToken = default);
}
