namespace KickstartCreator.Web.Services;

public sealed class ArtifactRetentionOptions
{
    public int Hours { get; set; } = 24;
    public int SweepIntervalMinutes { get; set; } = 15;
}
