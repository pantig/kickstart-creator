namespace KickstartCreator.Web.Services;

/// <summary>
/// Backed by <see cref="TimeZoneInfo.TryFindSystemTimeZoneById"/>, which on the
/// Linux container reads the real /usr/share/zoneinfo database directly (this
/// does not depend on ICU, so it is unaffected by InvariantGlobalization).
/// </summary>
public sealed class SystemTimeZoneCatalog : ITimeZoneCatalog
{
    public bool IsKnown(string timeZoneId)
        => TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
}
