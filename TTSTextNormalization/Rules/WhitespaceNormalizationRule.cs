﻿using System.Text.RegularExpressions;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.Rules;

/// <summary>
/// Normalizes whitespace: trims ends, collapses internal spaces, and adjusts spacing around common punctuation.
/// Runs late in the pipeline.
/// </summary>
public sealed partial class WhitespaceNormalizationRule : ITextNormalizationRule
{
    /// <inheritdoc/>
    public int Order => 9000;
    private const int RegexTimeoutMilliseconds = 100;

    /// <inheritdoc/>
    public WhitespaceNormalizationRule() { }

    /// <inheritdoc/>
    public string Apply(string inputText)
    {
        ArgumentNullException.ThrowIfNull(inputText);

        // 1. Trim leading and trailing whitespace
        string currentText = inputText.Trim();
        if (string.IsNullOrEmpty(currentText))
        {
            return currentText;
        }

        try
        {
            // 2. Replace multiple internal whitespace characters with a single space
            currentText = MultipleWhitespaceRegex().Replace(currentText, " ");

            // 3. Remove space BEFORE common punctuation [.,!?;:]
            // Looks for one or more spaces followed by one of the punctuation chars in the class.
            // Replaces with just the punctuation character (group 1).
            currentText = SpaceBeforePunctuationRegex().Replace(currentText, "$1");

            // 4. Ensure single space AFTER common punctuation [.,!?;:]
            // Looks for one of the punctuation chars NOT followed by whitespace or end-of-string.
            // Replaces with the punctuation char (group 1) followed by a space.
            currentText = SpaceAfterPunctuationRegex().Replace(currentText, "$1 ");
        }
        catch (RegexMatchTimeoutException ex)
        {
            Console.Error.WriteLine($"Regex timeout during whitespace normalization step: {ex.Message}");
            // Depending on which step timed out, currentText might be partially processed.
            // Returning it is usually better than returning the original input.
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during whitespace normalization step: {ex.Message}");
        }

        return currentText;
    }

    /// <summary>
    /// Regex for Step 2: Collapse multiple whitespace. Uses NonBacktracking.
    /// </summary>
    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled | RegexOptions.NonBacktracking, RegexTimeoutMilliseconds)]
    private static partial Regex MultipleWhitespaceRegex();

    /// <summary>
    /// Regex for Step 3: Remove space before punctuation. Uses NonBacktracking.
    /// \s+ : one or more whitespace chars
    /// ([.,!?;:]) : Captures one of the punctuation marks into group 1
    /// </summary>
    [GeneratedRegex(@"\s+([.,!?;:])", RegexOptions.Compiled | RegexOptions.NonBacktracking, RegexTimeoutMilliseconds)]
    private static partial Regex SpaceBeforePunctuationRegex();

    /// <summary>
    /// Regex for Step 4: Ensure space after punctuation. Cannot use NonBacktracking due to lookahead.
    /// ([.,!?;:]) : Captures one of the punctuation marks into group 1
    /// (?!\s|$)   : Negative lookahead - asserts that the char is NOT followed by whitespace OR end of string
    /// </summary>
    [GeneratedRegex(@"([.,!?;:])(?!\s|$)", RegexOptions.Compiled, RegexTimeoutMilliseconds)]
    private static partial Regex SpaceAfterPunctuationRegex();
}