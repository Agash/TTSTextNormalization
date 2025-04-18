using System.Collections.Frozen;

namespace TTSTextNormalization.Rules;

/// <summary>
/// Configuration options for the AbbreviationNormalizationRule.
/// </summary>
public sealed class AbbreviationRuleOptions
{
    /// <summary>
    /// Gets or sets a dictionary of custom abbreviations to add to or replace the default ones.
    /// Keys are the abbreviations (case-insensitive), values are the expansions.
    /// If a key exists in both default and custom maps, the custom value takes precedence.
    /// </summary>
    public FrozenDictionary<string, string>? CustomAbbreviations { get; set; }

    /// <summary>
    /// Gets or sets whether to completely replace the default abbreviations with the custom ones.
    /// If false (default), custom abbreviations are merged with defaults (custom taking precedence).
    /// If true, only the abbreviations provided in CustomAbbreviations will be used.
    /// </summary>
    public bool ReplaceDefaultAbbreviations { get; set; } = false;
}