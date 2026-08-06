using KickstartCreator.Web.Services;
using Xunit;

namespace KickstartCreator.Tests;

public class StrongPasswordGeneratorServiceTests
{
    private static readonly HashSet<char> AllowedChars =
        "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*".ToHashSet();

    private static readonly HashSet<char> ExcludedAmbiguousChars = ['I', 'l', 'O', '0', '1'];

    [Fact]
    public void Generate_ProducesExactlyTheRequestedLength()
    {
        var generator = new StrongPasswordGeneratorService();

        var password = generator.Generate(16);

        Assert.Equal(16, password.Length);
    }

    [Fact]
    public void Generate_NeverContainsAmbiguousOrDisallowedCharacters()
    {
        var generator = new StrongPasswordGeneratorService();

        for (var i = 0; i < 1000; i++)
        {
            var password = generator.Generate(16);
            foreach (var c in password)
            {
                Assert.Contains(c, AllowedChars);
                Assert.DoesNotContain(c, ExcludedAmbiguousChars);
            }
        }
    }

    [Fact]
    public void Generate_AlwaysIncludesAllFourCharacterCategories()
    {
        var generator = new StrongPasswordGeneratorService();

        for (var i = 0; i < 1000; i++)
        {
            var password = generator.Generate(16);

            Assert.Contains(password, char.IsUpper);
            Assert.Contains(password, char.IsLower);
            Assert.Contains(password, char.IsDigit);
            Assert.Contains(password, c => "!@#$%^&*".Contains(c));
        }
    }

    [Fact]
    public void Generate_DoesNotRepeatAcrossCalls()
    {
        var generator = new StrongPasswordGeneratorService();

        var passwords = Enumerable.Range(0, 200).Select(_ => generator.Generate(16)).ToHashSet();

        Assert.Equal(200, passwords.Count);
    }

    [Fact]
    public void Generate_ThrowsForTooShortLength()
    {
        var generator = new StrongPasswordGeneratorService();

        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(2));
    }
}
