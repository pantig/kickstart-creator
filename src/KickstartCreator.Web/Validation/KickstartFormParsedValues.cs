namespace KickstartCreator.Web.Validation;

/// <summary>
/// Structured values produced by <see cref="KickstartFormValidator"/> once a
/// <see cref="Models.KickstartFormModel"/> passes validation - normalized once
/// here so the generation orchestrator never re-parses (and potentially
/// re-validates differently) the same raw strings.
/// </summary>
public sealed record KickstartFormParsedValues(
    IReadOnlyList<string> SshAllowedCidrs,
    IReadOnlyList<string> AdminExtraGroups,
    IReadOnlyList<string> NtpServers,
    string? NormalizedStaticIp,
    string? NormalizedStaticNetmask,
    string? NormalizedStaticGateway,
    string? NormalizedStaticDns);

public sealed record KickstartFormValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    KickstartFormParsedValues? Parsed)
{
    public static KickstartFormValidationResult Failure(IReadOnlyList<string> errors)
        => new(false, errors, null);

    public static KickstartFormValidationResult Success(KickstartFormParsedValues parsed)
        => new(true, [], parsed);
}
