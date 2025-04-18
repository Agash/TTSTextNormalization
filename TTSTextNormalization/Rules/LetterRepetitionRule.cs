using System.Text.RegularExpressions;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.Rules;

/// <summary>
/// Reduces excessive repetitions of the same letter within words (e.g., "soooo" -> "soo").
/// </summary>
public sealed partial class LetterRepetitionRule : ITextNormalizationRule
{
    /// <inheritdoc/>
    public int Order => 510;
    private const int RegexTimeoutMilliseconds = 150; // Might need slightly more time for complex strings

    /// <inheritdoc/>
    public LetterRepetitionRule() { }

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
            // Replace sequences of 3 or more identical letters with just two letters.
            // This might need multiple passes if the replacement itself creates a new sequence,
            // but a single pass handles most common cases like "soooo".
            // For perfect handling, a loop could be used:
            // string previousText;
            // do {
            //    previousText = currentText;
            //    currentText = LetterRepetitionRegex().Replace(previousText, "$1$1");
            // } while (currentText != previousText);
            // Let's stick to a single pass for now for simplicity and performance.
            currentText = LetterRepetitionRegex().Replace(currentText, "$1$1");
        }
        catch (RegexMatchTimeoutException ex)
        {
            Console.Error.WriteLine($"Regex timeout during letter repetition normalization: {ex.Message}");
            // Return text processed so far
        }

        return currentText;
    }

    /// <summary>
    /// Regex to find sequences of 3 or more identical letters (case-insensitive).
    /// ([a-zA-Z]): Captures a single letter (case-insensitive due to options) into group 1.
    /// \1{2,}: Matches two or more occurrences of the exact letter captured in group 1.
    /// </summary>
    [GeneratedRegex(
        @"([a-zA-Z])\1{2,}", // Match a letter followed by itself 2 or more times
        RegexOptions.Compiled | RegexOptions.IgnoreCase, // Case-insensitive matching
        matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex LetterRepetitionRegex();
}