using Microsoft.Extensions.DependencyInjection;
using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.DependencyInjection;

/// <summary>
/// Internal record holding registration details for a normalization rule.
/// </summary>
public sealed class RuleRegistration
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
}