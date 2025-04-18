using Humanizer;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.Rules;

/// <summary>
/// Normalizes standalone numbers, including cardinals, ordinals, decimals, and multi-dot sequences (like version numbers).
/// Uses Humanizer for cardinal and ordinal word conversion.
/// </summary>
public sealed partial class NumberNormalizationRule : ITextNormalizationRule
{
    /// <inheritdoc/>
    public int Order => 400;

    private const int RegexTimeoutMilliseconds = 150;

    // Keep DigitWords for the new multi-dot logic
    private static readonly string[] DigitWords = ["zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine"];

    /// <summary>
    /// Initializes a new instance of the <see cref="NumberNormalizationRule"/> class.
    /// </summary>
    public NumberNormalizationRule() { }

    /// <inheritdoc/>
    public string Apply(string inputText)
    {
        ArgumentNullException.ThrowIfNull(inputText);
        if (string.IsNullOrEmpty(inputText))
            return inputText;

        string currentText = inputText;
        try
        {
            // --- Pass 1: Handle Ordinals ---
            currentText = OrdinalNumberRegex().Replace(currentText, OrdinalMatchEvaluator);

            // --- Pass 2: Handle Multi-Dot Version Numbers ---
            currentText = MultiDotNumberRegex().Replace(currentText, MultiDotNumberMatchEvaluator);

            // --- Pass 3: Handle Cardinals and Decimals ---
            currentText = CardinalDecimalNumberRegex().Replace(currentText, CardinalDecimalMatchEvaluator);
        }
        catch (RegexMatchTimeoutException ex) { Console.Error.WriteLine($"Regex timeout during number normalization: {ex.Message}"); }
        catch (Exception ex) { Console.Error.WriteLine($"Error during number normalization: {ex.Message}"); }

        return currentText;
    }

    // --- Evaluator for Ordinals (Unchanged) ---
    private static string OrdinalMatchEvaluator(Match match)
    {
        string numberStr = match.Groups["number"].Value;
        if (int.TryParse(numberStr, NumberStyles.None, CultureInfo.InvariantCulture, out int numberValue))
        {
            try
            { return $" {numberValue.ToOrdinalWords()} "; } // Keep default Humanizer culture for consistency
            catch (Exception ex) { Console.Error.WriteLine($"Humanizer.ToOrdinalWords failed: {ex.Message}"); }
        }

        return match.Value;
    }

    // --- Evaluator for Multi-Dot Numbers (NEW) ---
    private static string MultiDotNumberMatchEvaluator(Match match)
    {
        StringBuilder builder = new();
        string numberSequence = match.Value;

        for (int i = 0; i < numberSequence.Length; i++)
        {
            char c = numberSequence[i];
            if (char.IsDigit(c))
            {
                // Convert digit character to word
                int digitValue = c - '0'; // Fast char to int conversion for digits
                builder.Append(DigitWords[digitValue]);
            }
            else if (c == '.')
            {
                builder.Append(" point"); // Append " point" for dots
            }
            else
            {
                // Should not happen based on Regex, but as fallback keep the char
                builder.Append(c);
            }

            // Add space after each part (digit word or "point") unless it's the last char
            if (i < numberSequence.Length - 1)
            {
                builder.Append(' ');
            }
        }
        // Pad the whole result
        return $" {builder} ";
    }


    // --- Evaluator for Cardinals/Decimals (Unchanged) ---
    private static string CardinalDecimalMatchEvaluator(Match match)
    {
        string integerPartStr = match.Groups["integer"].Value;
        string fractionPartStr = match.Groups["fraction"].Success ? match.Groups["fraction"].Value : string.Empty;

        if (long.TryParse(integerPartStr, NumberStyles.None, CultureInfo.InvariantCulture, out long integerValue))
        {
            try
            {
                string integerWords = integerValue.ToWords(); // Keep default Humanizer culture
                if (string.IsNullOrEmpty(fractionPartStr))
                {
                    return $" {integerWords} ";
                }
                else
                {
                    StringBuilder builder = new();
                    builder.Append(integerWords).Append(" point");
                    foreach (char digitChar in fractionPartStr)
                    {
                        if (int.TryParse(digitChar.ToString(), out int digitValue) && digitValue >= 0 && digitValue <= 9)
                        { builder.Append(' ').Append(DigitWords[digitValue]); }
                        else
                        { return match.Value; }
                    }

                    return $" {builder} ";
                }
            }
            catch (Exception ex) { Console.Error.WriteLine($"Humanizer.ToWords failed: {ex.Message}"); }
        }

        return match.Value;
    }


    // --- Regex Definitions ---
    [GeneratedRegex(@"(?<![\p{L}\p{N}-])(?<number>\d+)(st|nd|rd|th)(?![\p{L}\p{N}-])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeoutMilliseconds)]
    private static partial Regex OrdinalNumberRegex();


    [GeneratedRegex(
        // Matches a number starting at a boundary, followed by at least TWO dot-number groups
        @"(?<![\p{L}\p{N}-])(?<number>\d+(?:\.\d+){2,})(?![\p{L}\p{N}-])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex MultiDotNumberRegex();

    // Cardinal/Decimal Regex (Must  NOT match multi-dot sequences)
    // We rely on the order of operations: MultiDot runs first, so this one only gets
    // simpler integers or single-dot decimals.
    [GeneratedRegex(@"(?<![\p{L}\p{N}-])(?<integer>\d+)(?:\.(?<fraction>\d+))?(?![\p{L}\p{N}-])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeoutMilliseconds)]
    private static partial Regex CardinalDecimalNumberRegex();
}