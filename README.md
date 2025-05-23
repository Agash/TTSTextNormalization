﻿# TTSTextNormalization - Normalize Text for TTS

[![NuGet Version](https://img.shields.io/nuget/v/Agash.TTSTextNormalization.svg?style=flat-square)](https://www.nuget.org/packages/Agash.TTSTextNormalization/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/Agash/TTSTextNormalization/dotnet-publish.yml?branch=master&style=flat-square)](https://github.com/Agash/TTSTextNormalization/actions)

A .NET 9 / C# 13 class library designed to normalize text containing emojis, currency symbols, numbers, URLs, abbreviations, and other non-standard elements, making it suitable for consistent and natural-sounding Text-to-Speech (TTS) synthesis across different engines (e.g., System.Speech, KokoroSharp). Specifically tailored for scenarios involving user-generated content like Twitch/YouTube chat and donations.

## Problem Solved

TTS engines often struggle with or produce inconsistent results when encountering:

*   Emojis (e.g., ✨, 👍, 🇬🇧)
*   Currency symbols and codes from various locales (e.g., $, £, €, USD, JPY, BRL)
*   Different number formats (cardinals, ordinals, decimals, version numbers)
*   Common chat/gaming abbreviations and slang (e.g., lol, brb, gg, afk)
*   URLs (e.g., https://example.com, www.test.org)
*   Excessive punctuation or letter repetitions (e.g., !!!, ???, sooooo)
*   Non-standard characters

This library preprocesses input text using a configurable pipeline of rules to replace or adjust these elements *before* sending the text to the TTS engine, leading to a more predictable, consistent, and pleasant listening experience.

## Features

*   **Emoji Normalization:** Replaces Unicode emojis (including flags, ZWJ sequences) with descriptive text (e.g., ✨ -> `sparkles`).
    *   *Configurable:* Add optional prefix/suffix (e.g., "emoji sparkles", "sparkles emoji") via `EmojiRuleOptions`.
*   **Currency Normalization:** Detects currency symbols and ISO codes. Replaces amounts with spoken text using locale-aware mappings (e.g., `$10.50` -> `ten US dollars fifty cents`). Uses Humanizer.
*   **Number Normalization:** Handles standalone cardinals ("123" -> `one hundred and twenty-three`), ordinals ("1st" -> `first`), decimals ("1.5" -> `one point five`), and version-like numbers ("1.2.3" -> `one point two point three`). Uses Humanizer.
*   **URL Normalization:** Replaces detected URLs (http, https, www) with a placeholder (default: " link ").
    *   *Configurable:* Specify custom placeholder text via `UrlRuleOptions`.
*   **Abbreviation/Acronym Expansion:** Expands a comprehensive list of common chat, gaming, and streaming abbreviations (e.g., `lol` -> `laughing out loud`). Case-insensitive and whole-word matching.
    *   *Configurable:* Add custom abbreviations or completely replace the default list via `AbbreviationRuleOptions`.
*   **Basic Text Sanitization:** Normalizes line breaks, removes common problematic control/formatting characters, and replaces non-standard "fancy" punctuation with ASCII equivalents.
*   **Chat Text Cleanup:**
    *   Reduces sequences of excessive punctuation (`!!!` -> `!`, `...` -> `.`, `???` -> `?`).
    *   Reduces excessive letter repetitions (`soooo` -> `soo`).
*   **Whitespace Normalization:** Trims leading/trailing whitespace, collapses multiple internal whitespace characters to a single space, and normalizes spacing around common punctuation.
*   **Extensibility & Configuration:**
    *   Designed around a pipeline of `ITextNormalizationRule` instances.
    *   Easily configurable via Dependency Injection using `AddTextNormalization`.
    *   Rule execution order can be overridden during registration.
    *   Specific rules offer configuration via the standard .NET Options pattern (`IOptions<T>`).
    *   Custom rules can be created by implementing the `ITextNormalizationRule` interface.
*   **Performance:** Optimized using modern .NET features like source generators (Regex, Emoji data), `FrozenDictionary` for lookups, `IOptions`, and efficient string handling where possible.

## Technology

*   **C# 13 / .NET 9**: Leverages the latest language and runtime features.
*   **Source Generators:** Used for generating optimized Regex patterns and embedding up-to-date Emoji data at compile time.
*   **Humanizer:** Used for robust number-to-words and ordinal conversion.
*   **Core .NET Libraries:** `System.Text.RegularExpressions`, `System.Globalization`, `System.Collections.Frozen`, `System.Text.Json` (in generator), `Microsoft.Extensions.Options`.
*   **Dependency Injection:** Designed for easy integration using `Microsoft.Extensions.DependencyInjection`.

## Getting Started

### Installation

```powershell
dotnet add package Agash.TTSTextNormalization
```
Or install `Agash.TTSTextNormalization` via the NuGet Package Manager in Visual Studio.

### Basic Usage with Dependency Injection (Recommended)

1.  **Configure Services (e.g., in `Program.cs` or `Startup.cs`):**

    ```csharp
    using Microsoft.Extensions.DependencyInjection;
    using TTSTextNormalization.Abstractions; // For ITextNormalizer
    using TTSTextNormalization.DependencyInjection; // For extension methods
    using TTSTextNormalization.Rules; // For rule options classes
    using System.Collections.Frozen; // For FrozenDictionary

    // ... other using statements

    var services = new ServiceCollection();

    // --- Configure Rule Options (Optional) ---
    services.Configure<AbbreviationRuleOptions>(options =>
    {
        // Example: Add custom abbreviations and override 'gg'
        var customMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "cya", "see you" },
            { "gg", "very good game" } // Overrides default
        };
        options.CustomAbbreviations = customMap.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        options.ReplaceDefaultAbbreviations = false; // Merge with defaults (default behavior)
    });

    services.Configure<UrlRuleOptions>(options =>
    {
        options.PlaceholderText = " website link "; // Use a custom placeholder
    });

    services.Configure<EmojiRuleOptions>(options =>
    {
        options.Suffix = "emoji"; // Append " emoji" to names, e.g., "thumbs up emoji"
    });


    // --- Configure the Normalization Pipeline ---
    services.AddTextNormalization(builder =>
    {
        // Add rules. Order is determined by default 'Order' property unless overridden.
        builder.AddBasicSanitizationRule();           // Order 10
        builder.AddEmojiRule();                       // Order 100 (Uses configured options)
        builder.AddCurrencyRule();                    // Order 200
        builder.AddAbbreviationNormalizationRule();   // Order 300 (Uses configured options)
        builder.AddNumberNormalizationRule();           // Order 400
        builder.AddExcessivePunctuationRule();        // Order 500
        builder.AddLetterRepetitionRule();            // Order 510
        builder.AddUrlNormalizationRule();            // Order 600 (Uses configured options)
        // Example: Add Whitespace rule but make it run earlier
        builder.AddWhitespaceNormalizationRule(orderOverride: 50); // Runs before Emoji now!
        // Add custom rules here: builder.AddRule<MyCustomRule>(orderOverride: 700);
    });

    // Register other services...
    // Add Logging if desired (pipeline logs information)
    // services.AddLogging(logBuilder => logBuilder.AddConsole());

    // Build the provider
    var serviceProvider = services.BuildServiceProvider();
    ```

2.  **Use the Normalizer:**

    ```csharp
    // Get the normalizer instance from DI
    var normalizer = serviceProvider.GetRequiredService<ITextNormalizer>();

    // Example inputs
    string input1 = "  OMG!!! That stream was 🔥🔥🔥!! CYA! Costs $10... Check www.example.com! ";
    string input2 = "He said “hello” ✨ gg.";

    // Normalize
    string normalized1 = normalizer.Normalize(input1);
    string normalized2 = normalizer.Normalize(input2);

    // Output (approximate, based on configured rules and options)
    Console.WriteLine(normalized1);
    // Output: oh my god! That stream was fire emoji fire emoji fire emoji! see you! Costs ten US dollars. Check website link!

    Console.WriteLine(normalized2);
    // Output: He said "hello" sparkles emoji very good game.

    // Pass the normalized text to your TTS engine
    // MyTTSEngine.Speak(normalized1);
    // MyTTSEngine.Speak(normalized2);
    ```

## Building

Ensure you have the .NET 9 SDK installed.

1.  Clone the repository:
    ```bash
    git clone https://github.com/Agash/TTSTextNormalization.git
    cd TTSTextNormalization
    ```
2.  Build the solution:
    ```bash
    dotnet build -c Release
    ```

## Contributing

Contributions are welcome! Please open an issue first to discuss potential changes or bug fixes. If submitting a pull request, ensure tests pass and new features include corresponding tests.

## License

This project is licensed under the [MIT License](LICENSE.txt).