using System.Collections.Frozen;
using Microsoft.Extensions.DependencyInjection;
using TTSTextNormalization.Abstractions;
using TTSTextNormalization.DependencyInjection;
using TTSTextNormalization.Rules;

namespace TTSTextNormalization.Tests.Core;

[TestClass]
public class TextNormalizationPipelineTests
{
    // Helper to build the normalizer with specific rule configurations
    private static ITextNormalizer BuildNormalizer(
        Action<ITextNormalizationBuilder> configureRules,
        Action<IServiceCollection>? configureServices = null
    )
    {
        ServiceCollection services = new();

        // Allow additional service configuration (e.g., for options)
        configureServices?.Invoke(services);

        // Add logging (optional, but useful for debugging pipeline issues)
        // services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

        // Configure the normalization pipeline rules
        services.AddTextNormalization(configureRules);

        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ITextNormalizer>();
    }

    // ... existing tests ...

    [TestMethod]
    public void Normalize_RuleOrderOverride_ExecutesInSpecifiedOrder()
    {
        // Goal: Run Whitespace (default 9000) *before* Emoji (default 100)
        ITextNormalizer normalizer = BuildNormalizer(builder =>
        {
            // Add Emoji with default order (100)
            builder.AddEmojiRule();
            // Add Whitespace but override order to run first (e.g., 50)
            builder.AddWhitespaceNormalizationRule(orderOverride: 50);
        });

        string input = "  Hello   ✨  world  ";
        // Expected:
        // 1. Whitespace (Order 50): "Hello ✨ world"
        // 2. Emoji (Order 100): "Hello  sparkles  world"
        // Note: Final whitespace rule isn't present to clean up the emoji spaces
        string expected = "Hello  sparkles  world";
        Assert.AreEqual(expected, normalizer.Normalize(input));
    }

    [TestMethod]
    public void Normalize_AbbreviationWithOptions_UsesCustomAbbreviations()
    {
        static void configureServices(IServiceCollection services)
        {
            services.Configure<AbbreviationRuleOptions>(options =>
            {
                Dictionary<string, string> customMap = new(StringComparer.OrdinalIgnoreCase)
                {
                    { "custom", "my custom expansion" },
                    { "gg", "very good game" },
                };
                options.CustomAbbreviations = customMap.ToFrozenDictionary(
                    StringComparer.OrdinalIgnoreCase
                );
                options.ReplaceDefaultAbbreviations = false;
            });
        }

        ITextNormalizer normalizer = BuildNormalizer(
            builder => builder.AddAbbreviationNormalizationRule(),
            configureServices
        );

        string input = "This is custom, gg.";
        string expected = "This is  my custom expansion ,  very good game .";
        Assert.AreEqual(expected, normalizer.Normalize(input));
    }

    [TestMethod]
    public void Normalize_AbbreviationWithOptions_ReplaceDefaults()
    {
        static void configureServices(IServiceCollection services)
        {
            services.Configure<AbbreviationRuleOptions>(options =>
            {
                Dictionary<string, string> customMap = new(StringComparer.OrdinalIgnoreCase)
                {
                    { "abc", "alphabet" },
                };
                options.CustomAbbreviations = customMap.ToFrozenDictionary(
                    StringComparer.OrdinalIgnoreCase
                );
                options.ReplaceDefaultAbbreviations = true;
            });
        }

        ITextNormalizer normalizer = BuildNormalizer(
            builder => builder.AddAbbreviationNormalizationRule(),
            configureServices
        );

        string input = "Use abc not lol.";
        string expected = "Use  alphabet  not lol.";
        Assert.AreEqual(expected, normalizer.Normalize(input));
    }

    // --- New Tests for URL and Emoji Options in Pipeline ---

    [TestMethod]
    public void Normalize_UrlRuleWithOptions_UsesCustomPlaceholder()
    {
        static void configureServices(IServiceCollection services)
        {
            services.Configure<UrlRuleOptions>(options =>
            {
                options.PlaceholderText = "[REDACTED_URL]";
            });
        }

        ITextNormalizer normalizer = BuildNormalizer(
            builder =>
            {
                builder.AddUrlNormalizationRule();
                builder.AddWhitespaceNormalizationRule(); // Add whitespace for cleanup
            },
            configureServices
        );

        string input = "Go to www.example.com now";
        string expected = "Go to [REDACTED_URL] now"; // Whitespace rule cleans up extra spaces
        Assert.AreEqual(expected, normalizer.Normalize(input));
    }

    [TestMethod]
    public void Normalize_EmojiRuleWithOptions_AddsPrefixSuffix()
    {
        static void configureServices(IServiceCollection services)
        {
            services.Configure<EmojiRuleOptions>(options =>
            {
                options.Prefix = "the";
                options.Suffix = "emoji";
            });
        }

        ITextNormalizer normalizer = BuildNormalizer(
            builder =>
            {
                builder.AddEmojiRule();
                builder.AddWhitespaceNormalizationRule(); // Add whitespace for cleanup
            },
            configureServices
        );

        string input = "Look: ✨ !";
        // Expected pipeline:
        // 1. Emoji: "Look:  the sparkles emoji  !"
        // 2. Whitespace: "Look: the sparkles emoji!"
        string expected = "Look: the sparkles emoji!";
        Assert.AreEqual(expected, normalizer.Normalize(input));
    }

    // --- Update Comprehensive Test to Use Options (Example) ---
    [TestMethod]
    [DataRow(
        "  ‘Test’ 1st..  soooo   cool ✨!! LOL go to https://example.com/page?q=1 Cost: $12.50 USD??? ",
        "'Test' first. soo cool the sparkles emoji! laughing out loud go to [URL] Cost: twelve US dollars fifty cents?",
        DisplayName = "All Rules Integration Test - With Custom Options"
    )]
    public void Normalize_AllRulesEnabled_HandlesComplexInput_WithCustomOptions(
        string input,
        string expected
    )
    {
        static void configureServices(IServiceCollection services)
        {
            services.Configure<UrlRuleOptions>(options => options.PlaceholderText = "[URL]");
            services.Configure<EmojiRuleOptions>(options =>
            {
                options.Prefix = "the";
                options.Suffix = "emoji";
            });
            // Could configure AbbreviationRuleOptions here too if needed
        }

        ITextNormalizer normalizer = BuildNormalizer(
            builder =>
            {
                builder.AddBasicSanitizationRule(); // 10
                builder.AddUrlNormalizationRule(); // 20
                builder.AddEmojiRule(); // 100
                builder.AddCurrencyRule(); // 200
                builder.AddAbbreviationNormalizationRule(); // 300
                builder.AddNumberNormalizationRule(); // 400
                builder.AddExcessivePunctuationRule(); // 500
                builder.AddLetterRepetitionRule(); // 510
                builder.AddWhitespaceNormalizationRule(); // 9000
            },
            configureServices
        ); // Pass the options configuration

        string result = normalizer.Normalize(input);
        Assert.AreEqual(expected, result);
    }
}
