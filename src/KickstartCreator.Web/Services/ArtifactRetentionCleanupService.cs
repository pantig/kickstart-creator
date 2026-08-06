using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KickstartCreator.Web.Services;

/// <summary>Periodically deletes generated artifact directories older than the configured retention window.</summary>
public sealed class ArtifactRetentionCleanupService(
    IArtifactStore artifactStore,
    IOptions<ArtifactRetentionOptions> options,
    ILogger<ArtifactRetentionCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.SweepIntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        SweepOnce();

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                SweepOnce();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // normal shutdown
        }
    }

    private void SweepOnce()
    {
        try
        {
            artifactStore.DeleteExpired(TimeSpan.FromHours(Math.Max(1, options.Value.Hours)));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Artifact retention sweep failed");
        }
    }
}
