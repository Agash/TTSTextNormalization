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
    [DataRow("$1", " one US dollar ", DisplayName = "USD Simple ($)")]
    [DataRow("$1.00", " one US dollar ", DisplayName = "USD Simple zero cents ($)")]
    [DataRow("$1.50", " one US dollar fifty cents ", DisplayName = "USD with Cents ($)")]
    [DataRow("$1,234.56", " one thousand two hundred and thirty-four US dollars fifty-six cents ", DisplayName = "USD Large with Cents ($)")]
    [DataRow("£10", " ten British pounds ", DisplayName = "GBP Simple (£)")]
    [DataRow("£0.50", " zero British pounds fifty pence ", DisplayName = "GBP Only Pence (£)")]
    [DataRow("€100", " one hundred euros ", DisplayName = "EUR Simple (€)")]
    [DataRow("€1.25", " one euro twenty-five cents ", DisplayName = "EUR With Cents (€)")]
    [DataRow("¥500", " five hundred Japanese yen ", DisplayName = "JPY Simple (¥)")]
    // Code Last
    [DataRow("1 USD", " one US dollar ", DisplayName = "USD Code Simple")]
    [DataRow("1.00 USD", " one US dollar ", DisplayName = "USD Code zero cents")]
    [DataRow("1.50 USD", " one US dollar fifty cents ", DisplayName = "USD Code with Cents")]
    [DataRow("1,234.56 USD", " one thousand two hundred and thirty-four US dollars fifty-six cents ", DisplayName = "USD Code Large")]
    [DataRow("10 GBP", " ten British pounds ", DisplayName = "GBP Code Simple")]
    [DataRow("0.50 GBP", " zero British pounds fifty pence ", DisplayName = "GBP Code Only Pence")]
    [DataRow("100 EUR", " one hundred euros ", DisplayName = "EUR Code Simple")]
    [DataRow("1.25 EUR", " one euro twenty-five cents ", DisplayName = "EUR Code With Cents")]
    [DataRow("500 JPY", " five hundred Japanese yen ", DisplayName = "JPY Code Simple")]
    [DataRow("100 CAD", " one hundred Canadian dollars ", DisplayName = "CAD Code Example")]
    [DataRow("10 BRL", " ten Brazilian reais ", DisplayName = "BRL Code Example")]
    // Combined
    [DataRow("$10 USD", " ten US dollars ", DisplayName = "USD Combined ($)")]
    [DataRow("$10USD", " ten US dollars ", DisplayName = "USD Combined (wihtout spaces)")]
    [DataRow("$10MXN", " ten Mexican pesos ", DisplayName = "MXN Combined (without spaces)")]
    [DataRow("$10 CAD", " ten Canadian dollars ", DisplayName = "CAD Combined ($)")]
    [DataRow("£10 GBP", " ten British pounds ", DisplayName = "GBP Combined (£)")]
    [DataRow("€100 EUR", " one hundred euros ", DisplayName = "EUR Combined (€)")]
    [DataRow("¥500 JPY", " five hundred Japanese yen ", DisplayName = "JPY Combined (¥)")]
    [DataRow("10 USD $", " ten US dollars  $", DisplayName = "USD Combined with Trailing Symbol")]
    [DataRow("10 GBP £", " ten British pounds  £", DisplayName = "GBP Combined with Trailing Symbol")]
    [DataRow("100 EUR €", " one hundred euros  €", DisplayName = "EUR Combined with Trailing Symbol")]
    [DataRow("500 JPY ¥", " five hundred Japanese yen  ¥", DisplayName = "JPY Combined with Trailing Symbol")]
    public void Apply_KnownCurrencies_ReplacesWithSpokenForm(string input, string expected)
    {
        // Act
        string result = _rule.Apply(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("Send $10 now", "Send  ten US dollars  now", DisplayName = "Currency within sentence")]
    [DataRow("It costs 50 EUR.", "It costs  fifty euros .", DisplayName = "Currency at end of sentence")]
    [DataRow("It costs 50 EUR now.", "It costs  fifty euros  now.", DisplayName = "Currency within sentence")]
    [DataRow("$5 and £10", " five US dollars  and  ten British pounds ", DisplayName = "Multiple different currencies")]
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