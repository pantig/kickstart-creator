using System.Security.Cryptography;

namespace KickstartCreator.Web.Services;

public sealed class RandomSaltProvider : ISaltProvider
{
    public byte[] GetSalt(int lengthBytes) => RandomNumberGenerator.GetBytes(lengthBytes);
}
