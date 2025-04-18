using System.Text.RegularExpressions;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.Rules;

/// <summary>
/// Reduces sequences of common punctuation marks (., !, ?) to a single instance.
/// </summary>
public sealed partial class ExcessivePunctuationRule : ITextNormalizationRule
{
    /// <inheritdoc/>
    public int Order => 500;
    private const int RegexTimeoutMilliseconds = 100;

    /// <inheritdoc/>
    public ExcessivePunctuationRule() { }

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
            // Replace sequences of 2 or more identical punctuation marks with a single one.
            currentText = ExcessivePunctuationRegex().Replace(currentText, "$1");
        }
        catch (RegexMatchTimeoutException ex)
        {
            Console.Error.WriteLine(
                $"Regex timeout during excessive punctuation normalization: {ex.Message}"
            );
        }

        return currentText;
    }

    /// <summary>
    /// Regex to find sequences of 2 or more identical punctuation characters from the set [!?.].
    /// ([!?.]): Captures a single !, ?, or . into group 1.
    /// \1{1,}: Matches one or more occurrences of the exact character captured in group 1.
    /// </summary>
    [GeneratedRegex(
        @"([!?.])\1{1,}", // Match !, ?, or . followed by itself one or more times
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: RegexTimeoutMilliseconds
    )]
    private static partial Regex ExcessivePunctuationRegex();
}
