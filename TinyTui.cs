using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace AlfredBr;

/// <summary>
/// Tiny helper for emitting common ANSI/VT100 control sequences from C#.
/// </summary>
public static class TinyTui
{
    private const string Csi = "\u001B["; // Control Sequence Introducer

    /// <summary>
    /// Ensures the console streams speak UTF-8 so escape sequences pass through untouched.
    /// Call once near startup.
    /// </summary>
    public static void UseUtf8()
    {
        if (Console.OutputEncoding != Encoding.UTF8)
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        if (Console.InputEncoding != Encoding.UTF8)
        {
            Console.InputEncoding = Encoding.UTF8;
        }
    }

    /// <summary>
    /// Clears the entire screen and optionally homes the cursor.
    /// </summary>
    public static void ClearScreen(TextWriter? writer = null, bool home = true)
    {
        Write(Csi + "2J", writer);
        if (home)
        {
            Write(Csi + "H", writer);
        }
    }

    /// <summary>
    /// Clears the active line (default: whole line = ESC[2K).
    /// </summary>
    public static void ClearLine(TextWriter? writer = null, LineClearMode mode = LineClearMode.Full)
    {
        Write($"{Csi}{(int)mode}K", writer);
    }

    /// <summary>
    /// Moves the cursor to an absolute position (1-based row and column).
    /// </summary>
    public static void Goto(int row, int column, TextWriter? writer = null)
    {
        Write($"{Csi}{row};{column}H", writer);
    }

    public static void MoveUp(int rows = 1, TextWriter? writer = null) => Move("A", rows, writer);
    public static void MoveDown(int rows = 1, TextWriter? writer = null) => Move("B", rows, writer);
    public static void MoveRight(int columns = 1, TextWriter? writer = null) => Move("C", columns, writer);
    public static void MoveLeft(int columns = 1, TextWriter? writer = null) => Move("D", columns, writer);

    /// <summary>
    /// Writes text at the given row/column without altering other content.
    /// </summary>
    public static void WriteAt(int row, int column, string text, TextWriter? writer = null)
    {
        Goto(row, column, writer);
        Write(text, writer);
    }

    /// <summary>
    /// Saves and restores the cursor position (works on modern terminals).
    /// </summary>
    public static void SaveCursor(TextWriter? writer = null) => Write(Csi + "s", writer);

    public static void RestoreCursor(TextWriter? writer = null) => Write(Csi + "u", writer);

    public static void HideCursor(TextWriter? writer = null) => Write(Csi + "?25l", writer);

    public static void ShowCursor(TextWriter? writer = null) => Write(Csi + "?25h", writer);

    /// <summary>
    /// Sets the scrolling region so that scrolls stay within a sub-rectangle.
    /// </summary>
    public static void SetScrollRegion(int topRow, int bottomRow, TextWriter? writer = null)
    {
        Write($"{Csi}{topRow};{bottomRow}r", writer);
    }

    /// <summary>
    /// Resets the scrolling region to the full screen.
    /// </summary>
    public static void ResetScrollRegion(TextWriter? writer = null) => Write(Csi + "r", writer);

    /// <summary>
    /// Applies zero or more Select Graphic Rendition (SGR) attributes; pass none to reset.
    /// </summary>
    public static void SetStyle(TextWriter? writer = null, params AnsiStyle[] styles)
    {
        if (styles is null || styles.Length == 0)
        {
            Write(Csi + "0m", writer);
            return;
        }

        Span<int> codes = styles.Length <= 8 ? stackalloc int[styles.Length] : new int[styles.Length];
        for (int i = 0; i < styles.Length; i++)
        {
            codes[i] = (int)styles[i];
        }

        Write($"{Csi}{string.Join(';', codes.ToArray())}m", writer);
    }

    /// <summary>
    /// Applies a foreground and/or background color. Pass null to leave either unchanged.
    /// Includes the bright color variants when requested.
    /// </summary>
    public static void SetColors(TextWriter? writer = null, AnsiColor? foreground = null, bool brightForeground = false, AnsiColor? background = null, bool brightBackground = false)
    {
        Span<int> parts = stackalloc int[4];
        int count = 0;

        if (foreground.HasValue)
        {
            parts[count++] = (brightForeground ? 90 : 30) + (int)foreground.Value;
        }

        if (background.HasValue)
        {
            parts[count++] = (brightBackground ? 100 : 40) + (int)background.Value;
        }

        if (count == 0)
        {
            SetStyle(writer); // resets
            return;
        }

        Write($"{Csi}{string.Join(';', parts[..count].ToArray())}m", writer);
    }

    /// <summary>
    /// Resets all SGR attributes (colors, bold, underline, etc.).
    /// </summary>
    public static void ResetStyle(TextWriter? writer = null) => Write(Csi + "0m", writer);

