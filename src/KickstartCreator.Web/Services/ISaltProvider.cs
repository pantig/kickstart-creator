namespace KickstartCreator.Web.Services;

/// <summary>Abstraction over salt generation, so tests can inject a fixed salt for golden-vector comparisons.</summary>
public interface ISaltProvider
{
    byte[] GetSalt(int lengthBytes);
}
