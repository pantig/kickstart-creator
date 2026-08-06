using System.Text.RegularExpressions;

namespace KickstartCreator.Web.Services;

/// <summary>
/// Shells out to `openssl passwd -6 -stdin`. .NET has no trustworthy native
/// crypt(3)-compatible SHA-512 implementation, and this is the same tool RHEL
/// itself ships. The password is written to the process's stdin, never passed
/// as a command-line argument, so it cannot appear in `ps`/argv.
/// </summary>
public sealed partial class OpenSslShaCryptHashingService(IProcessRunner processRunner) : IPasswordHashingService
{
    public async Task<string> HashAsync(string plaintextPassword, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintextPassword);

        var result = await processRunner.RunAsync(
            "openssl",
            ["passwd", "-6", "-stdin"],
            standardInput: plaintextPassword,
            sensitiveOutput: true,
            cancellationToken: cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException("openssl passwd failed while hashing a password.");
        }

        var hash = result.StandardOutput.Trim();
        if (!Sha512CryptShape().IsMatch(hash))
        {
            throw new InvalidOperationException("openssl passwd produced an unexpected hash format.");
        }

        return hash;
    }

    [GeneratedRegex(@"^\$6\$[^\s$]+\$[./A-Za-z0-9]+$")]
    private static partial Regex Sha512CryptShape();
}
