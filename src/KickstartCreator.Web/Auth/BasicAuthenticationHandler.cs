using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KickstartCreator.Web.Auth;

/// <summary>
/// Minimal HTTP Basic auth gate for the internal tool - not a user database,
/// just a trip-wire. Credentials come from env vars (BASIC_AUTH_USER /
/// BASIC_AUTH_PASSWORD) or BasicAuth:User / BasicAuth:Password, compared with
/// a constant-time comparison. Only wired up when BasicAuth:Enabled=true;
/// the recommended default is a reverse proxy doing auth instead.
/// </summary>
public sealed class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "BasicAuth";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));
        }

        var authHeader = authHeaderValues.ToString();
        if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Unsupported Authorization scheme"));
        }

        string decoded;
        try
        {
            var encoded = authHeader["Basic ".Length..].Trim();
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic Authorization header"));
        }

        var separatorIndex = decoded.IndexOf(':');
        if (separatorIndex < 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic Authorization header"));
        }

        var providedUser = decoded[..separatorIndex];
        var providedPassword = decoded[(separatorIndex + 1)..];

        var expectedUser = configuration["BASIC_AUTH_USER"] ?? configuration["BasicAuth:User"] ?? string.Empty;
        var expectedPassword = configuration["BASIC_AUTH_PASSWORD"] ?? configuration["BasicAuth:Password"] ?? string.Empty;

        if (expectedUser.Length == 0 || !FixedTimeEquals(providedUser, expectedUser) || !FixedTimeEquals(providedPassword, expectedPassword))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid credentials"));
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, providedUser)], SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"kickstart-creator\"";
        return base.HandleChallengeAsync(properties);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);

        if (aBytes.Length != bBytes.Length)
        {
            // Compare against a same-length dummy buffer so a length mismatch
            // doesn't short-circuit before any constant-time comparison runs.
            CryptographicOperations.FixedTimeEquals(aBytes, new byte[aBytes.Length]);
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