    /// <summary>
    /// Returns the current logical window size reported by Console.
    /// </summary>
    public static TerminalSize GetWindowSize() => new(Console.WindowWidth, Console.WindowHeight);

    /// <summary>
    /// Writes the "9999" probe and reads a device status response to discover the screen size.
    /// Reader should be tied to the terminal and already in a state where replies flow through.
    /// On Windows consoles, we temporarily disable echo/line input so the reply isn't printed.
    /// </summary>
    public static TerminalSize ProbeSize(TextReader reader, TextWriter? writer = null)
    {
        // Ensure the probe is sent before we start reading a reply.
        Write(Csi + "9999;9999H", writer);
        Write(Csi + "6n", writer);
        if (writer is not null)
        {
            writer.Flush();
        }
        else
        {
            Console.Out.Flush();
        }

        using var _ = WindowsInputModeScope.EnterForProbe();

        var buffer = new StringBuilder();
        while (true)
        {
            int ch = reader.Read();
            if (ch < 0)
            {
                break;
            }

            buffer.Append((char)ch);
            if (ch == 'R')
            {
                break;
            }
        }

        if (TryParseCursorResponse(buffer.ToString(), out int rows, out int columns))
        {
            return new TerminalSize(columns, rows);
        }

        return GetWindowSize();
    }

    private static bool TryParseCursorResponse(string response, out int rows, out int columns)
    {
        rows = 0;
        columns = 0;

        int start = response.IndexOf('[');
        int end = response.IndexOf('R');
        if (start == -1 || end == -1 || end <= start)
        {
            return false;
        }

        var payload = response.Substring(start + 1, end - start - 1);
        var parts = payload.Split(';');
        if (parts.Length != 2)
        {
            return false;
        }

        return int.TryParse(parts[0], out rows) && int.TryParse(parts[1], out columns);
    }

    private static void Move(string code, int amount, TextWriter? writer)
    {
        if (amount <= 0)
        {
            return;
        }

        Write($"{Csi}{amount}{code}", writer);
    }

    private static void Write(string data, TextWriter? writer)
    {
        (writer ?? Console.Out).Write(data);
    }

    public readonly record struct TerminalSize(int Columns, int Rows);

    public enum AnsiColor
    {
        Black = 0,
        Red = 1,
        Green = 2,
        Yellow = 3,
        Blue = 4,
        Magenta = 5,
        Cyan = 6,
        White = 7
    }

    public enum AnsiStyle
    {
        Reset = 0,
        Bold = 1,
        Dim = 2,
        Italic = 3,
        Underline = 4,
        Blink = 5,
        Inverse = 7,
        Hidden = 8,
        Strike = 9
    }

    public enum LineClearMode
    {
        ToEnd = 0,
        ToStart = 1,
        Full = 2
    }

    // Windows-specific helper to temporarily disable echo/line input and
    // enable VT input so DSR replies don't get echoed to the screen.
    private sealed class WindowsInputModeScope : IDisposable
    {
        private readonly nint _hIn;
        private readonly uint _prevMode;
        private readonly bool _changed;

        private WindowsInputModeScope(nint hIn, uint prevMode, bool changed)
        {
            _hIn = hIn;
            _prevMode = prevMode;
            _changed = changed;
        }

        public static WindowsInputModeScope EnterForProbe()
        {
            if (!OperatingSystem.IsWindows())
            {
                return new WindowsInputModeScope(nint.Zero, 0, false);
            }

            try
            {
                nint hIn = GetStdHandle(STD_INPUT_HANDLE);
                if (hIn == nint.Zero)
                {
                    return new WindowsInputModeScope(nint.Zero, 0, false);
                }

                if (!GetConsoleMode(hIn, out uint mode))
                {
                    return new WindowsInputModeScope(nint.Zero, 0, false);
                }

                uint newMode = mode | ENABLE_VIRTUAL_TERMINAL_INPUT;
                newMode &= ~(ENABLE_ECHO_INPUT | ENABLE_LINE_INPUT);

                if (newMode != mode)
                {
                    SetConsoleMode(hIn, newMode);
                    return new WindowsInputModeScope(hIn, mode, true);
                }

                return new WindowsInputModeScope(hIn, mode, false);
            }
            catch
            {
                return new WindowsInputModeScope(nint.Zero, 0, false);
            }
        }

        public void Dispose()
        {
            if (_changed && _hIn != nint.Zero)
            {
                try { SetConsoleMode(_hIn, _prevMode); } catch { /* ignore */ }
            }
        }

        private const int STD_INPUT_HANDLE = -10;
        private const uint ENABLE_ECHO_INPUT = 0x0004;
        private const uint ENABLE_LINE_INPUT = 0x0002;
        private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(nint hConsoleHandle, uint dwMode);
    }
}
