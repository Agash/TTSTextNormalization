using TTSTextNormalization.Rules;

namespace TTSTextNormalization.Tests.Rules;

[TestClass]
public class ExcessivePunctuationRuleTests
{
    private readonly ExcessivePunctuationRule _rule = new();

    [TestMethod]
    [DataRow("", "", DisplayName = "Empty Input")]
    [DataRow(" ", " ", DisplayName = "Whitespace Input")]
    [DataRow("Hello world.", "Hello world.", DisplayName = "Single Period")]
    [DataRow("Hello world!", "Hello world!", DisplayName = "Single Exclamation")]
    [DataRow("Hello world?", "Hello world?", DisplayName = "Single Question Mark")]
    [DataRow("No excess here.", "No excess here.", DisplayName = "Normal Sentence")]
    [DataRow("Mixed!?.", "Mixed!?.", DisplayName = "Mixed Single Punctuation")]
    public void Apply_NoExcessivePunctuation_ReturnsInput(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);
        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("Hello!!", "Hello!", DisplayName = "Double Exclamation")]
    [DataRow("Hello!!!", "Hello!", DisplayName = "Triple Exclamation")]
    [DataRow("Hello!!!!!!!!", "Hello!", DisplayName = "Many Exclamations")]
    [DataRow("Why???", "Why?", DisplayName = "Triple Question Mark")]
    [DataRow("Why??????", "Why?", DisplayName = "Many Question Marks")]
    [DataRow("End...", "End.", DisplayName = "Triple Period")]
    [DataRow("End.........", "End.", DisplayName = "Many Periods")]
    public void Apply_ExcessiveSingleTypePunctuation_ReducesToOne(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);
        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("Really!?!?", "Really!?!?", DisplayName = "Mixed Excessive Punctuation 1")]
    [DataRow("What??!!", "What?!", DisplayName = "Mixed Excessive Punctuation 2")]
    [DataRow("Okay... Sure!!! No way??", "Okay. Sure! No way?", DisplayName = "Multiple Groups Mixed")]
    [DataRow("Test.. Test!! Test??", "Test. Test! Test?", DisplayName = "Separated Groups")]
    [DataRow("...!!!???...", ".!?.", DisplayName = "Consecutive Mixed Groups")]
    public void Apply_ExcessiveMixedTypePunctuation_ReducesEachSequenceToOne(string input, string expected)
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