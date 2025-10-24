using System.Collections.Frozen;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.Rules;

/// <summary>
/// Normalizes common chat/gaming/streaming abbreviations and acronyms.
/// Configurable via <see cref="AbbreviationRuleOptions"/>.
/// </summary>
public sealed partial class AbbreviationNormalizationRule : ITextNormalizationRule
{
    /// <inheritdoc/>
    public int Order => 300;
    private const int RegexTimeoutMilliseconds = 150;

    private readonly FrozenDictionary<string, string> _effectiveAbbreviations;
    private readonly Regex _abbreviationRegex;

    // Default abbreviations
    private static readonly FrozenDictionary<string, string> DefaultAbbreviationMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // General Chat
            { "lol", "laughing out loud" },
            { "rofl", "rolling on the floor laughing" },
            { "brb", "be right back" },
            { "bbl", "be back later" },
            { "bbs", "be back soon" },
            { "omg", "oh my god" },
            { "omw", "on my way" },
            { "btw", "by the way" },
            { "fyi", "for your information" },
            { "ttyl", "talk to you later" },
            { "tyt", "take your time" },
            { "idk", "I don't know" },
            { "idc", "I don't care" },
            { "imo", "in my opinion" },
            { "imho", "in my humble opinion" },
            { "thx", "thanks" },
            { "ty", "thank you" },
            { "tyvm", "thank you very much" },
            { "np", "no problem" },
            { "yw", "you're welcome" },
            { "pls", "please" },
            { "plz", "please" },
            { "sry", "sorry" },
            { "afaik", "as far as I know" },
            { "smh", "shaking my head" },
            { "tbh", "to be honest" },
            { "fomo", "fear of missing out" },
            { "gtg", "got to go" },
            { "g2g", "got to go" },
            { "irl", "in real life" },
            { "rn", "right now" },
            { "fr", "for real" },
            { "cmon", "come on" },
            // Gaming/Streaming Specific
            { "gg", "good game" },
            { "ggwp", "good game well played" },
            { "glhf", "good luck have fun" },
            { "afk", "away from keyboard" },
            { "ez", "easy" },
            { "noob", "newbie" },
            // { "op", "overpowered" }, // Consider conflicts
            { "kekw", "kek double u" },
            { "omegalul", "omega lol" },
            { "w", "dub" },
            { "l", "el" },
            { "dc", "d c" },
            { "ig", "in game" },
            // YouTube Specific
            { "yt", "youtube" },
            // PokeTuber Specific
            { "twf", "then we fight" },
            // Technical/Setup
            { "os", "o s" },
            { "cpu", "c p u" },
            { "gpu", "g p u" },
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="AbbreviationNormalizationRule"/> class.
    /// </summary>
    /// <param name="optionsAccessor">The configuration options.</param>
    /// <exception cref="ArgumentNullException">Thrown if optionsAccessor is null.</exception>
    /// <exception cref="RegexCreationException">Thrown if the abbreviation regex cannot be created.</exception>
    public AbbreviationNormalizationRule(IOptions<AbbreviationRuleOptions> optionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        AbbreviationRuleOptions options = optionsAccessor.Value ?? new AbbreviationRuleOptions(); // Use default options if null

        if (options.ReplaceDefaultAbbreviations)
        {
            _effectiveAbbreviations =
                options.CustomAbbreviations ?? FrozenDictionary<string, string>.Empty;
        }
        else
        {
            // Merge dictionaries, custom taking precedence
            Dictionary<string, string> merged = new(
                DefaultAbbreviationMap,
                StringComparer.OrdinalIgnoreCase
            );
            if (options.CustomAbbreviations != null)
            {
                foreach (KeyValuePair<string, string> kvp in options.CustomAbbreviations)
                {
                    merged[kvp.Key] = kvp.Value; // Add or overwrite
                }
            }

            _effectiveAbbreviations = merged.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        }

        // Generate Regex dynamically based on the effective abbreviations
        _abbreviationRegex = BuildAbbreviationRegex(_effectiveAbbreviations.Keys);
    }

    /// <inheritdoc/>
    public string Apply(string inputText)
    {
        ArgumentNullException.ThrowIfNull(inputText);
        if (string.IsNullOrEmpty(inputText) || _effectiveAbbreviations.Count == 0)
            return inputText;

        string currentText = inputText;
        try
        {
            currentText = _abbreviationRegex.Replace(currentText, AbbreviationMatchEvaluator);
        }
        catch (RegexMatchTimeoutException ex)
        {
            Console.Error.WriteLine(
                $"Regex timeout during abbreviation normalization: {ex.Message}"
            );
        }

        return currentText;
    }

    private string AbbreviationMatchEvaluator(Match match)
    {
        // Use the instance field _effectiveAbbreviations
        if (_effectiveAbbreviations.TryGetValue(match.Value, out string? expansion))
        {
            return $" {expansion} ";
        }
        // Should not happen if regex is built correctly from keys, but fallback just in case
        return match.Value;
    }

    /// <summary>
    /// Builds the abbreviation regex dynamically from the effective keys.
    /// </summary>
    /// <param name="abbreviations">The keys (abbreviations) to include in the pattern.</param>
    /// <returns>A compiled Regex.</returns>
    /// <exception cref="RegexCreationException">If regex creation fails.</exception>
    private static Regex BuildAbbreviationRegex(IEnumerable<string> abbreviations)
    {
        if (!abbreviations.Any())
        {
            // Return a regex that never matches if there are no abbreviations
            return Never();
        }

        // Sort keys by length descending to match longest first (e.g., "ggwp" before "gg")
        string patternPart = string.Join(
            "|",
            abbreviations.OrderByDescending(k => k.Length).Select(Regex.Escape)
        );
        string pattern = $@"(?<![\p{{L}}\p{{N}}-])({patternPart})(?![\p{{L}}\p{{N}}-])";

        try
        {
            return new Regex(
                pattern,
                RegexOptions.Compiled | RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(RegexTimeoutMilliseconds)
            );
        }
        catch (Exception ex)
        {
            // Wrap specific regex creation errors
            throw new RegexCreationException(
                $"Failed to create abbreviation regex: {ex.Message}",
                ex
            );
        }
    }

    /// <summary>
    /// Exception thrown when regex creation fails.
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="innerException">Inner exception</param>
    public sealed class RegexCreationException(string message, Exception innerException)
        : Exception(message, innerException) { }

    [GeneratedRegex("(?!)", RegexOptions.Compiled)]
    private static partial Regex Never();
}
