using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TTSTextNormalization.Abstractions;
using TTSTextNormalization.Core;
using TTSTextNormalization.Rules;

namespace TTSTextNormalization.DependencyInjection;

/// <summary>
/// Provides extension methods for configuring text normalization services
/// in an <see cref="IServiceCollection"/>.
/// </summary>
public static class TextNormalizationServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core text normalization services and provides a builder
    /// for configuring normalization rules.
    /// </summary>
    public static IServiceCollection AddTextNormalization(
        this IServiceCollection services,
        Action<ITextNormalizationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        services.TryAddSingleton<ITextNormalizer, TextNormalizationPipeline>();
        TextNormalizationBuilder builder = new(services);
        configure(builder);
        return services;
    }

    // --- Built-in Rule Extensions for the Builder ---

    /// <summary>
    /// Adds the <see cref="BasicSanitizationRule"/> to the text normalization pipeline.
    /// Performs essential cleanup like normalizing line breaks and replacing fancy characters. Recommended Order: 10.
    /// </summary>
    /// <param name="builder">The text normalization builder.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static ITextNormalizationBuilder AddBasicSanitizationRule(this ITextNormalizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<BasicSanitizationRule>(ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Adds the <see cref="EmojiNormalizationRule"/> to the text normalization pipeline.
    /// Replaces standard Unicode emojis with their textual descriptions. Recommended Order: 100.
    /// </summary>
    /// <param name="builder">The text normalization builder.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static ITextNormalizationBuilder AddEmojiRule(this ITextNormalizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<EmojiNormalizationRule>(ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Adds the <see cref="CurrencyNormalizationRule"/> to the text normalization pipeline.
    /// Normalizes currency symbols and codes (e.g., "$10.50", "100 EUR") into spoken text. Recommended Order: 200.
    /// </summary>
    /// <param name="builder">The text normalization builder.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static ITextNormalizationBuilder AddCurrencyRule(this ITextNormalizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<CurrencyNormalizationRule>(ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Adds the <see cref="AbbreviationNormalizationRule"/> to the text normalization pipeline.
    /// Expands common chat/gaming abbreviations (e.g., "lol", "gg"). Recommended Order: 300.
    /// </summary>
    /// <param name="builder">The text normalization builder.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static ITextNormalizationBuilder AddAbbreviationNormalizationRule(this ITextNormalizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<AbbreviationNormalizationRule>(ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Adds the <see cref="NumberNormalizationRule"/> to the text normalization pipeline.
    /// Converts cardinals, ordinals, decimals, and version-like numbers into words. Recommended Order: 400.
    /// </summary>
    /// <param name="builder">The text normalization builder.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static ITextNormalizationBuilder AddNumberNormalizationRule(this ITextNormalizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<NumberNormalizationRule>(ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Adds the <see cref="ExcessivePunctuationRule"/> to the text normalization pipeline.
    /// Reduces sequences like "!!!" to "!". Recommended Order: 500.
    /// </summary>
    public static ITextNormalizationBuilder AddExcessivePunctuationRule(this ITextNormalizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<ExcessivePunctuationRule>(ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Adds the <see cref="LetterRepetitionRule"/> to the text normalization pipeline.
    /// Reduces sequences like "soooo" to "soo". Recommended Order: 510.
    /// </summary>
    public static ITextNormalizationBuilder AddLetterRepetitionRule(this ITextNormalizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<LetterRepetitionRule>(ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Adds the <see cref="WhitespaceNormalizationRule"/> to the text normalization pipeline.
    /// Trims ends, collapses internal spaces, and adjusts spacing around punctuation. Recommended Order: 9000.
    /// </summary>
    /// <param name="builder">The text normalization builder.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static ITextNormalizationBuilder AddWhitespaceNormalizationRule(this ITextNormalizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<WhitespaceNormalizationRule>(ServiceLifetime.Singleton);
    }
}