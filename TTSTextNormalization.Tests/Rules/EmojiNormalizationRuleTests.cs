using Microsoft.Extensions.Options; // Add this
using TTSTextNormalization.Rules;

namespace TTSTextNormalization.Tests.Rules;

[TestClass]
public class EmojiNormalizationRuleTests
{
    // Use default options for standard tests
    private readonly EmojiNormalizationRule _rule = new(Options.Create(new EmojiRuleOptions()));

    [TestMethod]
    [DataRow("", "", DisplayName = "Empty Input")]
    [DataRow(" ", " ", DisplayName = "Whitespace Input")]
    [DataRow("No emojis here.", "No emojis here.", DisplayName = "Text without Emojis")]
    public void Apply_NoEmojis_ReturnsInput(string input, string expected)
    {
        string result = _rule.Apply(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("👍", " thumbs up ", DisplayName = "Thumbs Up")]
    [DataRow("❤️", " red heart ", DisplayName = "Red Heart")]
    [DataRow("✨", " sparkles ", DisplayName = "Sparkles")]
    [DataRow("🚀", " rocket ", DisplayName = "Rocket")]
    [DataRow(
        "Check this out: 😊",
        "Check this out:  smiling face with smiling eyes ",
        DisplayName = "Emoji at end"
    )]
    [DataRow("🍕 Party!", " pizza  Party!", DisplayName = "Emoji at start")]
    [DataRow("🍕👍", " pizza  thumbs up ", DisplayName = "Multiple adjacent emojis")]
    [DataRow(
        "Text ✨ with ✨ sparkles",
        "Text  sparkles  with  sparkles  sparkles",
        DisplayName = "Repeated emoji"
    )]
    [DataRow("Flag: 🇬🇧", "Flag:  flag United Kingdom ", DisplayName = "Flag Emoji (UK)")]
    [DataRow(
        "Combined: 🧑‍💻",
        "Combined:  technologist ",
        DisplayName = "ZWJ Sequence (Technologist)"
    )]
    public void Apply_KnownEmojis_ReplacesWithGeneratedName(string input, string expected)
    {
        string result = _rule.Apply(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Apply_NullInput_ThrowsArgumentNullException()
    {
        string? input = null;
        Assert.ThrowsException<ArgumentNullException>(() => _rule.Apply(input!));
    }

    [TestMethod]
    public void Apply_NonEmojiCharacter_NoChange()
    {
        string input = "Text with \u03A9"; // Greek Omega
        string expected = "Text with \u03A9";
        string result = _rule.Apply(input);
        Assert.AreEqual(expected, result);
    }

    // --- New Tests for Options ---
    [TestMethod]
    public void Apply_WithOptions_AddsPrefix()
    {
        // Arrange
        EmojiRuleOptions options = new() { Prefix = "emoji:" }; // Note: space is implicit
        EmojiNormalizationRule ruleWithOptions = new(Options.Create(options));
        string input = "I feel 😊 today";
        string expected = "I feel  emoji: smiling face with smiling eyes  today";

        // Act
        string result = ruleWithOptions.Apply(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Apply_WithOptions_AddsSuffix()
    {
        // Arrange
        EmojiRuleOptions options = new() { Suffix = "(emoji)" }; // Note: space is implicit
        EmojiNormalizationRule ruleWithOptions = new(Options.Create(options));
        string input = "Party time 🚀";
        string expected = "Party time  rocket (emoji) ";

        // Act
        string result = ruleWithOptions.Apply(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Apply_WithOptions_AddsPrefixAndSuffix()
    {
        // Arrange
        EmojiRuleOptions options = new() { Prefix = "The", Suffix = "Emoji" };
        EmojiNormalizationRule ruleWithOptions = new(Options.Create(options));
        string input = "It's a 🍕";
        // Expected: " It's a  The pizza Emoji  "
        string expected = "It's a  The pizza Emoji ";

        // Act
        string result = ruleWithOptions.Apply(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Apply_WithOptions_PrefixSuffixWithExistingSpaces()
    {
        // Arrange
        EmojiRuleOptions options = new() { Prefix = "Prefix ", Suffix = " Suffix" };
        EmojiNormalizationRule ruleWithOptions = new(Options.Create(options));
        string input = "Test 👍 this";
        // Expected: " Test  Prefix thumbs up Suffix  this" (extra internal spaces handled by evaluator logic)
        string expected = "Test  Prefix thumbs up Suffix  this";

        // Act
        string result = ruleWithOptions.Apply(input);

        // Assert
        Assert.AreEqual(expected, result);
    }
}
