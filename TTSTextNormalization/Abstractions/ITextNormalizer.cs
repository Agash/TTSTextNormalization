namespace TTSTextNormalization.Abstractions;

/// <summary>
/// Defines the contract for the main text normalization service.
/// </summary>
public interface ITextNormalizer
{
    /// <summary>
    /// Normalizes the input text according to the configured pipeline of rules.
    /// </summary>
    /// <param name="inputText">The text to normalize.</param>
    /// <returns>The normalized text, suitable for TTS processing.</returns>
    /// <remarks>
    /// Implementations should handle null or empty input gracefully,
    /// typically by returning an empty string.
    /// </remarks>
    string Normalize(string? inputText);
}