namespace KickstartCreator.Web.Services;

/// <summary>
/// On-disk layout for one generation run. StagingKickstartPath lives alone in
/// StagingDirectoryPath (nothing else in that folder) because
/// <see cref="IRemovableMediaBuilder.BuildIsoAsync"/> includes the entire
/// parent directory's contents as the OEMDRV media's root filesystem.
/// FinalKickstartPath is a separate copy meant for direct download/review.
/// </summary>
public sealed record ArtifactWorkspace(
    Guid Id,
    string DirectoryPath,
    string StagingDirectoryPath,
    string StagingKickstartPath,
    string FinalKickstartPath,
    string IsoPath,
    string ImgPath);
