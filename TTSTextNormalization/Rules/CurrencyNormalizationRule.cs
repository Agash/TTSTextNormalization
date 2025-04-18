using Humanizer;
using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.Rules;

/// <summary>
/// Normalizes currency amounts based on symbols and ISO codes using a multi-pass approach.
/// Handles patterns like $10, 10 USD, $10 USD, £5.50, 100 EUR, €100 EUR.
/// Uses Humanizer for number-to-words conversion.
/// </summary>
public sealed partial class CurrencyNormalizationRule : ITextNormalizationRule
{
    /// <inheritdoc/>
    public int Order => 200;
    private const int RegexTimeoutMilliseconds = 150; // Timeout per regex operation
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(
        RegexTimeoutMilliseconds
    );

    // Structure to hold TTS specific names for a currency
    private readonly record struct CurrencyTTSInfo(
        string Singular,
        string Plural,
        string FractionSingular,
        string FractionPlural
    );

    // Maps ISO Code (e.g., "USD") to its spoken form info
    private static readonly FrozenDictionary<string, CurrencyTTSInfo> IsoCodeToTTSInfoMap;

    // Maps Symbol (e.g., "$") or Code (e.g., "USD") to its most likely ISO Code
    private static readonly FrozenDictionary<string, string> SymbolOrCodeToIsoCodeMap;

    // Regex definitions (will be populated in static constructor)
    private static readonly Regex? SymbolNumberCodeRegexInstance;
    private static readonly Regex? SymbolNumberRegexInstance;
    private static readonly Regex? NumberCodeRegexInstance;

    // Flag indicating successful initialization
    private static readonly bool IsInitialized;

    // Shared number pattern part used in regexes
    private const string NumberPatternPart =
        @"(?<integer>\d{1,3}(?:[,\s'.]\d{3})*|\d+)(?:[.,](?<fraction>\d{1,2}))?";

