using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.DependencyInjection;

/// <summary>
/// Default implementation of <see cref="ITextNormalizationBuilder"/>.
/// </summary>
internal sealed class TextNormalizationBuilder : ITextNormalizationBuilder
{
    /// <inheritdoc/>
    public IServiceCollection Services { get; }

    // Store registration details internally
    internal List<RuleRegistration> Registrations { get; } = [];

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

    /// <inheritdoc/>
    public ITextNormalizationBuilder AddRule<T>(
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        int? orderOverride = null)
        where T : class, ITextNormalizationRule
    {
        // 1. Register the concrete rule type itself so the pipeline can resolve it.
        //    Use TryAdd to avoid duplicate registrations of the type itself.
        Services.TryAdd(new ServiceDescriptor(typeof(T), typeof(T), lifetime));

        // 2. Record the registration details (including the override) for the pipeline.
        //    We allow multiple registrations if needed, although the pipeline
        //    will likely resolve only one instance per type unless configured differently.
        Registrations.Add(new RuleRegistration
        {
            RuleType = typeof(T),
            Lifetime = lifetime,
            OrderOverride = orderOverride
        });

        return this;
    }
}