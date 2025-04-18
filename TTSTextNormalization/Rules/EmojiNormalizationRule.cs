using Microsoft.Extensions.Options;
using System.Text;
using System.Text.RegularExpressions;
using TTSTextNormalization.Abstractions;
using TTSTextNormalization.EmojiDataGenerated;

namespace TTSTextNormalization.Rules;

/// <summary>
/// Normalizes standard Unicode emojis into their textual descriptions
/// using data generated from an emoji JSON source at compile time.
/// Allows optional prefix and suffix via <see cref="EmojiRuleOptions"/>.
/// </summary>
public sealed class EmojiNormalizationRule : ITextNormalizationRule
{
    /// <inheritdoc/>
    public int Order => 100;

    private readonly EmojiRuleOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmojiNormalizationRule"/> class.
    /// </summary>
    /// <param name="optionsAccessor">The configuration options.</param>
    /// <exception cref="ArgumentNullException">Thrown if optionsAccessor is null.</exception>
    public EmojiNormalizationRule(IOptions<EmojiRuleOptions> optionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        _options = optionsAccessor.Value ?? new EmojiRuleOptions();
    }

    /// <summary>
    /// Replaces known emojis in the input text using the source-generated Regex and map,
    /// applying configured prefix and suffix.
    /// </summary>
    public string Apply(string inputText)
    {
        ArgumentNullException.ThrowIfNull(inputText);
        // Check if the map or regex were successfully generated
        if (string.IsNullOrEmpty(inputText) || EmojiData.EmojiToNameMap.Count == 0)
            return inputText;

        try
        {
            // Pass the instance method as the evaluator
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
    /// MatchEvaluator to look up the found emoji in the generated map
    /// and apply configured prefix/suffix.
    /// </summary>
    private string EmojiMatchEvaluator(Match match)
    {
        // The Regex ensures we only match keys present in the map.
        if (EmojiData.EmojiToNameMap.TryGetValue(match.Value, out string? name))
        {
            // Use StringBuilder for efficient concatenation
            StringBuilder builder = new();
            builder.Append(' '); // Leading space always

            if (!string.IsNullOrEmpty(_options.Prefix))
            {
                builder.Append(_options.Prefix);
                // Ensure space after prefix if prefix itself doesn't end with one
                if (!_options.Prefix.EndsWith(' '))
                    builder.Append(' ');
            }

            builder.Append(name);

            if (!string.IsNullOrEmpty(_options.Suffix))
            {
                // Ensure space before suffix if suffix itself doesn't start with one
                if (!_options.Suffix.StartsWith(' '))
                    builder.Append(' ');
                builder.Append(_options.Suffix);
            }

            builder.Append(' '); // Trailing space always

            return builder.ToString();
        }
        else
        {
            // Should not happen if Regex and Map are generated correctly.
            // Fallback: return original emoji.
            return match.Value;
        }
    }
}