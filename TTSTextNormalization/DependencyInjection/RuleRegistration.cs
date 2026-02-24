using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.DependencyInjection;

/// <summary>
/// Internal record holding registration details for a normalization rule.
/// </summary>
public sealed record RuleRegistration
{
    /// <summary>
    /// The concrete type of the rule implementation (<see cref="ITextNormalizationRule"/>).
    /// </summary>
    public required Type RuleType { get; init; }

    /// <summary>
    /// The desired service lifetime for this rule instance.
    /// </summary>
    public required ServiceLifetime Lifetime { get; init; }

    /// <summary>
    /// An optional override for the rule's default <see cref="ITextNormalizationRule.Order"/>.
    /// If null, the rule's default order is used.
    /// </summary>
    public int? OrderOverride { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleRegistration"/> record.
    /// </summary>
    /// <param name="ruleType">The concrete rule implementation type.</param>
    /// <param name="lifetime">The service lifetime for the rule.</param>
    /// <param name="orderOverride">Optional order override for the rule.</param>
    [SetsRequiredMembers]
    public RuleRegistration(Type ruleType, ServiceLifetime lifetime, int? orderOverride = null)
    {
        RuleType = ruleType;
        Lifetime = lifetime;
        OrderOverride = orderOverride;
    }
}
