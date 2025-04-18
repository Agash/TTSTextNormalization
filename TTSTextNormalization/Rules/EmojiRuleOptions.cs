namespace TTSTextNormalization.Rules;

/// <summary>
/// Configuration options for the <see cref="EmojiNormalizationRule"/>.
/// </summary>
public sealed class EmojiRuleOptions
{
    /// <summary>
    /// Gets or sets an optional string to prepend to the normalized emoji name.
    /// Remember to include trailing space if needed.
    /// Example: Setting to "emoji " would result in "emoji grinning face".
    /// Defaults to null (no prefix).
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Gets or sets an optional string to append to the normalized emoji name.
    /// Remember to include leading space if needed.
    /// Example: Setting to " emoji" would result in "grinning face emoji".
    /// Defaults to null (no suffix).
    /// </summary>
    public string? Suffix { get; set; }
}