using TTSTextNormalization.Rules;

namespace TTSTextNormalization.Tests.Rules;

[TestClass]
public class CurrencyNormalizationRuleTests
{
    private readonly CurrencyNormalizationRule _rule = new();

    [TestMethod]
    [DataRow("", "", DisplayName = "Empty Input")]
    [DataRow(" ", " ", DisplayName = "Whitespace Input")]
    [DataRow("No currency here", "No currency here", DisplayName = "Text without Currency")]
    public void Apply_NoCurrency_ReturnsInput(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    // NOTE: Expectations updated for default Humanizer output (includes "and")
    [TestMethod]
    // Symbol First
    [DataRow("$1", " one dollar ", DisplayName = "USD Simple ($)")]
    [DataRow("$1.00", " one dollar ", DisplayName = "USD Simple zero cents ($)")]
    [DataRow("$1.50", " one dollar fifty cents ", DisplayName = "USD with Cents ($)")] // No "and" for cents usually
    [DataRow("$1,234.56", " one thousand two hundred and thirty-four dollars fifty-six cents ", DisplayName = "USD Large with Cents ($)")]
    [DataRow("£10", " ten pounds ", DisplayName = "GBP Simple (£)")]
    [DataRow("£0.50", " zero pounds fifty pence ", DisplayName = "GBP Only Pence (£)")]
    [DataRow("€100", " one hundred euros ", DisplayName = "EUR Simple (€)")]
    [DataRow("€1.25", " one euro twenty-five cents ", DisplayName = "EUR With Cents (€)")]
    [DataRow("¥500", " five hundred yen ", DisplayName = "JPY Simple (¥)")]
    // Code Last
    [DataRow("1 USD", " one dollar ", DisplayName = "USD Code Simple")]
    [DataRow("1.00 USD", " one dollar ", DisplayName = "USD Code zero cents")]
    [DataRow("1.50 USD", " one dollar fifty cents ", DisplayName = "USD Code with Cents")]
    [DataRow("1,234.56 USD", " one thousand two hundred and thirty-four dollars fifty-six cents ", DisplayName = "USD Code Large")]
    [DataRow("10 GBP", " ten pounds ", DisplayName = "GBP Code Simple")] // Uses "pound" from map
    [DataRow("0.50 GBP", " zero pounds fifty pence ", DisplayName = "GBP Code Only Pence")]
    [DataRow("100 EUR", " one hundred euros ", DisplayName = "EUR Code Simple")]
    [DataRow("1.25 EUR", " one euro twenty-five cents ", DisplayName = "EUR Code With Cents")]
    [DataRow("500 JPY", " five hundred yen ", DisplayName = "JPY Code Simple")] // Uses "yen" from map
    [DataRow("100 CAD", " one hundred Canadian dollars ", DisplayName = "CAD Code Example")]
    [DataRow("10 BRL", " ten reais ", DisplayName = "BRL Code Example")]
    public void Apply_KnownCurrencies_ReplacesWithSpokenForm(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("Send $10 now", "Send  ten dollars  now", DisplayName = "Currency within sentence")]
    [DataRow("It costs 50 EUR.", "It costs  fifty euros .", DisplayName = "Currency at end of sentence")]
    [DataRow("$5 and £10", " five dollars  and  ten pounds ", DisplayName = "Multiple different currencies")]
    public void Apply_CurrencyInContext_ReplacesCorrectly(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("10XYZ", "10XYZ", DisplayName = "Unknown Code XYZ")]
    [DataRow("¤10", "¤10", DisplayName = "Generic Currency Symbol")]
    [DataRow("$10MXN", "$10MXN", DisplayName = "Symbol and Code")]
    public void Apply_UnknownOrAmbiguousCurrency_NoChange(string input, string expected)
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