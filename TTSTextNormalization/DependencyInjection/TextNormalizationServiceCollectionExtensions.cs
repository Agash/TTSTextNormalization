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
    /// for configuring normalization rules and their order.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">An action to configure the normalization rules using the builder.</param>
    /// <returns>The service collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown if services or configure is null.</exception>
    public static IServiceCollection AddTextNormalization(
        this IServiceCollection services,
        Action<ITextNormalizationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Ensure Options services are registered
        services.AddOptions();

        // Create the builder which will collect registrations
        TextNormalizationBuilder builder = new(services);
        configure(builder);

        // Register the collected RuleRegistrations so the pipeline can access them
        services.AddSingleton(builder.Registrations as IEnumerable<RuleRegistration>);

        // Register the main normalizer implementation.
        services.TryAddSingleton<ITextNormalizer, TextNormalizationPipeline>();

        // Note: We do NOT register default IOptions<T> instances here.
        // The framework handles resolving IOptions<T> based on services.Configure<T>() calls.
        // Users should use services.Configure<RuleOptions>(...) to set options.

        return services;
    }

    // --- Built-in Rule Extensions for the Builder ---

    /// <summary>
    /// Adds the <see cref="BasicSanitizationRule"/> to the text normalization pipeline.
    /// Performs essential cleanup like normalizing line breaks and replacing fancy characters. Default Order: 10.
    /// </summary>
    /// <param name="builder">The text normalization builder.</param>
    /// <param name="orderOverride">Optional order override.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static ITextNormalizationBuilder AddBasicSanitizationRule(
        this ITextNormalizationBuilder builder, int? orderOverride = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<BasicSanitizationRule>(ServiceLifetime.Singleton, orderOverride);
    }

    /// <summary>
    /// Adds the <see cref="EmojiNormalizationRule"/> to the text normalization pipeline.
    /// Replaces standard Unicode emojis with their textual descriptions. Default Order: 100.
    /// Configurable via <see cref="EmojiRuleOptions"/>.
    /// </summary>
    /// <param name="builder">The text normalization builder.</param>
    /// <param name="orderOverride">Optional order override.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static ITextNormalizationBuilder AddEmojiRule(
        this ITextNormalizationBuilder builder, int? orderOverride = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        // Rule now depends on IOptions, use Singleton
        return builder.AddRule<EmojiNormalizationRule>(ServiceLifetime.Singleton, orderOverride);
    }

    /// <summary>
    /// Adds the <see cref="CurrencyNormalizationRule"/> to the text normalization pipeline.
    /// Normalizes currency symbols and codes (e.g., "$10.50", "100 EUR") into spoken text. Default Order: 200.
    /// </summary>
    /// <param name="builder">The text normalization builder.</param>
    /// <param name="orderOverride">Optional order override.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static ITextNormalizationBuilder AddCurrencyRule(
        this ITextNormalizationBuilder builder, int? orderOverride = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<CurrencyNormalizationRule>(ServiceLifetime.Singleton, orderOverride);
    }

    /// <summary>
    /// Adds the <see cref="AbbreviationNormalizationRule"/> to the text normalization pipeline.
    /// Expands common chat/gaming abbreviations (e.g., "lol", "gg"). Default Order: 300.
    /// Configurable via <see cref="AbbreviationRuleOptions"/>.
    /// </summary>
    /// <param name="builder">The text normalization builder.</param>
    /// <param name="orderOverride">Optional order override.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static ITextNormalizationBuilder AddAbbreviationNormalizationRule(
        this ITextNormalizationBuilder builder, int? orderOverride = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<AbbreviationNormalizationRule>(ServiceLifetime.Singleton, orderOverride);
    }

    /// <summary>
    /// Adds the <see cref="NumberNormalizationRule"/> to the text normalization pipeline.
    /// Converts cardinals, ordinals, decimals, and version-like numbers into words. Default Order: 400.
    /// </summary>
    /// <param name="builder">The text normalization builder.</param>
    /// <param name="orderOverride">Optional order override.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static ITextNormalizationBuilder AddNumberNormalizationRule(
        this ITextNormalizationBuilder builder, int? orderOverride = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<NumberNormalizationRule>(ServiceLifetime.Singleton, orderOverride);
    }

    /// <summary>
    /// Adds the <see cref="ExcessivePunctuationRule"/> to the text normalization pipeline.
    /// Reduces sequences like "!!!" to "!". Default Order: 500.
    /// </summary>
    /// <param name="builder">The text normalization builder.</param>
    /// <param name="orderOverride">Optional order override.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static ITextNormalizationBuilder AddExcessivePunctuationRule(
        this ITextNormalizationBuilder builder, int? orderOverride = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<ExcessivePunctuationRule>(ServiceLifetime.Singleton, orderOverride);
    }

    /// <summary>
    /// Adds the <see cref="LetterRepetitionRule"/> to the text normalization pipeline.
    /// Reduces sequences like "soooo" to "soo". Default Order: 510.
    /// </summary>
    /// <param name="builder">The text normalization builder.</param>
    /// <param name="orderOverride">Optional order override.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static ITextNormalizationBuilder AddLetterRepetitionRule(
        this ITextNormalizationBuilder builder, int? orderOverride = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<LetterRepetitionRule>(ServiceLifetime.Singleton, orderOverride);
    }

    /// <summary>
    /// Adds the <see cref="UrlNormalizationRule"/> to the text normalization pipeline.
    /// Replaces detected URLs with a placeholder. Default Order: 600.
    /// Configurable via <see cref="UrlRuleOptions"/>.
    /// </summary>
    /// <param name="builder">The text normalization builder.</param>
    /// <param name="orderOverride">Optional order override.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static ITextNormalizationBuilder AddUrlNormalizationRule(
        this ITextNormalizationBuilder builder, int? orderOverride = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        // Rule now depends on IOptions, use Singleton
        return builder.AddRule<UrlNormalizationRule>(ServiceLifetime.Singleton, orderOverride);
    }

    /// <summary>
    /// Adds the <see cref="WhitespaceNormalizationRule"/> to the text normalization pipeline.
    /// Trims ends, collapses internal spaces, and adjusts spacing around punctuation. Default Order: 9000.
    /// </summary>
    /// <param name="builder">The text normalization builder.</param>
    /// <param name="orderOverride">Optional order override.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static ITextNormalizationBuilder AddWhitespaceNormalizationRule(
        this ITextNormalizationBuilder builder, int? orderOverride = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRule<WhitespaceNormalizationRule>(ServiceLifetime.Singleton, orderOverride);
    }
}