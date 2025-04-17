using Microsoft.Extensions.DependencyInjection;
using TTSTextNormalization.Abstractions;
using TTSTextNormalization.DependencyInjection;

namespace TTSTextNormalization.Tests.Core;

[TestClass]
public class TextNormalizationPipelineTests
{
    // Helper to build the pipeline with specific rules for testing
    private static ITextNormalizer BuildNormalizer(Action<ITextNormalizationBuilder> configure)
    {
        ServiceCollection services = new();
        services.AddTextNormalization(configure);
        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ITextNormalizer>();
    }

    [TestMethod]
    public void Normalize_NullInput_ReturnsEmptyString()
    {
        ITextNormalizer normalizer = BuildNormalizer(builder => { }); // No rules
        Assert.AreEqual(string.Empty, normalizer.Normalize(null));
    }

    [TestMethod]
    public void Normalize_EmptyInput_ReturnsEmptyString()
    {
        ITextNormalizer normalizer = BuildNormalizer(builder => { }); // No rules
        Assert.AreEqual(string.Empty, normalizer.Normalize(""));
    }

    [TestMethod]
    public void Normalize_WhitespaceInput_ReturnsEmptyString()
    {
        ITextNormalizer normalizer = BuildNormalizer(builder => { }); // No rules
        Assert.AreEqual(string.Empty, normalizer.Normalize("   \t\n "));
    }

    [TestMethod]
    public void Normalize_NoRules_ReturnsOriginalText()
    {
        ITextNormalizer normalizer = BuildNormalizer(builder => { }); // No rules
        string input = "Some Text 123 !?";
        Assert.AreEqual(input, normalizer.Normalize(input));
    }

    [TestMethod]
    public void Normalize_SingleRule_Emoji_AppliesRule()
    {
        ITextNormalizer normalizer = BuildNormalizer(builder => builder.AddEmojiRule());
        string input = "Hello ✨ world";
        // Rule adds spaces, no whitespace cleanup rule added here
        string expected = "Hello  sparkles  world";
        Assert.AreEqual(expected, normalizer.Normalize(input));
    }

    [TestMethod]
    public void Normalize_SingleRule_Whitespace_AppliesRule()
    {
        ITextNormalizer normalizer = BuildNormalizer(builder => builder.AddWhitespaceNormalizationRule());
        string input = "  Extra   Spaces  ";
        string expected = "Extra Spaces";
        Assert.AreEqual(expected, normalizer.Normalize(input));
    }

    [TestMethod]
    public void Normalize_RuleOrder_SanitizeBeforeEmoji_WhitespaceAfter()
    {
        ITextNormalizer normalizer = BuildNormalizer(builder =>
        {
            // Explicitly add in order
            builder.AddBasicSanitizationRule(); // Order 10
            builder.AddEmojiRule(); // Order 100
            builder.AddWhitespaceNormalizationRule(); // Order 9000
        });

        // Input has fancy quotes and emoji needing sanitization first, then whitespace cleanup
        string input = "   ‘Hey’ ✨!!!   ";
        // Expected:
        // 1. Sanitize: "'Hey' ✨!!!"
        // 2. Emoji: "'Hey'  sparkles !!!"
        // 3. Whitespace: "'Hey' sparkles !!!"
        string expected = "'Hey' sparkles! ! !";

        Assert.AreEqual(expected, normalizer.Normalize(input));
    }

    [TestMethod]
    public void Normalize_RuleOrder_NumberBeforeWhitespace()
    {
        ITextNormalizer normalizer = BuildNormalizer(builder =>
        {
            builder.AddNumberNormalizationRule();
            builder.AddWhitespaceNormalizationRule();
        });
        // Number rule adds spaces, WS rule cleans up
        string input = "Value is 123";
        // Expected:
        // 1. Number: "Value is  one hundred and twenty-three  "
        // 2. Whitespace: "Value is one hundred and twenty-three"
        string expected = "Value is one hundred and twenty-three";
        Assert.AreEqual(expected, normalizer.Normalize(input));
    }

    // --- Comprehensive Test with All Rules ---
    [TestMethod]
    [DataRow(
        "  ‘Test’ 1st..  soooo   cool ✨!! LOL   Cost: $12.50 USD??? ",
        "'Test' first. soo cool sparkles! laughing out loud Cost: twelve dollars fifty cents USD?",
        DisplayName = "All Rules Integration Test 1 - Corrected"
    )]
    [DataRow(
        "BRB... 2nd place!! Got 2 pay €1,000.00!! 🤑🤑",
        "be right back. second place! Got two pay one thousand euros! money-mouth face money-mouth face", // Removed final space
        DisplayName = "All Rules Integration Test 2 - Corrected"
    )]
    [DataRow(
        "IDK man.... soooo many ??? GLHF! Check 123.45!",
        "I don't know man. soo many? good luck have fun! Check one hundred and twenty-three point four five!",
        DisplayName = "All Rules Integration Test 3 - Corrected"
    )]
    [DataRow(
        "  OMG!!! The price is £50.00??? LOL... IDK.  1st prize! ",
        "oh my god! The price is fifty pounds? laughing out loud. I don't know. first prize!",
        DisplayName = "All Rules Integration Test 4 - Mixed Punctuation & Abbr - Corrected"
    )]
    [DataRow(
        "  SOOOOooooo   MUCHHH Textt!!!  123rd ", // Note: LetterRepetition is case-insensitive
        "SOO MUCHH Textt! hundred and twenty-third", // Expect 'Textt' to remain as only 't' repeats twice
        DisplayName = "All Rules Integration Test 5 - Letter Repetition Focus - Corrected"
    )]
    public void Normalize_AllRulesEnabled_HandlesComplexInput(string input, string expected)
    {
        ITextNormalizer normalizer = BuildNormalizer(builder =>
        {
            builder.AddBasicSanitizationRule(); // 10
            builder.AddEmojiRule(); // 100
            builder.AddCurrencyRule(); // 200
            builder.AddAbbreviationNormalizationRule(); // 300
            builder.AddNumberNormalizationRule(); // 400
            builder.AddExcessivePunctuationRule(); // 500
            builder.AddLetterRepetitionRule(); // 510
            builder.AddWhitespaceNormalizationRule(); // 9000
        });

        // Act
        string result = normalizer.Normalize(input);

        // Assert
        Assert.AreEqual(expected, result);
    }
}
