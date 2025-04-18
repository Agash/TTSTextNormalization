namespace TTSTextNormalization.Rules;

/// <summary>
/// Configuration options for the <see cref="UrlNormalizationRule"/>.
/// </summary>
public sealed class UrlRuleOptions
{
    /// <summary>
    /// Gets or sets the placeholder text used to replace detected URLs.
    /// Defaults to " link ". Remember to include padding spaces if desired for TTS.
    /// </summary>
    public string PlaceholderText { get; set; } = " link ";
}