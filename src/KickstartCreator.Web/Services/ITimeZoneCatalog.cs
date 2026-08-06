namespace KickstartCreator.Web.Services;

/// <summary>Cross-checks a candidate IANA timezone id against what the host actually knows.</summary>
public interface ITimeZoneCatalog
{
    bool IsKnown(string timeZoneId);
}
