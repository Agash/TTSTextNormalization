using Humanizer;
using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.Rules;

public sealed partial class CurrencyNormalizationRule : ITextNormalizationRule
{
    public int Order => 200;
    private const int RegexTimeoutMilliseconds = 150;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(
        RegexTimeoutMilliseconds
    );

    private readonly record struct CurrencyTTSInfo(
        string Singular,
        string Plural,
        string FractionSingular,
        string FractionPlural
    );

    private static readonly FrozenDictionary<string, CurrencyTTSInfo> IsoCodeToTTSInfoMap;
    private static readonly FrozenDictionary<string, string> SymbolOrCodeToIsoCodeMap;
    private static readonly Regex CombinedCurrencyRegex;
    private static readonly bool IsInitialized;

    static CurrencyNormalizationRule()
    {
        try
        {
            // Define the Manual ISO -> TTS Mapping
            Dictionary<string, CurrencyTTSInfo> ttsMapBuilder = new(
                StringComparer.OrdinalIgnoreCase
            )
            {
                // Add more common currencies...
                { "USD", new("dollar", "dollars", "cent", "cents") },
                { "CAD", new("Canadian dollar", "Canadian dollars", "cent", "cents") },
                { "AUD", new("Australian dollar", "Australian dollars", "cent", "cents") },
                { "GBP", new("pound", "pounds", "penny", "pence") }, // Using "pound" for GBP
                { "EUR", new("euro", "euros", "cent", "cents") },
                { "JPY", new("yen", "yen", "sen", "sen") },
                { "INR", new("rupee", "rupees", "paisa", "paise") },
                { "BRL", new("real", "reais", "centavo", "centavos") },
                { "CNY", new("yuan", "yuan", "fen", "fen") },
                { "RUB", new("ruble", "rubles", "kopek", "kopeks") },
            };
            IsoCodeToTTSInfoMap = ttsMapBuilder.ToFrozenDictionary(
                StringComparer.OrdinalIgnoreCase
            );

            // Build Symbol/Code -> ISO Code Mapping
            Dictionary<string, string> symbolMapBuilder = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> uniqueSymbols = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> uniqueIsoCodes = new(StringComparer.OrdinalIgnoreCase);

            foreach (
                CultureInfo ci in CultureInfo.GetCultures(
                    CultureTypes.SpecificCultures | CultureTypes.InstalledWin32Cultures
                )
            ) // Broader search
            {
                // Skip problematic cultures
                if (
                    ci.IsNeutralCulture
                    || ci.LCID == CultureInfo.InvariantCulture.LCID
                    || ci.Name == "" /* Invariant */
                    || ci.Name.StartsWith("x-", StringComparison.Ordinal)
                )
                {
                    continue;
                }

                RegionInfo? region = null;
                try
                {
                    region = new RegionInfo(ci.Name);
                }
                catch (ArgumentException)
                {
                    continue; /* Cannot create RegionInfo */
                }

                string isoCode = region.ISOCurrencySymbol;
                string symbol = region.CurrencySymbol;

                // Only add if we have TTS info for this ISO code
                if (!string.IsNullOrEmpty(isoCode) && IsoCodeToTTSInfoMap.ContainsKey(isoCode))
                {
                    if (symbolMapBuilder.TryAdd(isoCode, isoCode))
                        uniqueIsoCodes.Add(isoCode);
                    // FIX: Prioritize JPY for ¥ symbol if not already mapped
                    if (symbol == "¥" && !symbolMapBuilder.ContainsKey("¥"))
                    {
                        symbolMapBuilder.Add("¥", "JPY");
                        uniqueSymbols.Add("¥");
                    }
                    else if (
                        !string.IsNullOrEmpty(symbol)
                        && symbol != "¥"
                        && !symbolMapBuilder.ContainsKey(symbol)
                        && !symbol.All(char.IsLetterOrDigit)
                    )
                    {
                        symbolMapBuilder.Add(symbol, isoCode);
                        uniqueSymbols.Add(symbol);
                    }
                }
            }

            if (IsoCodeToTTSInfoMap.ContainsKey("JPY"))
            {
                symbolMapBuilder["¥"] = "JPY";
                uniqueSymbols.Add("¥"); // Ensure it's in the symbol list for regex
            }

            SymbolOrCodeToIsoCodeMap = symbolMapBuilder.ToFrozenDictionary(
                StringComparer.OrdinalIgnoreCase
            );

            // Dynamically Generate the Regex
            IOrderedEnumerable<string> escapedSymbols = uniqueSymbols
                .Select(Regex.Escape)
                .OrderByDescending(s => s.Length);
            IOrderedEnumerable<string> escapedIsoCodes = uniqueIsoCodes
                .Select(Regex.Escape)
                .OrderByDescending(s => s.Length);

            string symbolPatternPart = string.Join("|", escapedSymbols);
            string codePatternPart = string.Join("|", escapedIsoCodes);

            // Number pattern allowing flexible separators but requiring at least one digit
            string numberPatternPart =
                @"(?<integer>\d{1,3}(?:[,\s'.]\d{3})*|\d+)(?:[.,](?<fraction>\d{1,2}))?";

            string pattern1 = !string.IsNullOrEmpty(symbolPatternPart)
                ? $@"(?<![\p{{L}}\p{{N}}])(?<symbol>{symbolPatternPart})\s?{numberPatternPart}(?![\p{{L}}\p{{N}}])"
                : string.Empty;
            string pattern2 = !string.IsNullOrEmpty(codePatternPart)
                ? $@"(?<![\p{{L}}\p{{N}}]){numberPatternPart}\s?(?<symbol>{codePatternPart})(?![\p{{L}}\p{{N}}])"
                : string.Empty;

            string combinedPattern = !string.IsNullOrEmpty(pattern1) && !string.IsNullOrEmpty(pattern2)
                ? $"({pattern1})|({pattern2})"
                : !string.IsNullOrEmpty(pattern1) ? pattern1 : pattern2;

            if (!string.IsNullOrEmpty(combinedPattern))
            {
                CombinedCurrencyRegex = new Regex(
                    combinedPattern,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase,
                    RegexTimeout
                );
                IsInitialized = true;
                Console.WriteLine($"INFO: Currency Regex Initialized: {CombinedCurrencyRegex}");
            }
            else
            {
                CombinedCurrencyRegex = new Regex("(?!)", RegexOptions.Compiled); // Never matches
                IsInitialized = false;
                Console.Error.WriteLine("Warning: No valid currency patterns generated.");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FATAL: Currency Rule static constructor failed: {ex}");
            CombinedCurrencyRegex = new Regex("(?!)", RegexOptions.Compiled);
            IsInitialized = false;
        }
    }

    public CurrencyNormalizationRule() { }

    public string Apply(string inputText)
    {
        ArgumentNullException.ThrowIfNull(inputText);
        if (!IsInitialized || string.IsNullOrEmpty(inputText))
            return inputText;

        string currentText = inputText;
        try
        {
            currentText = CombinedCurrencyRegex.Replace(currentText, CurrencyMatchEvaluator);
        }
        catch (RegexMatchTimeoutException ex)
        {
            Console.Error.WriteLine($"Regex timeout during currency normalization: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during currency normalization: {ex.Message}");
        }

        return currentText;
    }

    private static string CurrencyMatchEvaluator(Match match)
    {
        // Prioritize symbol group if it exists and matched (pattern 1 or 2 specific capture)
        // This requires naming the outer groups in the combined pattern. Let's adjust:
        // combinedPattern = $"(?<p1>{pattern1})|(?<p2>{pattern2})";
        // But for simplicity now, rely on the 'symbol' group captured by either.
        string detectedSymbolOrCode = match.Groups["symbol"].Value;
        string integerPartStr = match.Groups["integer"].Value;
        string fractionPartStr = match.Groups["fraction"].Success
            ? match.Groups["fraction"].Value
            : string.Empty;

        if (!SymbolOrCodeToIsoCodeMap.TryGetValue(detectedSymbolOrCode, out string? isoCode))
            return match.Value;
        if (!IsoCodeToTTSInfoMap.TryGetValue(isoCode, out CurrencyTTSInfo currencyTTSInfo))
            return match.Value;

        string integerForParsing = Regex.Replace(integerPartStr, "[,' .]", ""); // Remove common separators
        if (
            !long.TryParse(
                integerForParsing,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long integerValue
            )
        )
        {
            return match.Value;
        }

        int fractionValue = 0;
        if (!string.IsNullOrEmpty(fractionPartStr))
        {
            string paddedFraction = fractionPartStr.PadRight(2, '0');
            if (
                !int.TryParse(
                    paddedFraction,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out fractionValue
                )
                || fractionValue < 0
                || fractionValue > 99
            )
            {
                return match.Value;
            }
        }

        try
        {
            string integerWords = integerValue.ToWords();
            string? fractionWords = fractionValue > 0 ? fractionValue.ToWords() : null;

            StringBuilder builder = new();
            builder.Append(integerWords);
            builder.Append(' ');
            builder.Append(integerValue == 1 ? currencyTTSInfo.Singular : currencyTTSInfo.Plural);

            if (fractionWords != null && fractionValue > 0) // Ensure fraction > 0
            {
                builder.Append(' ');
                builder.Append(fractionWords);
                builder.Append(' ');
                builder.Append(
                    fractionValue == 1
                        ? currencyTTSInfo.FractionSingular
                        : currencyTTSInfo.FractionPlural
                );
            }

            return $" {builder} ";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Humanizer failed for '{match.Value}': {ex.Message}");
            return match.Value;
        }
    }
}
