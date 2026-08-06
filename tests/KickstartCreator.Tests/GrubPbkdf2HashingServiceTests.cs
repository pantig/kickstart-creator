using System.Text.RegularExpressions;
using KickstartCreator.Web.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace KickstartCreator.Tests;

/// <summary>
/// Covers output *format* (matches grub2-mkpasswd-pbkdf2's
/// "grub.pbkdf2.sha512.&lt;iterations&gt;.&lt;SALT-HEX&gt;.&lt;HASH-HEX&gt;" shape,
/// uppercase hex, correct field lengths) and determinism. The underlying
/// PBKDF2-HMAC-SHA512 math is delegated to .NET's Rfc2898DeriveBytes, not
/// reimplemented here.
///
/// NOTE: this does NOT include a true golden vector cross-checked against a
/// real `grub2-mkpasswd-pbkdf2` run - that tool wasn't available in the
/// environment this was written in. Before relying on this in production,
/// generate one reference hash with a fixed salt using the real tool and pin
/// it here as an additional test.
/// </summary>
public partial class GrubPbkdf2HashingServiceTests
{
    private sealed class FixedSaltProvider(byte[] salt) : ISaltProvider
    {
        public byte[] GetSalt(int lengthBytes) => salt;
    }

    [GeneratedRegex(@"^grub\.pbkdf2\.sha512\.(\d+)\.([0-9A-F]+)\.([0-9A-F]+)$")]
    private static partial Regex HashShape();

    private static GrubPbkdf2HashingService CreateService(byte[] salt, int iterations = 10000)
    {
        var options = Options.Create(new GrubPbkdf2Options
        {
            Iterations = iterations,
            SaltLengthBytes = salt.Length,
            DerivedKeyLengthBytes = 64,
        });

        return new GrubPbkdf2HashingService(new FixedSaltProvider(salt), options);
    }

    [Fact]
    public void Hash_ProducesTheExpectedGrubPbkdf2Shape()
    {
        var salt = new byte[64];
        Array.Fill(salt, (byte)0xAB);

        var hash = CreateService(salt).Hash("Correct-Horse-Battery-Staple!1");

        var match = HashShape().Match(hash);
        Assert.True(match.Success, $"Unexpected format: {hash}");
        Assert.Equal("10000", match.Groups[1].Value);
        Assert.Equal(128, match.Groups[2].Value.Length); // 64-byte salt -> 128 hex chars
        Assert.Equal(128, match.Groups[3].Value.Length); // 64-byte derived key -> 128 hex chars
    }

    [Fact]
    public void Hash_IsDeterministicForTheSameSaltAndIterations()
    {
        var salt = new byte[16];
        Array.Fill(salt, (byte)0x42);
        var service = CreateService(salt, iterations: 1000);

        var first = service.Hash("same-password");
        var second = service.Hash("same-password");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Hash_DiffersForDifferentPasswordsWithTheSameSalt()
    {
        var salt = new byte[16];
        Array.Fill(salt, (byte)0x07);
        var service = CreateService(salt, iterations: 1000);

        var first = service.Hash("password-one");
        var second = service.Hash("password-two");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Hash_UsesTheConfiguredIterationCount()
    {
        var salt = new byte[16];
        Array.Fill(salt, (byte)0x11);

        var hash = CreateService(salt, iterations: 25000).Hash("whatever");

        Assert.StartsWith("grub.pbkdf2.sha512.25000.", hash, StringComparison.Ordinal);
    }
}
