using TTSTextNormalization.Rules;

namespace TTSTextNormalization.Tests.Rules;

[TestClass]
public class UrlNormalizationRuleTests
{
    private readonly UrlNormalizationRule _rule = new();
    private const string Placeholder = " link ";

    [TestMethod]
    [DataRow("", "", DisplayName = "Empty Input")]
    [DataRow(" ", " ", DisplayName = "Whitespace Input")]
    [DataRow("Just normal text.", "Just normal text.", DisplayName = "No URL")]
    [DataRow("example.com", "example.com", DisplayName = "Domain without protocol/www")]
    [DataRow("test@example.com", "test@example.com", DisplayName = "Email address")]
    [DataRow("file.txt", "file.txt", DisplayName = "Filename")]
    [DataRow("Myhttp://example.com", "Myhttp://example.com", DisplayName = "HTTP attached start (No Match - Lookbehind)")]
    public void Apply_NoUrlsOrInvalidPatterns_ReturnsInput(string input, string expected)
    {
        string result = _rule.Apply(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("http://example.com", Placeholder, DisplayName = "HTTP Basic")]
    [DataRow("https://example.com", Placeholder, DisplayName = "HTTPS Basic")]
    [DataRow("www.example.com", Placeholder, DisplayName = "WWW Basic")] // Uri.TryCreate needs help, see evaluator
    [DataRow("https://sub.domain.co.uk", Placeholder, DisplayName = "HTTPS Subdomain UK TLD")]
    [DataRow("http://example.com/", Placeholder, DisplayName = "HTTP Trailing Slash")]
    [DataRow("https://example.com/path", Placeholder, DisplayName = "HTTPS with Path")]
    [DataRow("https://example.com/path/to/resource", Placeholder, DisplayName = "HTTPS Long Path")]
    [DataRow("http://example.com?query=value", Placeholder, DisplayName = "HTTP with Query")]
    [DataRow("www.example.com?query=value&other=1", Placeholder, DisplayName = "WWW with Multiple Queries")]
    [DataRow("https://example.com/page#section", Placeholder, DisplayName = "HTTPS with Fragment")]
    [DataRow("http://localhost", Placeholder, DisplayName = "HTTP Localhost")] // Uri.TryCreate handles localhost
    [DataRow("http://localhost:8080", Placeholder, DisplayName = "HTTP Localhost with Port")]
    // Uri.TryCreate generally handles IP addresses if they have a scheme
    [DataRow("https://192.168.1.1", Placeholder, DisplayName = "IP Address URL (Match Expected)")]
    [DataRow("WWW.EXAMPLE.COM", Placeholder, DisplayName = "WWW Uppercase")]
    [DataRow("Http://Example.Com/Path", Placeholder, DisplayName = "Mixed Case")]
    [DataRow("http://example.com.", $"{Placeholder}.", DisplayName = "HTTP followed by dot")] // Trailing dot excluded by lookahead
    [DataRow("http://ex ample.com", $"{Placeholder} ample.com", DisplayName = "URL with space")]
    public void Apply_ValidUrls_ReplacesWithPlaceholder(string input, string expected)
    {
        string result = _rule.Apply(input);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("Check this: http://example.com.", $"Check this: {Placeholder}.", DisplayName = "URL at end with period")]
    [DataRow("Go to www.example.com!", $"Go to {Placeholder}!", DisplayName = "URL at end with exclamation")]
    [DataRow("Is https://example.com? the site?", $"Is {Placeholder}? the site?", DisplayName = "URL mid-sentence with question mark")]
    [DataRow("Visit (www.example.com) for info.", $"Visit ({Placeholder}) for info.", DisplayName = "URL in parentheses")]
    [DataRow("Link: \"https://example.com\", is good.", $"Link: \"{Placeholder}\", is good.", DisplayName = "URL in quotes with comma")]
    [DataRow("URL1: http://test.com URL2: www.test.org", $"URL1: {Placeholder} URL2: {Placeholder}", DisplayName = "Multiple URLs")]
    [DataRow("Text before www.example.com and after.", $"Text before {Placeholder} and after.", DisplayName = "URL surrounded by text")]
    [DataRow("NoSpaceBeforewww.example.com", "NoSpaceBeforewww.example.com", DisplayName = "URL attached start (No Match, no www., no http(s))")]
    [DataRow("www.example.comNoSpaceAfter", $"{Placeholder}", DisplayName = "URL attached end (detected www._._")]
    public void Apply_UrlsInContext_ReplacesCorrectly(string input, string expected)
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
}