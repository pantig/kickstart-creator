using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace KickstartCreator.Web.Services;

/// <summary>
/// Pure-managed reimplementation of `grub2-mkpasswd-pbkdf2`'s output format.
/// GRUB's PBKDF2 hash is standard PBKDF2-HMAC-SHA512 (RFC 2898/8018) with no
/// proprietary framing, so <see cref="Rfc2898DeriveBytes"/> covers it exactly -
/// no need to install grub2-tools (awkward on a Debian-based image) just for
/// this one command. Verified against a real grub2-mkpasswd-pbkdf2 run via the
/// golden test vector in GrubPbkdf2HashingServiceTests.
/// </summary>
public sealed class GrubPbkdf2HashingService(ISaltProvider saltProvider, IOptions<GrubPbkdf2Options> options) : IGrubPasswordHashingService
{
    public string Hash(string plaintextPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintextPassword);

        var opts = options.Value;
        var salt = saltProvider.GetSalt(opts.SaltLengthBytes);

        var derived = Rfc2898DeriveBytes.Pbkdf2(
            password: Encoding.UTF8.GetBytes(plaintextPassword),
            salt: salt,
            iterations: opts.Iterations,
            hashAlgorithm: HashAlgorithmName.SHA512,
            outputLength: opts.DerivedKeyLengthBytes);

        // Convert.ToHexString produces uppercase hex, matching grub2-mkpasswd-pbkdf2's output.
        var saltHex = Convert.ToHexString(salt);
        var hashHex = Convert.ToHexString(derived);

        return $"grub.pbkdf2.sha512.{opts.Iterations}.{saltHex}.{hashHex}";
    }
}
