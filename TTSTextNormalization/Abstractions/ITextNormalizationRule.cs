namespace TTSTextNormalization.Abstractions;

/// <summary>
/// Defines the contract for a single text normalization rule within the pipeline.
/// Rules are executed sequentially based on their Order property.
/// </summary>
public interface ITextNormalizationRule
{
    /// <summary>
    /// Gets the order in which this rule should be executed relative to others.
    /// Lower numbers run first (e.g., 0 runs before 100).
    /// Rules with the same order value might execute in an unpredictable sequence relative to each other.
    /// Consider defining common order ranges (e.g., PreProcessing = 0-99, Core = 100-499, PostProcessing = 500+).
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Applies the normalization rule to the input text.
    /// </summary>
    /// <param name="inputText">The text processed by preceding rules (or the original input).</param>
    /// <returns>The text after applying this rule.</returns>
    /// <remarks>
    /// Implementations should be robust. While the pipeline handles initial null/whitespace checks,
    /// rules might receive empty strings from preceding rules.
    /// If registered as singletons, implementations should ideally be stateless or thread-safe.
    /// </remarks>
    string Apply(string inputText);
}