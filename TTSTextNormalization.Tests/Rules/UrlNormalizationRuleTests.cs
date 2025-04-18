using Microsoft.Extensions.Options;
using TTSTextNormalization.Rules;

namespace TTSTextNormalization.Tests.Rules;

[TestClass]
public class UrlNormalizationRuleTests
{
    // Use default options for standard tests
    private readonly UrlNormalizationRule _rule = new(Options.Create(new UrlRuleOptions()));
    private const string DefaultPlaceholder = " link "; // Default for comparison

    [TestMethod]
    [DataRow("", "", DisplayName = "Empty Input")]
    [DataRow(" ", " ", DisplayName = "Whitespace Input")]
    [DataRow("Just normal text.", "Just normal text.", DisplayName = "No URL")]
    [DataRow("example.com", "example.com", DisplayName = "Domain without protocol/www")]
    [DataRow("test@example.com", "test@example.com", DisplayName = "Email address")]
    [DataRow("file.txt", "file.txt", DisplayName = "Filename")]
    [DataRow(
        "Myhttp://example.com",
        "Myhttp://example.com",
        DisplayName = "HTTP attached start (No Match - Lookbehind)"
    )]
    public void Apply_NoUrlsOrInvalidPatterns_ReturnsInput(string input, string expected)
    {
        string result = _rule.Apply(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("http://example.com", DefaultPlaceholder, DisplayName = "HTTP Basic")]
    [DataRow("https://example.com", DefaultPlaceholder, DisplayName = "HTTPS Basic")]
    [DataRow("www.example.com", DefaultPlaceholder, DisplayName = "WWW Basic")]
    [DataRow(
        "https://sub.domain.co.uk",
        DefaultPlaceholder,
        DisplayName = "HTTPS Subdomain UK TLD"
    )]
    [DataRow("http://example.com/", DefaultPlaceholder, DisplayName = "HTTP Trailing Slash")]
    [DataRow("https://example.com/path", DefaultPlaceholder, DisplayName = "HTTPS with Path")]
    [DataRow(
        "https://example.com/path/to/resource",
        DefaultPlaceholder,
        DisplayName = "HTTPS Long Path"
    )]
    [DataRow("http://example.com?query=value", DefaultPlaceholder, DisplayName = "HTTP with Query")]
    [DataRow(
        "www.example.com?query=value&other=1",
        DefaultPlaceholder,
        DisplayName = "WWW with Multiple Queries"
    )]
    [DataRow(
        "https://example.com/page#section",
        DefaultPlaceholder,
        DisplayName = "HTTPS with Fragment"
    )]
    [DataRow("http://localhost", DefaultPlaceholder, DisplayName = "HTTP Localhost")]
    [DataRow("http://localhost:8080", DefaultPlaceholder, DisplayName = "HTTP Localhost with Port")]
    [DataRow(
        "https://192.168.1.1",
        DefaultPlaceholder,
        DisplayName = "IP Address URL (Match Expected)"
    )]
    [DataRow("WWW.EXAMPLE.COM", DefaultPlaceholder, DisplayName = "WWW Uppercase")]
    [DataRow("Http://Example.Com/Path", DefaultPlaceholder, DisplayName = "Mixed Case")]
    [DataRow("http://example.com.", $"{DefaultPlaceholder}.", DisplayName = "HTTP followed by dot")] // Trailing dot excluded by lookahead
    public void Apply_ValidUrls_ReplacesWithDefaultPlaceholder(string input, string expected)
    {
        string result = _rule.Apply(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow(
        "Check this: http://example.com.",
        $"Check this: {DefaultPlaceholder}.",
        DisplayName = "URL at end with period"
    )]
    [DataRow(
        "Go to www.example.com!",
        $"Go to {DefaultPlaceholder}!",
        DisplayName = "URL at end with exclamation"
    )]
    [DataRow(
        "Is https://example.com? the site?",
        $"Is {DefaultPlaceholder}? the site?",
        DisplayName = "URL mid-sentence with question mark"
    )]
    [DataRow(
        "Visit (www.example.com) for info.",
        $"Visit ({DefaultPlaceholder}) for info.",
        DisplayName = "URL in parentheses"
    )]
    [DataRow(
        "Link: \"https://example.com\", is good.",
        $"Link: \"{DefaultPlaceholder}\", is good.",
        DisplayName = "URL in quotes with comma"
    )]
    [DataRow(
        "URL1: http://test.com URL2: www.test.org",
        $"URL1: {DefaultPlaceholder} URL2: {DefaultPlaceholder}",
        DisplayName = "Multiple URLs"
    )]
    [DataRow(
        "Text before www.example.com and after.",
        $"Text before {DefaultPlaceholder} and after.",
        DisplayName = "URL surrounded by text"
    )]
    [DataRow(
        "NoSpaceBeforewww.example.com",
        "NoSpaceBeforewww.example.com",
        DisplayName = "URL attached start (No Match)"
    )]
    [DataRow(
        "www.example.comNoSpaceAfter",
        DefaultPlaceholder,
        DisplayName = "URL attached end (No Match)"
    )]
    public void Apply_UrlsInContext_ReplacesCorrectlyWithDefaultPlaceholder(
        string input,
        string expected
    )
    {
        string result = _rule.Apply(input);
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

    // --- New Test for Custom Placeholder Option ---
    [TestMethod]
    public void Apply_WithOptions_UsesCustomPlaceholder()
    {
        // Arrange
        const string customPlaceholder = "[WEBSITE]";
        UrlRuleOptions options = new() { PlaceholderText = customPlaceholder };
        UrlNormalizationRule ruleWithOptions = new(Options.Create(options));
        string input = "Visit www.example.com today!";
        string expected = $"Visit {customPlaceholder} today!";

        // Act
        string result = ruleWithOptions.Apply(input);

        // Assert
        Assert.AreEqual(expected, result);
    }
}
