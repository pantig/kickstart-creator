namespace KickstartCreator.Web.Services;

public sealed record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError);

/// <summary>
/// Thin wrapper around external CLI tools (openssl, xorriso, mkfs.vfat, mtools)
/// the container image ships. Every call is timeout-bound; stdin content is
/// never logged, and callers that pass secrets through stdin should set
/// <paramref name="sensitiveOutput"/> (see <see cref="RunAsync"/>) so stdout/stderr
/// are not logged on failure either.
/// </summary>
public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string? standardInput = null,
        bool sensitiveOutput = false,
        CancellationToken cancellationToken = default);
}
