using System.Collections.Frozen;
using System.Text;
using System.Text.RegularExpressions;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.Rules;

/// <summary>
/// Performs basic text sanitization, running early in the pipeline.
/// Normalizes line breaks, removes problematic control characters,
/// and replaces common non-ASCII punctuation with ASCII equivalents.
/// </summary>
public sealed partial class BasicSanitizationRule : ITextNormalizationRule
{
    /// <summary>
    /// Runs very early (Order 10) before most other rules.
    /// </summary>
    public int Order => 10;

    private const int RegexTimeoutMilliseconds = 100;

    // Dictionary for replacing common "fancy" characters with simpler ASCII versions.
    private static readonly FrozenDictionary<string, string> FancyCharMap = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "“", "\"" }, { "”", "\"" }, // Double quotes
        { "‘", "'" }, { "’", "'" }, // Single quotes/apostrophes
        { "«", "\"" }, { "»", "\"" }, // Guillemets to double quotes
        { "‹", "'" }, { "›", "'" }, // // FIX: Added Single Guillemets
        { "…", "..." }, // Ellipsis
        { "—", "-" }, // Em dash
        { "–", "-" }, // En dash            
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicSanitizationRule"/> class.
    /// </summary>
    public BasicSanitizationRule() { }

    /// <summary>
    /// Applies the sanitization steps.
    /// </summary>
    /// <param name="inputText">The raw input text.</param>
    /// <returns>The sanitized text.</returns>
    public string Apply(string inputText)
    {
        ArgumentNullException.ThrowIfNull(inputText);
        if (string.IsNullOrEmpty(inputText))
        {
            return inputText;
        }

        string currentText = inputText;

        // 1. Normalize line breaks (CRLF -> LF, CR -> LF)
        // Simple chained Replace is usually efficient enough for this.
        currentText = currentText.Replace("\r\n", "\n").Replace("\r", "\n");

        // 2. Remove problematic control and format characters using Regex
        try
        {
            currentText = RemoveControlCharsRegex().Replace(currentText, string.Empty);
        }
        catch (RegexMatchTimeoutException ex)
        {
            Console.Error.WriteLine($"Regex timeout during control char sanitization: {ex.Message}");
            // Continue with the text processed so far if timeout occurs
        }

        // 3. Replace fancy characters using the map.
        // Using StringBuilder can be more efficient than chained string.Replace
        // if the text is long and many replacements might occur.
        if (MightContainFancyChars(currentText)) // Simple heuristic check
        {
            StringBuilder builder = new(currentText);
            foreach ((string fancy, string ascii) in FancyCharMap)
            {
                builder.Replace(fancy, ascii);
            }

            currentText = builder.ToString();
        }
        // Alternative using LINQ Aggregate + Replace (potentially less efficient due to allocations):
        // currentText = FancyCharMap.Aggregate(currentText, (current, pair) => current.Replace(pair.Key, pair.Value));


        // 4. Final check: ensure no null characters remain (belt-and-suspenders)
        // Although the regex should handle \u0000, an explicit check is cheap insurance.
        if (currentText.Contains('\u0000'))
        {
            currentText = currentText.Replace("\u0000", string.Empty);
        }

        return currentText;
    }

    /// <summary>
    /// A quick check to see if applying the FancyCharMap replacement is potentially needed.
    /// Avoids allocating StringBuilder unnecessarily if no fancy chars are likely present.
    /// This is a heuristic and might not be perfect.
    /// </summary>
    private static bool MightContainFancyChars(string text)
    {
        // Check for a few common fancy chars. Can be expanded.
        // This avoids iterating the dictionary if none are present.
        return text.Contains('“') || text.Contains('”') || text.Contains('‘') ||
               text.Contains('’') || text.Contains('…') || text.Contains('—') ||
               text.Contains('–') || text.Contains('«') || text.Contains('»') ||
               text.Contains('‹') || text.Contains('›');
    }


    /// <summary>
    /// Source-Generated Regex to find common problematic Unicode characters.
    /// Includes:
    /// - C0 controls except HT, LF (\u0009, \u000A)
    /// - C1 controls (often problematic)
    /// - Zero-width spaces/joiners/non-joiners (\u200B-\u200D)
    /// - Byte Order Mark (\uFEFF)
    /// - DELETE char (\u007F)
    /// Does NOT remove surrogates, private use, or unassigned by default, as that can be overly broad.
    /// </summary>
    [GeneratedRegex(
        @"[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F-\u009F\u200B-\u200D\uFEFF]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, // CultureInvariant is fine for code points
        matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex RemoveControlCharsRegex();
}