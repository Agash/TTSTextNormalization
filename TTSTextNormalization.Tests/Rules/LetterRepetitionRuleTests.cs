using TTSTextNormalization.Rules;

namespace TTSTextNormalization.Tests.Rules;

[TestClass]
public class LetterRepetitionRuleTests
{
    private readonly LetterRepetitionRule _rule = new();

    [TestMethod]
    [DataRow("", "", DisplayName = "Empty Input")]
    [DataRow(" ", " ", DisplayName = "Whitespace Input")]
    [DataRow("Normal", "Normal", DisplayName = "No Repetitions")]
    [DataRow("Hello", "Hello", DisplayName = "Double Letter (Allowed)")]
    [DataRow("Bookkeeper", "Bookkeeper", DisplayName = "Multiple Double Letters (Allowed)")]
    [DataRow("a", "a", DisplayName = "Single Letter")]
    [DataRow("aa", "aa", DisplayName = "Double Letter")]
    public void Apply_NoExcessiveRepetitions_ReturnsInput(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);
        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("soooo", "soo", DisplayName = "Simple Case 'ooo'")]
    [DataRow("yesss", "yess", DisplayName = "Simple Case 'sss'")]
    [DataRow("heLLLLo", "heLLo", DisplayName = "Mixed Case 'LLLL'")] // Note: Replacement uses original case
    [DataRow("YeeeeeS", "YeeS", DisplayName = "Mixed Case 'eeeee'")]
    [DataRow("BuZZZing", "BuZZing", DisplayName = "Internal Repetition 'ZZZ'")]
    [DataRow("aaabbbccc", "aabbcc", DisplayName = "Multiple Repetitions")]
    [DataRow("Whoooops", "Whoops", DisplayName = "Internal 'ooo'")]
    [DataRow("Hmmmm...", "Hmm...", DisplayName = "Repetition Before Punctuation")]
    [DataRow("HMMMM...", "HMM...", DisplayName = "Uppercase Repetition Before Punctuation")]
    public void Apply_ExcessiveRepetitions_ReducesToTwoLetters(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);
        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("Whaaaaat", "Whaat", DisplayName = "Whaaaaat -> Whaat")]
    [DataRow("Grrrrreat", "Grreat", DisplayName = "Grrrrreat -> Grreat")]
    public void Apply_ComplexCases_ReducesCorrectly(string input, string expected)
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