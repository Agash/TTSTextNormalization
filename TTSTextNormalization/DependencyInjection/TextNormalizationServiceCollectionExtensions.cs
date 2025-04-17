using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TTSTextNormalization.Rules;
using TTSTextNormalization.Abstractions;
using TTSTextNormalization.Core;

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
    public static ITextNormalizationBuilder AddBasicSanitizationRule(this ITextNormalizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<BasicSanitizationRule>(ServiceLifetime.Singleton);
    }

    public static ITextNormalizationBuilder AddEmojiRule(this ITextNormalizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<EmojiNormalizationRule>(ServiceLifetime.Singleton);
    }

    public static ITextNormalizationBuilder AddCurrencyRule(this ITextNormalizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<CurrencyNormalizationRule>(ServiceLifetime.Singleton);
    }

    public static ITextNormalizationBuilder AddAbbreviationNormalizationRule(this ITextNormalizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<AbbreviationNormalizationRule>(ServiceLifetime.Singleton);
    }

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

    public static ITextNormalizationBuilder AddWhitespaceNormalizationRule(this ITextNormalizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<WhitespaceNormalizationRule>(ServiceLifetime.Singleton);
    }
}