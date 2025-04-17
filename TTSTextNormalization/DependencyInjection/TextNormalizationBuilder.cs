using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.DependencyInjection;

/// <summary>
/// Default implementation of <see cref="ITextNormalizationBuilder"/>.
/// </summary>
internal sealed class TextNormalizationBuilder : ITextNormalizationBuilder
{
    /// <summary>
    /// Gets the underlying service collection.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextNormalizationBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <exception cref="ArgumentNullException">Thrown if services is null.</exception>
    public TextNormalizationBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <summary>
    /// Adds a custom normalization rule to the pipeline by registering it
    /// with the service collection as <see cref="ITextNormalizationRule"/>.
    /// </summary>
    /// <typeparam name="T">The type of the rule implementation.</typeparam>
    /// <param name="lifetime">The desired service lifetime.</param>
    /// <returns>The builder instance.</returns>
    public ITextNormalizationBuilder AddRule<T>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where T : class, ITextNormalizationRule
    {
        // TryAddEnumerable ensures multiple rules of the same type or different types
        // can be added and injected as IEnumerable<ITextNormalizationRule>.
        // It registers T both as itself and as ITextNormalizationRule.
        Services.TryAddEnumerable(ServiceDescriptor.Describe(
            typeof(ITextNormalizationRule), // Service Type (what the pipeline asks for)
            typeof(T),                     // Implementation Type (the concrete rule class)
            lifetime));                    // Lifetime (Singleton recommended for stateless rules)

        return this;
    }
}