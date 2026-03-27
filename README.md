# Notari

A word processor for writers who care about sound. Notari shows rhymes, synonyms, assonance, and more in a live sidebar as you write — powered by a built-in phonetic database, no internet required.

No AI. No autocomplete. Just you and the right tools to find the words yourself.

![Notari](https://github.com/dvhm/notari/raw/main/screenshot.png)

---

## Features

### Live Phonetic Sidebar
The sidebar updates automatically as you move your cursor, showing results for the word you're on.

**Phonetic**
- Perfect rhymes, near rhymes, multisyllabic rhymes, homophones
- Assonance (matching vowel sounds)
- Alliteration (matching consonant sounds)

**Semantic**
- Synonyms, antonyms, hypernyms, hyponyms — grouped by part of speech

Every result shows a **syllable count** that fills in progressively. Results can be sorted by word frequency (Zipf score) and capped to a result limit.

### Editor
- Rich text editing with spell check
- Ctrl+Scroll to zoom (25–400%)
- Typewriter mode (keeps the caret vertically centered)
- Autosave with configurable interval
- Bracket dimming to reduce visual noise

### Settings
Accent color, result limits, sort order, sidebar section visibility, debounce timing, and more — all accessible from `Ctrl+,`.

---

## Requirements

- Windows 10 or later (x64)
- No .NET installation needed — the installer is self-contained

---

## Installation

Download `Notari-Setup-v1.0.0-win-x64.exe` from the [latest release](https://github.com/dvhm/notari/releases/latest) and run it.

A portable zip is also available if you prefer not to use an installer.

---

## Keyboard Shortcuts

| Action | Shortcut |
|---|---|
| New | `Ctrl+N` |
| Open | `Ctrl+O` |
| Save | `Ctrl+S` |
| Save As | `Ctrl+Shift+S` |
| Settings | `Ctrl+,` |
| Zoom | `Ctrl+Scroll` |

---

## Building from Source

**Prerequisites:** .NET 10 SDK, Visual Studio 2022+ or Rider

```powershell
git clone https://github.com/dvhm/notari
cd notari/Notari
dotnet build
dotnet run
```

**To build a release installer:**

1. Install [Inno Setup 6](https://jrsoftware.org/isdl.php)
2. Run from the repo root:
   ```powershell
   .\publish.ps1
   ```
   Outputs a setup `.exe` and a portable `.zip` in `publish\`.

---

## License

GPLv3 — see [LICENSE](LICENSE).

---

*If you find Notari useful, a ⭐ on GitHub goes a long way.*
