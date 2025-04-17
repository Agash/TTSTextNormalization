using Microsoft.Extensions.DependencyInjection;
using TTSTextNormalization.Abstractions;

// Namespace reflects DI focus
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
    /// Adds a custom normalization rule to the pipeline.
    /// </summary>
    /// <typeparam name="T">The type of the rule implementation, must inherit from ITextNormalizationRule.</typeparam>
    /// <param name="lifetime">The service lifetime for the rule (Singleton recommended for stateless rules).</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    ITextNormalizationBuilder AddRule<T>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where T : class, ITextNormalizationRule;
}