    static CurrencyNormalizationRule()
    {
        try
        {
            // --- TTS Map Population ---
            Dictionary<string, CurrencyTTSInfo> ttsMapBuilder = new(
                StringComparer.OrdinalIgnoreCase
            )
            {
                // === Africa ===
                { "DZD", new("Algerian dinar", "Algerian dinars", "santeem", "santeems") },
                { "BIF", new("Burundian franc", "Burundian francs", "centime", "centimes") },
                { "EGP", new("Egyptian pound", "Egyptian pounds", "piastre", "piastres") },
                { "ETB", new("Ethiopian birr", "Ethiopian birrs", "santim", "santim") },
                { "GHS", new("Ghanaian cedi", "Ghanaian cedis", "pesewa", "pesewas") },
                { "KES", new("Kenyan shilling", "Kenyan shillings", "cent", "cents") },
                { "MAD", new("Moroccan dirham", "Moroccan dirhams", "centime", "centimes") },
                { "MUR", new("Mauritian rupee", "Mauritian rupees", "cent", "cents") },
                { "NGN", new("Nigerian naira", "Nigerian naira", "kobo", "kobo") },
                { "TND", new("Tunisian dinar", "Tunisian dinars", "millime", "millimes") },
                { "TZS", new("Tanzanian shilling", "Tanzanian shillings", "cent", "cents") },
                { "UGX", new("Ugandan shilling", "Ugandan shillings", "cent", "cents") },
                {
                    "XOF",
                    new("West African CFA franc", "West African CFA francs", "centime", "centimes")
                },
                { "ZAR", new("South African rand", "South African rand", "cent", "cents") },
                // === Asia ===
                { "AFN", new("Afghan afghani", "Afghan afghanis", "pul", "puls") },
                { "AMD", new("Armenian dram", "Armenian drams", "luma", "luma") },
                { "AZN", new("Azerbaijani manat", "Azerbaijani manats", "qəpik", "qəpiks") },
                { "BDT", new("Bangladeshi taka", "Bangladeshi taka", "poisha", "poisha") },
                { "BND", new("Brunei dollar", "Brunei dollars", "sen", "sen") },
                { "CNY", new("Chinese yuan", "Chinese yuan", "fen", "fen") },
                { "GEL", new("Georgian lari", "Georgian lari", "tetri", "tetri") },
                { "HKD", new("Hong Kong dollar", "Hong Kong dollars", "cent", "cents") },
                { "IDR", new("Indonesian rupiah", "Indonesian rupiahs", "sen", "sen") },
                { "INR", new("Indian rupee", "Indian rupees", "paisa", "paise") },
                { "IQD", new("Iraqi dinar", "Iraqi dinars", "fils", "fils") },
                { "JPY", new("Japanese yen", "Japanese yen", "sen", "sen") }, // Note: JPY fraction often ignored
                { "KHR", new("Cambodian riel", "Cambodian riels", "sen", "sen") },
                { "KGS", new("Kyrgystani som", "Kyrgystani soms", "tyiyn", "tyiyns") },
                { "KRW", new("South Korean won", "South Korean won", "jeon", "jeon") },
                { "KZT", new("Kazakhstani tenge", "Kazakhstani tenge", "tiyn", "tiyn") },
                { "LAK", new("Lao kip", "Lao kips", "att", "att") },
                { "LKR", new("Sri Lankan rupee", "Sri Lankan rupees", "cent", "cents") },
                { "MNT", new("Mongolian tögrög", "Mongolian tögrögs", "möngö", "möngö") },
                { "MYR", new("Malaysian ringgit", "Malaysian ringgits", "sen", "sen") },
                { "NPR", new("Nepalese rupee", "Nepalese rupees", "paisa", "paise") },
                { "PHP", new("Philippine peso", "Philippine pesos", "sentimo", "sentimo") },
                { "PKR", new("Pakistani rupee", "Pakistani rupees", "paisa", "paisa") },
                { "RUB", new("Russian ruble", "Russian rubles", "kopek", "kopeks") },
                { "SGD", new("Singapore dollar", "Singapore dollars", "cent", "cents") },
                { "THB", new("Thai baht", "Thai baht", "satang", "satang") },
                { "TWD", new("new Taiwan dollar", "new Taiwan dollars", "cent", "cents") },
                { "UZS", new("Uzbekistani som", "Uzbekistani som", "tiyin", "tiyin") },
                { "VND", new("Vietnamese dong", "Vietnamese dong", "hao", "hao") },
                // === Europe ===
                { "ALL", new("Albanian lek", "Albanian lekë", "qindarkë", "qindarka") },
                {
                    "BAM",
                    new(
                        "Bosnia-Herzegovina convertible mark",
                        "Bosnia-Herzegovina convertible marks",
                        "fening",
                        "feninga"
                    )
                },
                { "BGN", new("Bulgarian lev", "Bulgarian leva", "stotinka", "stotinki") },
                { "BYN", new("Belarusian ruble", "Belarusian rubles", "kopek", "kopeks") },
                { "CHF", new("Swiss franc", "Swiss francs", "rappen", "rappen") },
                { "CZK", new("Czech koruna", "Czech koruny", "haler", "haleru") },
                { "DKK", new("Danish krone", "Danish kroner", "øre", "øre") },
                { "EUR", new("euro", "euros", "cent", "cents") },
                { "GBP", new("British pound", "British pounds", "penny", "pence") },
                { "HRK", new("Croatian kuna", "Croatian kunas", "lipa", "lipa") }, // Replaced by EUR, but kept for legacy
                { "HUF", new("Hungarian forint", "Hungarian forints", "fillér", "fillér") },
                { "ISK", new("Icelandic krona", "Icelandic kronur", "eyrir", "aurar") }, // Often no fractions used
                { "MDL", new("Moldovan leu", "Moldovan lei", "ban", "bani") },
                { "MKD", new("Macedonian denar", "Macedonian denari", "deni", "deni") },
                { "NOK", new("Norwegian krone", "Norwegian kroner", "øre", "øre") },
                { "PLN", new("Polish zloty", "Polish zlotys", "grosz", "groszy") },
                { "RON", new("Romanian leu", "Romanian lei", "ban", "bani") },
                { "RSD", new("Serbian dinar", "Serbian dinars", "para", "para") },
                { "SEK", new("Swedish krona", "Swedish kronor", "öre", "öre") },
                { "TRY", new("Turkish lira", "Turkish liras", "kurus", "kurus") },
                { "UAH", new("Ukrainian hryvnia", "Ukrainian hryvnias", "kopiyka", "kopiyky") },
                // === Middle East ===
                { "AED", new("UAE dirham", "UAE dirhams", "fils", "fils") },
                { "BHD", new("Bahraini dinar", "Bahraini dinars", "fils", "fils") },
                { "ILS", new("Israeli new shekel", "Israeli new shekels", "agora", "agorot") },
                { "JOD", new("Jordanian dinar", "Jordanian dinars", "piastre", "piastres") },
                { "KWD", new("Kuwaiti dinar", "Kuwaiti dinars", "fils", "fils") },
                { "LBP", new("Lebanese pound", "Lebanese pounds", "piastre", "piastres") },
                { "OMR", new("Omani rial", "Omani rials", "baisa", "baisa") },
                { "QAR", new("Qatari riyal", "Qatari riyals", "dirham", "dirhams") },
                { "SAR", new("Saudi riyal", "Saudi riyals", "halala", "halalas") },
                // === North America ===
                { "CAD", new("Canadian dollar", "Canadian dollars", "cent", "cents") },
                { "CRC", new("Costa Rican colón", "Costa Rican colones", "céntimo", "céntimos") },
                { "DOP", new("Dominican peso", "Dominican pesos", "centavo", "centavos") },
                { "GTQ", new("Guatemalan quetzal", "Guatemalan quetzals", "centavo", "centavos") },
                { "HNL", new("Honduran lempira", "Honduran lempiras", "centavo", "centavos") },
                { "JMD", new("Jamaican dollar", "Jamaican dollars", "cent", "cents") },
                { "MXN", new("Mexican peso", "Mexican pesos", "centavo", "centavos") },
                { "NIO", new("Nicaraguan córdoba", "Nicaraguan córdobas", "centavo", "centavos") },
                {
                    "PAB",
                    new("Panamanian balboa", "Panamanian balboas", "centésimo", "centésimos")
                },
                { "USD", new("US dollar", "US dollars", "cent", "cents") },
                // === Oceania ===
                { "AUD", new("Australian dollar", "Australian dollars", "cent", "cents") },
                { "FJD", new("Fijian dollar", "Fijian dollars", "cent", "cents") },
                { "NZD", new("New Zealand dollar", "New Zealand dollars", "cent", "cents") },
                // === South America ===
                { "ARS", new("Argentine peso", "Argentine pesos", "centavo", "centavos") },
                { "BOB", new("Bolivian boliviano", "Bolivian bolivianos", "centavo", "centavos") },
                { "BRL", new("Brazilian real", "Brazilian reais", "centavo", "centavos") },
                { "CLP", new("Chilean peso", "Chilean pesos", "", "") }, // No standard fraction
                { "COP", new("Colombian peso", "Colombian pesos", "centavo", "centavos") },
                { "PEN", new("Peruvian sol", "Peruvian soles", "céntimo", "céntimos") },
                { "PYG", new("Paraguayan guaraní", "Paraguayan guaraníes", "céntimo", "céntimos") },
                { "UYU", new("Uruguayan peso", "Uruguayan pesos", "centésimo", "centésimos") },
                {
                    "VES",
                    new(
                        "Venezuelan bolívar soberano",
                        "Venezuelan bolívares soberanos",
                        "céntimo",
                        "céntimos"
                    )
                },
            };
            IsoCodeToTTSInfoMap = ttsMapBuilder.ToFrozenDictionary(
                StringComparer.OrdinalIgnoreCase
            );

            // --- Symbol/Code -> ISO Code Mapping Population ---
            Dictionary<string, string> symbolMapBuilder = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> uniqueSymbols = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> uniqueIsoCodes = new(StringComparer.OrdinalIgnoreCase);

            foreach (
                CultureInfo ci in CultureInfo.GetCultures(
                    CultureTypes.SpecificCultures | CultureTypes.InstalledWin32Cultures
                )
            )
            {
                if (
                    ci.IsNeutralCulture
                    || ci.LCID == CultureInfo.InvariantCulture.LCID
                    || ci.Name == ""
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
                    continue;
                } // Cannot create RegionInfo

                string isoCode = region.ISOCurrencySymbol;
                string symbol = region.CurrencySymbol;

                // Only add if we have TTS info for this ISO code
                if (!string.IsNullOrEmpty(isoCode) && IsoCodeToTTSInfoMap.ContainsKey(isoCode))
                {
                    // Add the ISO code itself to the map (e.g., "USD" -> "USD")
                    if (symbolMapBuilder.TryAdd(isoCode, isoCode))
                        uniqueIsoCodes.Add(isoCode);

                    // Add the symbol if it's not empty, not just letters/digits, and not already mapped
                    // Special handling for Yen symbol '¥' to prioritize JPY
                    if (!string.IsNullOrEmpty(symbol) && !symbol.All(char.IsLetterOrDigit))
                    {
                        if (symbol == "¥")
                        {
                            if (symbolMapBuilder.TryAdd("¥", "JPY")) // Map '¥' only once, prioritize JPY
                            {
                                uniqueSymbols.Add("¥");
                            }
                        }
                        else if (symbolMapBuilder.TryAdd(symbol, isoCode)) // Try add other symbols
                        {
                            uniqueSymbols.Add(symbol);
                        }
                    }
                }
            }
            // Ensure JPY mapping for ¥ exists if JPY TTS info is present
            if (IsoCodeToTTSInfoMap.ContainsKey("JPY") && !symbolMapBuilder.ContainsKey("¥"))
            {
                symbolMapBuilder["¥"] = "JPY";
                uniqueSymbols.Add("¥");
            }

            SymbolOrCodeToIsoCodeMap = symbolMapBuilder.ToFrozenDictionary(
                StringComparer.OrdinalIgnoreCase
            );

            // --- Generate Regex Patterns ---
            string symbolPatternPart = string.Join(
                "|",
                uniqueSymbols.Select(Regex.Escape).OrderByDescending(s => s.Length)
            );
            string codePatternPart = string.Join(
                "|",
                uniqueIsoCodes.Select(Regex.Escape).OrderByDescending(s => s.Length)
            );

            // Only initialize regexes if symbols/codes were found
            if (!string.IsNullOrEmpty(symbolPatternPart) && !string.IsNullOrEmpty(codePatternPart))
            {
                // Pattern for Symbol + Number + Code (e.g., "$10 USD")
                string patternSNC =
                    $@"(?<![\p{{L}}\p{{N}}])(?<symbol>{symbolPatternPart})\s?{NumberPatternPart}\s?(?<code>{codePatternPart})(?![\p{{L}}\p{{N}}])";
                // Pattern for Symbol + Number (e.g., "$10")
                string patternSN =
                    $@"(?<![\p{{L}}\p{{N}}])(?<symbol>{symbolPatternPart})\s?{NumberPatternPart}(?![\p{{L}}\p{{N}}])";
                // Pattern for Number + Code (e.g., "10 USD")
                string patternNC =
                    $@"(?<![\p{{L}}\p{{N}}]){NumberPatternPart}\s?(?<code>{codePatternPart})(?![\p{{L}}\p{{N}}])";

                SymbolNumberCodeRegexInstance = BuildRegex(patternSNC);
                SymbolNumberRegexInstance = BuildRegex(patternSN);
                NumberCodeRegexInstance = BuildRegex(patternNC);

                IsInitialized =
                    SymbolNumberCodeRegexInstance != null
                    && SymbolNumberRegexInstance != null
                    && NumberCodeRegexInstance != null;

                if (!IsInitialized)
                {
                    Console.Error.WriteLine(
                        "Warning: One or more currency regex patterns failed to initialize."
                    );
                }
            }
            else
            {
                Console.Error.WriteLine(
                    "Warning: Could not generate currency regex patterns. No unique symbols or codes found/mapped."
                );
                IsInitialized = false;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FATAL: Currency Rule static constructor failed: {ex}");
            IsInitialized = false;
            throw; // Re-throw fatal exceptions during static init
        }
    }

    /// <summary>
    /// Helper to build Regex with options and timeout handling.
    /// </summary>
    private static Regex? BuildRegex(string pattern)
    {
        try
        {
            return new Regex(
                pattern,
                RegexOptions.Compiled | RegexOptions.IgnoreCase,
                RegexTimeout
            );
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error compiling regex pattern '{pattern}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencyNormalizationRule"/> class.
    /// </summary>
    public CurrencyNormalizationRule() { } // Instance constructor

    /// <inheritdoc/>
    public string Apply(string inputText)
    {
        ArgumentNullException.ThrowIfNull(inputText);

        if (!IsInitialized || string.IsNullOrEmpty(inputText))
            return inputText;

        string currentText = inputText;
        try
        {
            // Apply replacements in order of specificity: S+N+C -> S+N -> N+C
            // This ensures that "$10 USD" is matched by the first regex and not partially by the second.
            if (SymbolNumberCodeRegexInstance != null)
            {
                currentText = SymbolNumberCodeRegexInstance.Replace(
                    currentText,
                    CurrencyMatchEvaluator
                );
            }

            if (SymbolNumberRegexInstance != null)
            {
                currentText = SymbolNumberRegexInstance.Replace(
                    currentText,
                    CurrencyMatchEvaluator
                );
            }

            if (NumberCodeRegexInstance != null)
            {
                currentText = NumberCodeRegexInstance.Replace(currentText, CurrencyMatchEvaluator);
            }
        }
        catch (RegexMatchTimeoutException ex)
        {
            Console.Error.WriteLine(
                $"Regex timeout during currency normalization pass: {ex.Message}"
            );
            // Return text processed up to the point of timeout
        }
        catch (Exception ex) // Catch other potential errors during replacement
        {
            Console.Error.WriteLine($"Error during currency normalization: {ex.Message}");
            // Optionally return original text or partially processed text
            // return inputText; // Safer fallback
        }

        return currentText;
    }

    /// <summary>
    /// Shared evaluator for all currency regex matches. Determines ISO code and converts to spoken form.
    /// </summary>
    private static string CurrencyMatchEvaluator(Match match)
    {
        string? isoCode = null;
        string integerPartStr = match.Groups["integer"].Value; // Always expected
        string fractionPartStr = match.Groups["fraction"].Success
            ? match.Groups["fraction"].Value
            : string.Empty;

        // Determine ISO code based on captured groups in the specific match
        // Check which groups are present to infer which regex pattern succeeded
        if (match.Groups["symbol"].Success && match.Groups["code"].Success)
        {
            // S+N+C match (from SymbolNumberCodeRegexInstance): Prioritize the explicit code
            string explicitCode = match.Groups["code"].Value;
            // Verify the explicit code exists in our TTS map
            if (IsoCodeToTTSInfoMap.ContainsKey(explicitCode))
            {
                isoCode = explicitCode;
            }
            else
            {
                // Fallback to symbol's code if explicit code isn't recognized (less likely but possible)
                SymbolOrCodeToIsoCodeMap.TryGetValue(match.Groups["symbol"].Value, out isoCode);
            }
        }
        else if (match.Groups["symbol"].Success)
        {
            // S+N match (from SymbolNumberRegexInstance): Use symbol's code from the map
            SymbolOrCodeToIsoCodeMap.TryGetValue(match.Groups["symbol"].Value, out isoCode);
        }
        else if (match.Groups["code"].Success)
        {
            // N+C match (from NumberCodeRegexInstance): Use the code directly if valid
            string explicitCode = match.Groups["code"].Value;
            // Check if the code is known in the symbol/code map AND has TTS info
            if (
                SymbolOrCodeToIsoCodeMap.ContainsKey(explicitCode)
                && IsoCodeToTTSInfoMap.ContainsKey(explicitCode)
            )
            {
                isoCode = explicitCode;
            }
        }

        // --- Proceed if a valid ISO code was found and is supported ---
        if (
            isoCode == null
            || !IsoCodeToTTSInfoMap.TryGetValue(isoCode, out CurrencyTTSInfo currencyTTSInfo)
        )
        {
            // Cannot determine or unsupported currency, return the original matched text
            return match.Value;
        }

        // --- Parse Numbers ---
        // Remove common separators like commas, spaces, apostrophes, periods (for thousands)
        string integerForParsing = CleanIntegerRegex().Replace(integerPartStr, "");

        if (
            !long.TryParse(
                integerForParsing,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long integerValue
            )
        )
        {
            return match.Value; // Integer parsing failed
        }

        int fractionValue = 0;
        if (!string.IsNullOrEmpty(fractionPartStr))
        {
            // Ensure fraction is treated as two digits (e.g., ".5" becomes 50)
            string paddedFraction =
                fractionPartStr.Length == 1 ? fractionPartStr + "0" : fractionPartStr;
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
                return match.Value; // Invalid fraction format or value
            }
        }

        // --- Convert to Words using Humanizer ---
        try
        {
            // Use InvariantCulture for ToWords to get consistent English number words
            string integerWords = integerValue.ToWords(CultureInfo.InvariantCulture);
            string? fractionWords =
                fractionValue > 0 ? fractionValue.ToWords(CultureInfo.InvariantCulture) : null;

            // --- Build Spoken String ---
            StringBuilder builder = new();
            builder.Append(integerWords);
            builder.Append(' ');
            builder.Append(integerValue == 1 ? currencyTTSInfo.Singular : currencyTTSInfo.Plural);

            // Only add fraction part if it's greater than zero
            if (fractionWords != null && fractionValue > 0)
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

            // Pad result with spaces for proper separation in the final text
            return $" {builder} ";
        }
        catch (Exception ex)
        {
            // Log Humanizer errors
            Console.Error.WriteLine(
                $"Humanizer failed for '{match.Value}' (ISO: {isoCode}): {ex.Message}"
            );
            return match.Value; // Return original on Humanizer error
        }
    }

    [GeneratedRegex("[,' .]", RegexOptions.Compiled)]
    private static partial Regex CleanIntegerRegex();
}
