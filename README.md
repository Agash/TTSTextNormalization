# TTSTextNormalization

.NET library for normalizing user-generated text before Text-to-Speech playback (chat, donations, comments, alerts), so engines pronounce content more consistently.

[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/Agash/TTSTextNormalization/dotnet-publish.yml?style=flat-square&logo=github&logoColor=white)](https://github.com/Agash/TTSTextNormalization/actions)
[![NuGet Version](https://img.shields.io/nuget/v/Agash.TTSTextNormalization.svg?style=flat-square&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Agash.TTSTextNormalization/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)

## Targets

- `net10.0` (primary)
- `net9.0`

## Install

```bash
dotnet add package Agash.TTSTextNormalization
```

## What You Get

- Emoji normalization (including ZWJ sequences and flags)
- Currency normalization (`$10.50`, `EUR 100`, etc.) to spoken forms
- Number normalization (cardinal, ordinal, decimal, multi-dot/version-style)
- URL replacement via configurable placeholder text
- Abbreviation expansion for common chat/gaming terms (`lol`, `brb`, `gg`, ...)
- Basic sanitization of control chars and punctuation variants
- Cleanup for excessive punctuation and repeated letters
- Final whitespace and punctuation spacing normalization
- DI-first, ordered pipeline via `ITextNormalizationRule`

## Quick Start (DI)

```csharp
using Microsoft.Extensions.DependencyInjection;
using TTSTextNormalization.Abstractions;
using TTSTextNormalization.DependencyInjection;
using TTSTextNormalization.Rules;

ServiceCollection services = new();

services.Configure<UrlRuleOptions>(o => o.PlaceholderText = " website link ");
services.Configure<EmojiRuleOptions>(o => o.Suffix = "emoji");

services.AddTextNormalization(builder =>
{
    builder.AddBasicSanitizationRule();
    builder.AddEmojiRule();
    builder.AddCurrencyRule();
    builder.AddAbbreviationNormalizationRule();
    builder.AddNumberNormalizationRule();
    builder.AddExcessivePunctuationRule();
    builder.AddLetterRepetitionRule();
    builder.AddUrlNormalizationRule();
    builder.AddWhitespaceNormalizationRule();
});

ServiceProvider provider = services.BuildServiceProvider();
ITextNormalizer normalizer = provider.GetRequiredService<ITextNormalizer>();

string input = "OMG!!! that stream was 🔥🔥🔥 $10.50 www.example.com";
string output = normalizer.Normalize(input);
```

## Notes

- This library normalizes text only. It does not provide TTS playback itself.
- Rule ordering is configurable; defaults are designed for chat-like inputs.

## Build

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

## Contributing

PRs are welcome. If behavior changes, include tests in `TTSTextNormalization.Tests`.

## License

MIT. See `LICENSE.txt`.