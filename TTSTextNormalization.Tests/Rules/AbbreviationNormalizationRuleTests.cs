using TTSTextNormalization.Rules;

namespace TTSTextNormalization.Tests.Rules;

[TestClass]
public class AbbreviationNormalizationRuleTests
{
    private readonly AbbreviationNormalizationRule _rule = new();

    [TestMethod]
    [DataRow("", "", DisplayName = "Empty Input")]
    [DataRow(" ", " ", DisplayName = "Whitespace Input")]
    public void Apply_EmptyOrWhitespaceInput_ReturnsInput(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("lol", " laughing out loud ", DisplayName = "Simple LOL")]
    [DataRow("brb", " be right back ", DisplayName = "Simple BRB")]
    [DataRow("omg!", " oh my god !", DisplayName = "OMG with punctuation - Corrected Expectation")] // Match map
    [DataRow("Hey, ttyl.", "Hey,  talk to you later .", DisplayName = "TTYL in sentence - Corrected Spacing")] // Expect consistent padding
    [DataRow("thx.", " thanks .", DisplayName = "THX with period - Corrected Spacing")] // Expect consistent padding
    [DataRow("ty vm", " thank you  vm", DisplayName = "TY followed by unknown")]
    [DataRow("GG", " good game ", DisplayName = "Simple GG")]
    public void Apply_KnownAbbreviations_ReplacesWithExpansion(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("LOL", " laughing out loud ", DisplayName = "Uppercase LOL")]
    [DataRow("BrB", " be right back ", DisplayName = "Mixed case BRB")]
    [DataRow("iDK", " I don't know ", DisplayName = "Mixed case IDK")]
    public void Apply_KnownAbbreviationsCaseInsensitive_ReplacesWithExpansion(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);

        // Assert
        Assert.AreEqual(expected, result);
    }


    [TestMethod]
    [DataRow("idk lol", " I don't know   laughing out loud ", DisplayName = "Multiple abbreviations")]
    [DataRow("gg afk", " good game   away from keyboard ", DisplayName = "Multiple adjacent abbreviations")]
    public void Apply_MultipleAbbreviations_ReplacesAll(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);

        // Assert
        Assert.AreEqual(expected, result);
    }


    [TestMethod]
    [DataRow("lollipop", "lollipop", DisplayName = "Substring 'lol'")]
    [DataRow("scrolling", "scrolling", DisplayName = "Substring 'lol' (reverse)")]
    [DataRow("theory", "theory", DisplayName = "Substring 'ty'")]
    [DataRow("imo-test", "imo-test", DisplayName = "Abbreviation as prefix - Corrected Expectation")] // Lookaround fixed
    [DataRow("test-imo", "test-imo", DisplayName = "Abbreviation as suffix - Corrected Expectation")] // Lookaround fixed
    public void Apply_AbbreviationAsSubstringOrAttached_DoesNotReplace(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("wtf", "wtf", DisplayName = "Unknown abbreviation WTF")]
    [DataRow("rofl", " rolling on the floor laughing ", DisplayName = "Known abbreviation ROFL - Corrected Expectation")] // ROFL is now known
    [DataRow("This is normal text", "This is normal text", DisplayName = "Sentence with no abbreviations")]
    public void Apply_UnknownAbbreviationsOrNormalText_NoChangeOrReplacesKnown(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Apply_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        string? input = null;

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => _rule.Apply(input!));
    }
}