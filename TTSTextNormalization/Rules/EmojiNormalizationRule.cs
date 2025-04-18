using System.Text.RegularExpressions;
using TTSTextNormalization.Abstractions;
using TTSTextNormalization.EmojiDataGenerated;

namespace TTSTextNormalization.Rules;

/// <summary>
/// Normalizes standard Unicode emojis into their textual descriptions
/// using data generated from an emoji JSON source at compile time.
/// </summary>
public sealed class EmojiNormalizationRule : ITextNormalizationRule
{
    /// <inheritdoc/>
    public int Order => 100;

    /// <inheritdoc/>
    public EmojiNormalizationRule() { }

    /// <summary>
    /// Replaces known emojis in the input text using the source-generated Regex and map.
    /// </summary>
    public string Apply(string inputText)
    {
        ArgumentNullException.ThrowIfNull(inputText);
        // Check if the map or regex were successfully generated
        if (string.IsNullOrEmpty(inputText) || EmojiData.EmojiToNameMap.Count == 0)
            return inputText;

        try
        {
            return EmojiData.EmojiMatchRegex.Replace(inputText, EmojiMatchEvaluator);
        }
        catch (RegexMatchTimeoutException ex)
        {
            Console.Error.WriteLine($"Regex timeout during emoji normalization: {ex.Message}");
            return inputText;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during emoji normalization: {ex.Message}");
            return inputText;
        }
    }

    /// <summary>
    /// MatchEvaluator to look up the found emoji in the generated map.
    /// </summary>
    private static string EmojiMatchEvaluator(Match match)
    {
        // The Regex ensures we only match keys present in the map.
        if (EmojiData.EmojiToNameMap.TryGetValue(match.Value, out string? name))
        {
            // Pad with spaces for TTS separation. Use the 'name' from the JSON.
            return $" {name} ";
        }
        else
        {
            // Should not happen if Regex and Map are generated correctly.
            // Fallback: return original emoji (or empty string to remove). Let's keep it.
            return match.Value;
        }
    }
}