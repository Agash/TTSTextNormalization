using System.Collections.Frozen;
using System.Text.RegularExpressions;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.Rules;

/// <summary>
/// Normalizes common chat/gaming/streaming abbreviations and acronyms.
/// </summary>
public sealed partial class AbbreviationNormalizationRule : ITextNormalizationRule
{
    public int Order => 300;
    private const int RegexTimeoutMilliseconds = 150; // Slightly increased for larger pattern

    // Expanded map with common Twitch/YouTube/Gaming/General abbreviations
    private static readonly FrozenDictionary<string, string> AbbreviationMap = new Dictionary<
        string,
        string
    >(StringComparer.OrdinalIgnoreCase)
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
        // Gaming/Streaming Specific
        { "gg", "good game" },
        { "ggwp", "good game well played" },
        { "glhf", "good luck have fun" },
        { "afk", "away from keyboard" },
        { "ez", "easy" },
        { "noob", "newbie" },
        // { "op", "overpowered" }, // conflicts with normal usage..?
        { "kekw", "kek double u" },
        { "omegalul", "omega lol" },
        { "w", "dub" },
        { "l", "el" },
        { "dc", "d c" }, // Spell out
        { "ig", "in game" },
        // YouTube Specific (less common as single abbr.)
        { "yt", "youtube" },
        // Technical/Setup
        { "os", "o s" }, // Spell out
        { "cpu", "c p u" }, // Spell out
        { "gpu", "g p u" }, // Spell out
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public AbbreviationNormalizationRule() { }

    public string Apply(string inputText)
    {
        ArgumentNullException.ThrowIfNull(inputText);
        if (string.IsNullOrEmpty(inputText) || AbbreviationMap.Count == 0)
            return inputText;

        string currentText = inputText;
        try
        {
            currentText = AbbreviationRegex().Replace(currentText, AbbreviationMatchEvaluator);
        }
        catch (RegexMatchTimeoutException ex)
        {
            Console.Error.WriteLine(
                $"Regex timeout during abbreviation normalization: {ex.Message}"
            );
        }

        return currentText;
    }

    private static string AbbreviationMatchEvaluator(Match match)
    {
        if (AbbreviationMap.TryGetValue(match.Value, out string? expansion))
        {
            return $" {expansion} ";
        }

        return match.Value; // Fallback
    }

    /// <summary>
    /// Source-Generated Regex - Uses lookarounds including hyphen exclusion.
    /// </summary>
    [GeneratedRegex(
        // FIX: Exclude hyphen '-' from lookarounds as well
        // (?<![\p{L}\p{N}-]) : Not preceded by a Letter, Number, or Hyphen
        // (...)              : Match the abbreviation key list
        // (?![\p{L}\p{N}-])  : Not followed by a Letter, Number, or Hyphen
        @"(?<![\p{L}\p{N}-])(lol|rofl|brb|bbl|bbs|omg|omw|btw|fyi|ttyl|tyt|idk|idc|imo|imho|thx|ty|tyvm|np|yw|pls|plz|sry|afaik|smh|tbh|fomo|gtg|g2g|irl|ig|rn|fr|gg|ggwp|glhf|afk|ez|noob|op|nerf|buff|pog|poggers|kappa|kekw|omegalul|w|l|mic|cam|sub|emote|dc|yt|os|cpu|gpu|ram)(?![\p{L}\p{N}-])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex AbbreviationRegex();
}
