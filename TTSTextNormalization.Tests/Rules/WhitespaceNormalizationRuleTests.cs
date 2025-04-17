using TTSTextNormalization.Rules;

namespace TTSTextNormalization.Tests.Rules;

[TestClass]
public class WhitespaceNormalizationRuleTests
{
    private readonly WhitespaceNormalizationRule _rule = new();

    [TestMethod]
    [DataRow("", "", DisplayName = "Empty Input")]
    [DataRow(" ", "", DisplayName = "Single Space Input")]
    [DataRow("\t", "", DisplayName = "Single Tab Input")]
    [DataRow("  \t  \n ", "", DisplayName = "Multiple Whitespace Input")]
    public void Apply_EmptyOrWhitespaceInput_ReturnsEmptyString(string input, string expected)
    {
        string result = _rule.Apply(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow(" test ", "test", DisplayName = "Leading/Trailing Spaces")]
    [DataRow("\t test \t", "test", DisplayName = "Leading/Trailing Tabs")]
    [DataRow("  leading", "leading", DisplayName = "Leading Multiple Spaces")]
    [DataRow("trailing  ", "trailing", DisplayName = "Trailing Multiple Spaces")]
    [DataRow("\n test \n", "test", DisplayName = "Leading/Trailing Newlines (after trim)")]
    public void Apply_TrimsLeadingAndTrailingWhitespace(string input, string expected)
    {
        string result = _rule.Apply(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("word  word", "word word", DisplayName = "Multiple Spaces Between Words")]
    [DataRow("word\t\tword", "word word", DisplayName = "Multiple Tabs Between Words")]
    [DataRow("word \t word", "word word", DisplayName = "Mixed Space/Tab Between Words")]
    [DataRow("word   \t  word", "word word", DisplayName = "Multiple Mixed Whitespace")]
    [DataRow(" first   second   third ", "first second third", DisplayName = "Multiple Groups and Trim")]
    public void Apply_CollapsesMultipleInternalWhitespaceToOneSpace(string input, string expected)
    {
        string result = _rule.Apply(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("no_extra_spaces", "no_extra_spaces", DisplayName = "No Extra Spaces")]
    [DataRow("single space", "single space", DisplayName = "Single Internal Space")]
    [DataRow("a", "a", DisplayName = "Single Character")]
    [DataRow("already.trimmed", "already. trimmed", DisplayName = "Already Trimmed and Collapsed")]
    public void Apply_NoChangeNeeded_ReturnsOriginalTrimmedString(string input, string expected)
    {
        string result = _rule.Apply(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("Hello !", "Hello!", DisplayName = "Removes space before !")]
    [DataRow("Hello ?", "Hello?", DisplayName = "Removes space before ?")]
    [DataRow("Hello .", "Hello.", DisplayName = "Removes space before .")]
    [DataRow("Hello ,", "Hello,", DisplayName = "Removes space before ,")]
    [DataRow("Hello ;", "Hello;", DisplayName = "Removes space before ;")]
    [DataRow("Hello :", "Hello:", DisplayName = "Removes space before :")]
    [DataRow("Hello  !", "Hello!", DisplayName = "Removes multiple spaces before !")]
    [DataRow("Word1 ! Word2 ?", "Word1! Word2?", DisplayName = "Multiple spaces before punctuation")]
    public void Apply_RemovesSpaceBeforePunctuation(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);
        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("Hello!World", "Hello! World", DisplayName = "Ensures space after !")]
    [DataRow("Hello?World", "Hello? World", DisplayName = "Ensures space after ?")]
    [DataRow("Hello.World", "Hello. World", DisplayName = "Ensures space after .")]
    [DataRow("Hello,World", "Hello, World", DisplayName = "Ensures space after ,")]
    [DataRow("Hello;World", "Hello; World", DisplayName = "Ensures space after ;")]
    [DataRow("Hello:World", "Hello: World", DisplayName = "Ensures space after :")]
    [DataRow("Hello! World", "Hello! World", DisplayName = "Keeps existing space after !")]
    [DataRow("End.", "End.", DisplayName = "No space added at end of string")]
    [DataRow("End!", "End!", DisplayName = "No space added at end of string !")]
    [DataRow("End?", "End?", DisplayName = "No space added at end of string ?")]
    [DataRow("Okay.Next", "Okay. Next", DisplayName = "Mix ensure after")]
    public void Apply_EnsuresSpaceAfterPunctuation(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);
        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("Hello ! World . Bye ?", "Hello! World. Bye?", DisplayName = "Integration - Before and After")]
    [DataRow("Test   :   Okay", "Test: Okay", DisplayName = "Integration - Collapse and Spacing")]
    [DataRow("Multiple   things , like   this ; end . ", "Multiple things, like this; end.", DisplayName = "Integration - Full Sentence")]
    public void Apply_HandlesPunctuationSpacingIntegration(string input, string expected)
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