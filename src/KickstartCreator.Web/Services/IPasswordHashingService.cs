namespace KickstartCreator.Web.Services;

/// <summary>Produces SHA-512 crypt ($6$...) hashes for rootpw / user --password --iscrypted.</summary>
public interface IPasswordHashingService
{
    Task<string> HashAsync(string plaintextPassword, CancellationToken cancellationToken = default);
}
