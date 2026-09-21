using Emby.Plugins.Moonfin.Api;
using Xunit;

namespace Emby.Plugins.Moonfin.Tests;

/// <summary>
/// A rom file and the metadata record it should match rarely agree on accents: a no-intro set
/// writes "Pokémon", a hand-renamed file writes "Pokemon". Both name normalisers fold accents
/// out so the two spellings meet, the way the Jellyfin plugin's RdbMatcher and LaunchBoxService
/// always have.
/// </summary>
public class GameNameNormalizationTests
{
    [Theory]
    [InlineData("Pokémon Red", "pokemonred")]
    [InlineData("Pokemon Red", "pokemonred")]
    [InlineData("Ōkami", "okami")]
    [InlineData("Café International", "cafeinternational")]
    public void NormalizeAlphanumericLower_FoldsAccents(string input, string expected)
    {
        Assert.Equal(expected, GamesScanner.NormalizeAlphanumericLower(input));
    }

    [Fact]
    public void NormalizeAlphanumericLower_MatchesAcrossSpellings()
    {
        Assert.Equal(
            GamesScanner.NormalizeAlphanumericLower("Pokémon Red"),
            GamesScanner.NormalizeAlphanumericLower("Pokemon Red"));
    }

    [Theory]
    [InlineData("Super Mario Bros.", "supermariobros")]
    [InlineData("Legend of Zelda, The", "legendofzeldathe")]
    public void NormalizeAlphanumericLower_LeavesUnaccentedNamesAlone(string input, string expected)
    {
        Assert.Equal(expected, GamesScanner.NormalizeAlphanumericLower(input));
    }

    [Theory]
    [InlineData("Pokémon Red (USA)", "pokemonred")]
    [InlineData("Pokemon Red (USA)", "pokemonred")]
    [InlineData("Café International [!]", "cafeinternational")]
    public void LaunchBoxNormalizeName_FoldsAccentsAndStillDropsBracketedText(
        string input,
        string expected)
    {
        Assert.Equal(expected, GameLaunchBoxHelper.NormalizeName(input));
    }

    // NTFS lets a file or folder name hold an unpaired surrogate, and string.Normalize
    // throws on one. GetSystems, GetGames, GetGame and ResolveThumbSource all normalise
    // a folder name outside any try, so a single odd name must not take the scan down.
    // It falls back to the unfolded name, which is what every name did before folding.
    [Theory]
    [InlineData("\ud800")]
    [InlineData("Game\udfff Name")]
    [InlineData("\udc00Zelda")]
    public void NormalizeAlphanumericLower_SurvivesAnUnpairedSurrogate(string input)
    {
        Assert.Null(Record.Exception(() => GamesScanner.NormalizeAlphanumericLower(input)));
    }

    [Fact]
    public void LaunchBoxNormalizeName_SurvivesAnUnpairedSurrogate()
    {
        Assert.Null(Record.Exception(() => GameLaunchBoxHelper.NormalizeName("Game\ud800 (USA)")));
    }

    // Falling back must still yield the usable part of the name, not give up on it.
    [Fact]
    public void NormalizeAlphanumericLower_StillReadsTheNameAroundABadSurrogate()
    {
        Assert.Equal("gamename", GamesScanner.NormalizeAlphanumericLower("Game\udfffName"));
    }

    // The bracket depth counter is what keeps a region tag out of the key. Folding runs first,
    // so a combining mark must not be mistaken for a bracket on the way past.
    [Fact]
    public void LaunchBoxNormalizeName_KeepsNestedBracketsBalanced()
    {
        Assert.Equal("game", GameLaunchBoxHelper.NormalizeName("Game (Europe (En,Fr,De)) [b1]"));
    }
}
