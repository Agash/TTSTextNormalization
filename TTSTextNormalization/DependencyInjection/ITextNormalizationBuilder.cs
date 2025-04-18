using Microsoft.Extensions.DependencyInjection;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.DependencyInjection;

/// <summary>
/// Defines a builder interface for configuring text normalization rules
/// within the service collection.
/// </summary>
public interface ITextNormalizationBuilder
{
    /// <summary>
    /// Gets the underlying service collection.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Adds a normalization rule implementation to the pipeline configuration.
    /// </summary>
    /// <typeparam name="T">The type of the rule implementation, must inherit from <see cref="ITextNormalizationRule"/>.</typeparam>
    /// <param name="lifetime">The service lifetime for the rule (Singleton recommended for stateless rules, Scoped or Transient if stateful or using Scoped dependencies like IOptionsSnapshot).</param>
    /// <param name="orderOverride">An optional integer to override the rule's default <see cref="ITextNormalizationRule.Order"/>.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <remarks>
    /// This method registers the rule type <typeparamref name="T"/> with the specified <paramref name="lifetime"/>
    /// and records the registration details (including <paramref name="orderOverride"/>) for the pipeline to use.
    /// </remarks>
    ITextNormalizationBuilder AddRule<T>(
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        int? orderOverride = null)
        where T : class, ITextNormalizationRule;
}