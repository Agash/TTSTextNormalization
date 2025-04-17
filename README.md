# TTSTextNormalization - Normalize Text for TTS

[![NuGet Version](https://img.shields.io/nuget/v/Agash.TTSTextNormalization.svg?style=flat-square)](https://www.nuget.org/packages/Agash.TTSTextNormalization/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/Agash/TTSTextNormalization/dotnet-publish.yml?branch=master&style=flat-square)](https://github.com/Agash/TTSTextNormalization/actions)

A .NET 9 / C# 13 class library designed to normalize text containing emojis, currency symbols, numbers, abbreviations, and other non-standard elements, making it suitable for consistent and natural-sounding Text-to-Speech (TTS) synthesis across different engines (e.g., System.Speech, KokoroSharp). Specifically tailored for scenarios involving user-generated content like Twitch/YouTube chat and donations.

## Problem Solved

TTS engines often struggle with or produce inconsistent results when encountering:

*   Emojis (e.g., ✨, 👍, 🇬🇧)
*   Currency symbols and codes from various locales (e.g., $, £, €, USD, JPY, BRL)
*   Different number formats (cardinals, ordinals, decimals, version numbers)
*   Common chat/gaming abbreviations and slang (e.g., lol, brb, gg, afk)
*   Excessive punctuation or letter repetitions (e.g., !!!, ???, sooooo)
*   URLs or non-standard characters

This library preprocesses input text using a configurable pipeline of rules to replace or adjust these elements *before* sending the text to the TTS engine, leading to a more predictable, consistent, and pleasant listening experience.

## Features

*   **Emoji Normalization:** Replaces Unicode emojis (including flags, ZWJ sequences) with descriptive text (e.g., ✨ -> `sparkles`, 👍 -> `thumbs up`, 🇬🇧 -> `flag United Kingdom`) using an up-to-date emoji dataset processed by a source generator.
*   **Currency Normalization:** Detects currency symbols and ISO codes known to the .NET runtime. Replaces amounts with spoken text using locale-aware mappings (e.g., `$10.50` -> `ten dollars fifty cents`, `€100` -> `one hundred euros`, `500 JPY` -> `five hundred yen`). Uses Humanizer for number-to-word conversion. Requires manual mapping for TTS spoken names per ISO code.
*   **Number Normalization:** Handles standalone cardinals ("123" -> `one hundred and twenty-three`), ordinals ("1st" -> `first`), decimals ("1.5" -> `one point five`), and basic version-like numbers ("1.2.3" -> `one point two point three`) using Humanizer and custom Regex.
*   **Abbreviation/Acronym Expansion:** Expands a comprehensive list of common chat, gaming, and streaming abbreviations (e.g., `lol` -> `laughing out loud`, `gg` -> `good game`, `afk` -> `away from keyboard`). Case-insensitive and whole-word matching.
*   **Basic Text Sanitization:** Normalizes line breaks, removes common problematic control/formatting characters, and replaces non-standard "fancy" punctuation (smart quotes, dashes, ellipsis) with ASCII equivalents.
*   **Chat Text Cleanup:**
    *   Reduces sequences of excessive punctuation (`!!!` -> `!`, `...` -> `.`, `???` -> `?`).
    *   Reduces excessive letter repetitions (`soooo` -> `soo`).
*   **Whitespace Normalization:** Trims leading/trailing whitespace, collapses multiple internal whitespace characters to a single space, and normalizes spacing around common punctuation (removes space before, ensures space after).
*   **Extensibility:** Designed around a pipeline of `ITextNormalizationRule` instances, easily configurable via Dependency Injection. Custom rules can be created by implementing the interface.
*   **Performance:** Optimized using modern .NET features like source generators (Regex, Emoji data), `FrozenDictionary` for lookups, and efficient string handling where possible.

## Technology

*   **C# 13 / .NET 9 (Preview)**: Leverages the latest language and runtime features.
*   **Source Generators:** Used for generating optimized Regex patterns and embedding up-to-date Emoji data at compile time.
*   **Humanizer:** Used for robust number-to-words and ordinal conversion.
*   **Core .NET Libraries:** `System.Text.RegularExpressions`, `System.Globalization`, `System.Collections.Frozen`, `System.Text.Json` (in generator).
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

    // ... other using statements

    var services = new ServiceCollection();

    // Configure the normalization pipeline with desired rules
    services.AddTextNormalization(builder =>
    {
        // Add rules - order is managed internally by the 'Order' property of each rule (currently not yet configurable, opinionated)
        builder.AddBasicSanitizationRule();       // Runs first (Order 10)
        builder.AddEmojiRule();                   // Order 100
        builder.AddCurrencyRule();                // Order 200
        builder.AddAbbreviationNormalizationRule(); // Order 300
        builder.AddNumberNormalizationRule();       // Order 400
        builder.AddExcessivePunctuationRule();    // Order 500
        builder.AddLetterRepetitionRule();        // Order 510
        // Add custom rules here if needed: builder.AddRule<MyCustomRule>();
        builder.AddWhitespaceNormalizationRule(); // Runs last (Order 9000)
    });

    // Register other services...

    // Build the provider
    var serviceProvider = services.BuildServiceProvider();
    ```

2.  **Use the Normalizer:**

    ```csharp
    // Get the normalizer instance from DI
    var normalizer = serviceProvider.GetRequiredService<ITextNormalizer>();

    // Example inputs
    string input1 = "  OMG!!! That stream was 🔥🔥🔥!! Costs $10... BRB. 1st place!! ";
    string input2 = "He said “hello” ‹you› sooo many times... 1.2.3 check";

    // Normalize
    string normalized1 = normalizer.Normalize(input1);
    string normalized2 = normalizer.Normalize(input2);

    // Output (approximate, based on current rules)
    Console.WriteLine(normalized1);
    // Output: oh my god! That stream was fire fire fire! Costs ten dollars. be right back. first place!

    Console.WriteLine(normalized2);
    // Output: He said "hello" 'you' soo many times. one point two point three check

    // Pass the normalized text to your TTS engine
    // MyTTSEngine.Speak(normalized1);
    // MyTTSEngine.Speak(normalized2);
    ```

## Building

Ensure you have the .NET 9 SDK (Preview) installed.

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