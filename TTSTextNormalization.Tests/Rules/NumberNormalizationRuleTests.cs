using TTSTextNormalization.Rules;

namespace TTSTextNormalization.Tests.Rules;

[TestClass]
public class NumberNormalizationRuleTests
{
    private readonly NumberNormalizationRule _rule = new();

    [TestMethod]
    [DataRow("", "", DisplayName = "Empty Input")]
    [DataRow(" ", " ", DisplayName = "Whitespace Input")]
    [DataRow("No numbers here", "No numbers here", DisplayName = "Text without Numbers")]
    [DataRow("Item1", "Item1", DisplayName = "Number attached to text")]
    // FIX: Update expectation for multi-dot handling
    [DataRow("Version 1.2.3", "Version  one  point two  point three ", DisplayName = "Version Number Multi-Dot")]
    public void Apply_NoStandaloneOrPartial_HandlesCorrectly(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);
        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("0", " zero ", DisplayName = "Cardinal 0")]
    [DataRow("1", " one ", DisplayName = "Cardinal 1")]
    [DataRow("10", " ten ", DisplayName = "Cardinal 10")]
    [DataRow("15", " fifteen ", DisplayName = "Cardinal 15")]
    [DataRow("20", " twenty ", DisplayName = "Cardinal 20")]
    [DataRow("21", " twenty-one ", DisplayName = "Cardinal 21")]
    [DataRow("100", " one hundred ", DisplayName = "Cardinal 100")]
    [DataRow("101", " one hundred and one ", DisplayName = "Cardinal 101 (with and)")]
    [DataRow("123", " one hundred and twenty-three ", DisplayName = "Cardinal 123 (with and)")]
    [DataRow("1000", " one thousand ", DisplayName = "Cardinal 1000")]
    [DataRow("1001", " one thousand and one ", DisplayName = "Cardinal 1001 (with and)")]
    [DataRow("1234", " one thousand two hundred and thirty-four ", DisplayName = "Cardinal 1234 (with and)")]
    [DataRow("10000", " ten thousand ", DisplayName = "Cardinal 10k")]
    [DataRow("123456", " one hundred and twenty-three thousand four hundred and fifty-six ", DisplayName = "Cardinal Large (with and)")]
    [DataRow("1000000", " one million ", DisplayName = "Cardinal 1M")]
    public void Apply_StandaloneIntegers_ReplacesWithWords(string input, string expected)
    {
        string result = _rule.Apply(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("1st", " first ", DisplayName = "Ordinal 1st")]
    [DataRow("2nd", " second ", DisplayName = "Ordinal 2nd")]
    [DataRow("3rd", " third ", DisplayName = "Ordinal 3rd")]
    [DataRow("4th", " fourth ", DisplayName = "Ordinal 4th")]
    [DataRow("11th", " eleventh ", DisplayName = "Ordinal 11th")]
    [DataRow("12th", " twelfth ", DisplayName = "Ordinal 12th")]
    [DataRow("13th", " thirteenth ", DisplayName = "Ordinal 13th")]
    [DataRow("21st", " twenty-first ", DisplayName = "Ordinal 21st")]
    [DataRow("103rd", " hundred and third ", DisplayName = "Ordinal 103rd (with and)")]
    public void Apply_StandaloneOrdinals_ReplacesWithWords(string input, string expected)
    {
        string result = _rule.Apply(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("1.5", " one point five ", DisplayName = "Decimal 1.5")]
    [DataRow("0.25", " zero point two five ", DisplayName = "Decimal 0.25")]
    [DataRow("123.456", " one hundred and twenty-three point four five six ", DisplayName = "Decimal Long Fraction")]
    [DataRow("10.0", " ten point zero ", DisplayName = "Decimal Trailing Zero")]
    public void Apply_StandaloneDecimals_ReplacesWithWords(string input, string expected)
    {
        string result = _rule.Apply(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("Call 911 now", "Call  nine hundred and eleven  now", DisplayName = "Cardinal in sentence")]
    [DataRow("There are 3 apples.", "There are  three  apples.", DisplayName = "Cardinal at end of word")]
    [DataRow("Order 1 and 2", "Order  one  and  two ", DisplayName = "Multiple cardinals")]
    [DataRow("It's 1.5 meters", "It's  one point five  meters", DisplayName = "Decimal in sentence")]
    [DataRow("Get the 1st item", "Get the  first  item", DisplayName = "Ordinal in sentence")]
    [DataRow("Mix of 1st, 2 and 3.14", "Mix of  first ,  two  and  three point one four ", DisplayName = "Mixed numbers")]
    public void Apply_NumbersInContext_ReplacesCorrectly(string input, string expected)
    {
        string result = _rule.Apply(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("Item-123", "Item-123", DisplayName = "Number attached with hyphen")]
    [DataRow("1stPlace", "1stPlace", DisplayName = "Ordinal attached to text")]
    public void Apply_NonStandaloneNumbers_NoChange(string input, string expected)
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
}