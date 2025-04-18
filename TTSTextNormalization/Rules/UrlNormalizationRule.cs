using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.Rules;

/// <summary>
/// Normalizes URLs found in text by replacing them with a placeholder,
/// using Regex for initial detection and Uri.TryCreate for validation.
/// The placeholder is configurable via <see cref="UrlRuleOptions"/>.
/// </summary>
public sealed partial class UrlNormalizationRule : ITextNormalizationRule
{
    /// <inheritdoc/>
    /// <remarks>
    /// Runs after most content normalization but before final whitespace cleanup. Order: 20.
    /// </remarks>
    public int Order => 20;

    private const int RegexTimeoutMilliseconds = 200;
    private readonly string _placeholder; // Store the configured placeholder

    // Cache allowed schemes for performance
    private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        Uri.UriSchemeHttp,
        Uri.UriSchemeHttps
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlNormalizationRule"/> class.
    /// </summary>
    /// <param name="optionsAccessor">The configuration options.</param>
    /// <exception cref="ArgumentNullException">Thrown if optionsAccessor is null.</exception>
    public UrlNormalizationRule(IOptions<UrlRuleOptions> optionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        UrlRuleOptions options = optionsAccessor.Value ?? new UrlRuleOptions();
        _placeholder = options.PlaceholderText; // Use configured placeholder
    }

    /// <inheritdoc/>
    public string Apply(string inputText)
    {
        ArgumentNullException.ThrowIfNull(inputText);

        if (string.IsNullOrEmpty(inputText))
        {
            return inputText;
        }

        string currentText = inputText;
        try
        {
            // Pass the instance method as the evaluator
            currentText = PotentialUrlRegex().Replace(currentText, UrlMatchEvaluator);
        }
        catch (RegexMatchTimeoutException ex)
        {
            Console.Error.WriteLine($"Regex timeout during URL normalization: {ex.Message}");
        }
        catch (Exception ex) // Catch potential errors in Uri.TryCreate or evaluator
        {
            Console.Error.WriteLine($"Error during URL normalization: {ex.Message}");
            return inputText; // Fallback on error
        }

        return currentText;
    }

    /// <summary>
    /// Evaluates a potential URL match using Uri.TryCreate for validation.
    /// Uses the configured placeholder upon successful validation.
    /// </summary>
    private string UrlMatchEvaluator(Match match)
    {
        string potentialUrl = match.Value;

        // Prepend "http://" to www. URLs for Uri.TryCreate, as it often requires a scheme.
        string uriStringToValidate = potentialUrl.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? $"http://{potentialUrl}"
            : potentialUrl;

        // Validate using Uri.TryCreate
        if (Uri.TryCreate(uriStringToValidate, UriKind.Absolute, out Uri? uriResult)
            && AllowedSchemes.Contains(uriResult.Scheme))
        {
            // It's a valid HTTP/HTTPS URI, replace it with the configured placeholder.
            return _placeholder;
        }
        else
        {
            // Not a valid/allowed URI, return the original text.
            return potentialUrl;
        }
    }

    /// <summary>
    /// Regex to find potential URLs starting with http(s):// or www.
    /// Includes basic structural checks and boundary lookarounds.
    /// It's designed to be slightly broader than strictly necessary,
    /// relying on Uri.TryCreate in the evaluator for final validation.
    /// </summary>
    [GeneratedRegex(
        // Lookbehind: Not preceded by letter, number, or @
        @"(?<![\p{L}\p{N}@])" +
        // Main structure: scheme or www.
        @"(?:" +
            @"https?://" + // Scheme
            @"|" +
            @"www\." +     // OR www.
        @")" +
        // Host/Path part: Needs at least one non-delimiter char after scheme/www.
        // Matches common URL characters greedily but stops before whitespace/brackets etc.
        // It must end with a letter/number/slash to be plausible.
        @"[^\s<>\""()]+" + // Match one or more non-space/bracket chars
        @"(?<=[\p{L}\p{N}/])" + // Lookbehind: Ensure the last matched char is plausible end
                                // Lookahead: Must be followed by a boundary
        @"(?=[\s<>\""().,!?;:]|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex PotentialUrlRegex();
}