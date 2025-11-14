# TinyTui
- Single-file ANSI/VT100 helper (`TinyTui.cs`) for quickly hacking together TUIs in C#/.NET.
- Provides cursor movement, clearing, scroll regions, color/style helpers, and a portable terminal-size probe (with a Windows console mode shim).
- Ships with `Program.cs`, a guided demo that exercises every API so you can see the escape sequences in action.
- Built collaboratively by AlfredBr and GitHub Copilot (yes, the AI helped assemble this tiny toolkit).

**Quick Start**
- `dotnet build` then `dotnet run` from the repo root to launch the demo; press any key to exit once the walkthrough finishes.
- Review `Program.cs` to see real usage patterns: UTF-8 setup, full-screen refresh, progress updates via save/restore, relative cursor hops, color palettes, and scroll-region logging.
- For integration elsewhere, copy `TinyTui.cs` into your project, call `TinyTui.UseUtf8()` once at startup, then compose the helpers (`ClearScreen`, `Goto`, `SetColors`, `SetStyle`, etc.) to render your UI.

**Feature Highlights**
- Cursor control: `Goto`, `MoveUp/Down/Left/Right`, `SaveCursor`, `RestoreCursor`, `HideCursor`, `ShowCursor`.
- Screen management: `ClearScreen`, `ClearLine` (full or partial), `SetScrollRegion`/`ResetScrollRegion` for log panes.
- Styling: `SetStyle` for SGR attributes (bold, underline, inverse, blink, and more) and `SetColors` for foreground/background (including bright variants).
- Terminal sizing: `GetWindowSize` for .NETs view and `ProbeSize` for the 9999/DSR trick; on Windows the library temporarily toggles console input modes so replies are captured cleanly.
- All helpers accept an optional `TextWriter` so you can direct output somewhere other than `Console.Out` when needed.

**Design Notes**
- Everything lives in the `AlfredBr` namespace; change it if you embed the file elsewhere.
- The implementation stays ANSI-centric—i.e. no hidden state, no heavyweight abstractions—so you can mix TinyTui calls with your own Console writes without any surprises.
- The code sticks to standard ASCII to avoid any terminal oddities; any additional glyphs or emojis are up to you.
