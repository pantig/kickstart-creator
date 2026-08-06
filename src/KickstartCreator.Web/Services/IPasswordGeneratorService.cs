namespace KickstartCreator.Web.Services;

public interface IPasswordGeneratorService
{
    /// <summary>Generates a cryptographically-random password of the given length.</summary>
    string Generate(int length);
}
