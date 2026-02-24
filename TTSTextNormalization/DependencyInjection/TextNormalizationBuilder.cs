using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.DependencyInjection;

/// <summary>
/// Default implementation of <see cref="ITextNormalizationBuilder"/>.
/// </summary>
internal sealed class TextNormalizationBuilder(IServiceCollection services) : ITextNormalizationBuilder
{
    /// <inheritdoc/>
    public IServiceCollection Services { get; } =
        services ?? throw new ArgumentNullException(nameof(services));

    // Store registration details internally
    internal List<RuleRegistration> Registrations { get; } = [];

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
        Registrations.Add(new RuleRegistration(typeof(T), lifetime, orderOverride));

        return this;
    }
}
