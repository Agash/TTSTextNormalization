using TTSTextNormalization.Rules;

namespace TTSTextNormalization.Tests.Rules;

[TestClass]
public class BasicSanitizationRuleTests
{
    private readonly BasicSanitizationRule _rule = new();

    [TestMethod]
    [DataRow("", "", DisplayName = "Empty Input")]
    [DataRow(" ", " ", DisplayName = "Whitespace Input (no trim in this rule)")]
    [DataRow("Normal Text", "Normal Text", DisplayName = "Normal Text Input")]
    public void Apply_SimpleValidInput_ReturnsInput(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);
        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("Hello\r\nWorld", "Hello\nWorld", DisplayName = "CRLF to LF")]
    [DataRow("Hello\rWorld", "Hello\nWorld", DisplayName = "CR to LF")]
    [DataRow("Hello\nWorld", "Hello\nWorld", DisplayName = "LF remains LF")]
    [DataRow("Mixed\r\nCR\rLF\n", "Mixed\nCR\nLF\n", DisplayName = "Mixed Line Breaks")]
    public void Apply_NormalizesLineBreaks(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);
        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("Text\u0000WithNull", "TextWithNull", DisplayName = "Removes Null Char (U+0000)")]
    [DataRow("Zero\u200BWidth\u200CSpace\u200DTest", "ZeroWidthSpaceTest", DisplayName = "Removes Zero Width Spaces/Joiners (U+200B-D)")]
    [DataRow("\uFEFFLeadingBOM", "LeadingBOM", DisplayName = "Removes BOM (U+FEFF)")]
    [DataRow("With\u0007Bell", "WithBell", DisplayName = "Removes Bell Char (U+0007)")]
    [DataRow("Control\u001AChars", "ControlChars", DisplayName = "Removes SUB Char (U+001A)")]
    [DataRow("Keep\tTabs\nNewlines", "Keep\tTabs\nNewlines", DisplayName = "Keeps Tab and Newline")]
    public void Apply_RemovesProblematicControlChars(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);
        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("“Smart” Quotes", "\"Smart\" Quotes", DisplayName = "Replaces Smart Double Quotes")]
    [DataRow("‘Smart’ Apostrophe", "'Smart' Apostrophe", DisplayName = "Replaces Smart Single Quotes")]
    [DataRow("Ellipsis…", "Ellipsis...", DisplayName = "Replaces Ellipsis")]
    [DataRow("Em—Dash", "Em-Dash", DisplayName = "Replaces Em Dash")]
    [DataRow("En–Dash", "En-Dash", DisplayName = "Replaces En Dash")]
    [DataRow("«Guillemets»", "\"Guillemets\"", DisplayName = "Replaces Double Guillemets")]
    [DataRow("‹Guillemets›", "'Guillemets'", DisplayName = "Replaces Single Guillemets - Corrected")]
    [DataRow("Mix: “Smart” & ‘Apostrophe’…", "Mix: \"Smart\" & 'Apostrophe'...", DisplayName = "Replaces Mixed Fancy Chars")]
    public void Apply_ReplacesFancyCharsWithAscii(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);
        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("This is \u0000 clean \r\n text with ‘fancy’ stuff.", "This is  clean \n text with 'fancy' stuff.", DisplayName = "Integration of all rules")]
    public void Apply_CombinesAllSanitizations(string input, string expected)
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