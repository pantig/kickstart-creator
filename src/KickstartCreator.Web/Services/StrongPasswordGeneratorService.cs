using System.Security.Cryptography;

namespace KickstartCreator.Web.Services;

/// <summary>
/// Cryptographically secure password generator (System.Security.Cryptography.RandomNumberGenerator,
/// not System.Random). Character set deliberately excludes commonly-confused glyphs
/// (I, l, O, 0, 1) and restricts special characters to !@#$%^&amp;* for readability
/// when transcribed by hand.
/// </summary>
public sealed class StrongPasswordGeneratorService : IPasswordGeneratorService
{
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // no I, O
    private const string Lowercase = "abcdefghijkmnopqrstuvwxyz"; // no l
    private const string Digits = "23456789"; // no 0, 1
    private const string Special = "!@#$%^&*";

    private static readonly string AllChars = Uppercase + Lowercase + Digits + Special;

    public string Generate(int length)
    {
        if (length < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be at least 4 to cover all character categories.");
        }

        var chars = new char[length];

        // Guarantee at least one character from each required category.
        chars[0] = PickRandomChar(Uppercase);
        chars[1] = PickRandomChar(Lowercase);
        chars[2] = PickRandomChar(Digits);
        chars[3] = PickRandomChar(Special);

        for (var i = 4; i < length; i++)
        {
            chars[i] = PickRandomChar(AllChars);
        }

        Shuffle(chars);

        return new string(chars);
    }

    private static char PickRandomChar(string pool) => pool[RandomNumberGenerator.GetInt32(pool.Length)];

    private static void Shuffle(char[] chars)
    {
        // Fisher-Yates using a CSPRNG-backed, non-modulo-biased index picker.
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
    }
}
