using TTSTextNormalization.Rules;

namespace TTSTextNormalization.Tests.Rules;

[TestClass]
public class EmojiNormalizationRuleTests
{
    private readonly EmojiNormalizationRule _rule = new();

    [TestMethod]
    [DataRow("", "", DisplayName = "Empty Input")]
    [DataRow(" ", " ", DisplayName = "Whitespace Input")]
    [DataRow("No emojis here.", "No emojis here.", DisplayName = "Text without Emojis")]
    public void Apply_NoEmojis_ReturnsInput(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);
        // Assert
        Assert.AreEqual(expected, result);
    }

    // NOTE: Expected output relies on the 'name' field from data-by-emoji.json
    [TestMethod]
    [DataRow("👍", " thumbs up ", DisplayName = "Thumbs Up")]
    [DataRow("❤️", " red heart ", DisplayName = "Red Heart")]
    [DataRow("✨", " sparkles ", DisplayName = "Sparkles")]
    [DataRow("🚀", " rocket ", DisplayName = "Rocket")]
    [DataRow("Check this out: 😊", "Check this out:  smiling face with smiling eyes ", DisplayName = "Emoji at end - Corrected")]
    [DataRow("🍕 Party!", " pizza  Party!", DisplayName = "Emoji at start")]
    [DataRow("🍕👍", " pizza  thumbs up ", DisplayName = "Multiple adjacent emojis - Corrected")]
    [DataRow("Text ✨ with ✨ sparkles", "Text  sparkles  with  sparkles  sparkles", DisplayName = "Repeated emoji")]
    [DataRow("Flag: 🇬🇧", "Flag:  flag United Kingdom ", DisplayName = "Flag Emoji (UK) - Corrected")]
    [DataRow("Combined: 🧑‍💻", "Combined:  technologist ", DisplayName = "ZWJ Sequence (Technologist) - Corrected")]
    public void Apply_KnownEmojis_ReplacesWithGeneratedName(string input, string expected)
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

    [TestMethod]
    public void Apply_NonEmojiCharacter_NoChange()
    {
        // Test with a character that isn't in the generated emoji map
        string input = "Text with \u03A9"; // Greek Omega
        string expected = "Text with \u03A9";
        // Act
        string result = _rule.Apply(input);
        // Assert
        Assert.AreEqual(expected, result);
    }
}