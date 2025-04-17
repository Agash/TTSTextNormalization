using TTSTextNormalization.Abstractions;

namespace TTSTextNormalization.Core;

/// <summary>
/// Implements ITextNormalizer by executing a sequence of ITextNormalizationRule instances.
/// Rules are injected via DI and ordered based on their 'Order' property.
/// </summary>
/// <remarks>
/// This class is intended to be registered as the primary ITextNormalizer implementation in DI.
/// </remarks>
public sealed class TextNormalizationPipeline : ITextNormalizer
{
    // Store the ordered sequence of rules provided by DI.
    private readonly IReadOnlyList<ITextNormalizationRule> _rules;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextNormalizationPipeline"/> class.
    /// </summary>
    /// <param name="rules">
    /// An injectable collection of normalization rules. The pipeline automatically
    /// sorts these rules based on their 'Order' property before execution.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if the injected rules collection is null.</exception>
    public TextNormalizationPipeline(IEnumerable<ITextNormalizationRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        // Order the rules upon initialization and store them immutably for the lifetime of the pipeline instance.
        // This ensures consistent execution order and avoids re-sorting on every Normalize call.
        _rules = rules.OrderBy(rule => rule.Order)
                      .ToList() // Materialize the ordered list
                      .AsReadOnly(); // Store as read-only view
    }

    /// <summary>
    /// Normalizes the input text by applying the configured rules sequentially in their specified order.
    /// </summary>
    /// <param name="inputText">The raw text to normalize.</param>
    /// <returns>The normalized text after processing through the pipeline.</returns>
    public string Normalize(string? inputText)
    {
        // Handle null, empty, or whitespace input at the pipeline entry point.
        if (string.IsNullOrWhiteSpace(inputText))
            return string.Empty; // Consistent return for invalid input.

        string currentText = inputText;

        // Execute each rule in the pre-sorted order.
        foreach (ITextNormalizationRule rule in _rules)
        {
            currentText = rule.Apply(currentText);

            // Early exit if text becomes empty? Generally no, as later rules might still be needed
            // (e.g., logging, inserting placeholders). Let rules handle empty input if necessary.
        }

        // A final whitespace cleanup rule (with a high Order number) is recommended
        // to handle extra spaces introduced by rules like EmojiNormalizationRule.
        // This rule would be added via DI like any other rule.

        return currentText;
    }
}