namespace KickstartCreator.Web.Services;

/// <summary>Produces GRUB's `grub.pbkdf2.sha512.&lt;iterations&gt;.&lt;salt-hex&gt;.&lt;hash-hex&gt;` hash format.</summary>
public interface IGrubPasswordHashingService
{
    string Hash(string plaintextPassword);
}
