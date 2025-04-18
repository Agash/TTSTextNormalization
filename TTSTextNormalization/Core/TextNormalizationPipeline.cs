using TTSTextNormalization.Abstractions;
using TTSTextNormalization.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace TTSTextNormalization.Core;

/// <summary>
/// Implements ITextNormalizer by resolving registered rules via <see cref="RuleRegistration"/>
/// records, determining their effective order (considering overrides), and executing them sequentially.
/// </summary>
/// <remarks>
/// This class is intended to be registered as the primary ITextNormalizer implementation in DI.
/// It requires <see cref="IServiceProvider"/> and the list of <see cref="RuleRegistration"/>
/// (typically configured via <see cref="TextNormalizationServiceCollectionExtensions.AddTextNormalization"/>).
/// </remarks>
public sealed class TextNormalizationPipeline : ITextNormalizer
{
    private readonly ReadOnlyCollection<ITextNormalizationRule> _orderedRules;
    private readonly ILogger<TextNormalizationPipeline>? _logger; // Optional logging

    /// <summary>
    /// Initializes a new instance of the <see cref="TextNormalizationPipeline"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve rule instances.</param>
    /// <param name="registrations">The collection of rule registrations configured via the builder.</param>
    /// <param name="logger">Optional logger.</param>
    /// <exception cref="ArgumentNullException">Thrown if serviceProvider or registrations is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a registered rule cannot be resolved or does not implement <see cref="ITextNormalizationRule"/>.</exception>
    public TextNormalizationPipeline(
        IServiceProvider serviceProvider,
        IEnumerable<RuleRegistration> registrations,
        ILogger<TextNormalizationPipeline>? logger = null) // Make logger optional
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(registrations);
        _logger = logger;

        var resolvedAndOrderedRules = new List<(ITextNormalizationRule Rule, int EffectiveOrder)>();

        _logger?.LogDebug("Constructing TextNormalizationPipeline. Resolving and ordering rules...");

        foreach (var registration in registrations)
        {
            try
            {
                // Resolve the rule instance from the service provider using its concrete type
                object? serviceInstance = serviceProvider.GetService(registration.RuleType);

                if (serviceInstance is not ITextNormalizationRule ruleInstance)
                {
                    // This should not happen if AddRule was used correctly, but check defensively
                    var errorMsg = $"Resolved service for type '{registration.RuleType.FullName}' does not implement ITextNormalizationRule.";
                    _logger?.LogError("Resolved service for type '{RegistrationType}' does not implement ITextNormalizationRule.", registration.RuleType.FullName);
                    throw new InvalidOperationException(errorMsg);
                }

                // Determine the effective order
                int effectiveOrder = registration.OrderOverride ?? ruleInstance.Order;

                resolvedAndOrderedRules.Add((ruleInstance, effectiveOrder));

                _logger?.LogTrace("Resolved rule '{RuleType}' with effective order {Order}.", registration.RuleType.Name, effectiveOrder);
            }
            catch (Exception ex)
            {
                // Catch resolution errors
                var errorMsg = $"Failed to resolve or process rule registration for type '{registration.RuleType.FullName}'. See inner exception.";
                _logger?.LogError(ex, "Failed to resolve or process rule registration for type '{RegistrationType}'. See inner exception.", registration.RuleType.FullName);
                throw new InvalidOperationException(errorMsg, ex);
            }
        }

        // Sort the resolved rules by their effective order
        _orderedRules = resolvedAndOrderedRules
            .OrderBy(r => r.EffectiveOrder)
            .Select(r => r.Rule)
            .ToList()
            .AsReadOnly();

        _logger?.LogInformation("TextNormalizationPipeline constructed with {RuleCount} rules.", _orderedRules.Count);
        if (_logger?.IsEnabled(LogLevel.Debug) ?? false)
        {
            foreach (var rule in _orderedRules)
            {
                _logger.LogDebug(" > Rule: {RuleName} (Order: {Order})", rule.GetType().Name, rule.Order); // Log default order for reference
            }
        }
    }

    /// <summary>
    /// Normalizes the input text by applying the configured and ordered rules sequentially.
    /// </summary>
    /// <param name="inputText">The raw text to normalize.</param>
    /// <returns>The normalized text after processing through the pipeline.</returns>
    public string Normalize(string? inputText)
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            _logger?.LogDebug("Input text is null or whitespace, returning empty string.");
            return string.Empty;
        }

        _logger?.LogTrace("Starting normalization for input: \"{InputText}\"", inputText);

        string currentText = inputText;
        foreach (ITextNormalizationRule rule in _orderedRules)
        {
            string ruleName = rule.GetType().Name;
            _logger?.LogTrace("Applying rule: {RuleName}", ruleName);
            try
            {
                string previousText = currentText;
                currentText = rule.Apply(currentText);
                if (currentText != previousText && (_logger?.IsEnabled(LogLevel.Trace) ?? false))
                {
                    _logger?.LogTrace("Rule {RuleName} modified text to: \"{CurrentText}\"", ruleName, currentText);
                }
                else if (currentText == previousText)
                {
                    _logger?.LogTrace("Rule {RuleName} made no changes.", ruleName);
                }

                if (string.IsNullOrEmpty(currentText))
                {
                    _logger?.LogDebug("Text became empty after rule {RuleName}, exiting pipeline early.", ruleName);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error applying rule {RuleName}. Skipping rule and continuing pipeline.", ruleName);
                // Optionally re-throw or handle differently, for now continue pipeline
                // return inputText; // Or return original on error?
            }
        }

        _logger?.LogDebug("Normalization complete. Final text: \"{FinalText}\"", currentText);
        return currentText;
    }
}