using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

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

    #region Clear Screen/Line
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
    #endregion

    #region Cursor Movement
    /// <summary>
    /// Moves the cursor to an absolute position (1-based row and column).
    /// </summary>
    public static void Goto(int row, int column, TextWriter? writer = null)
    {
        Write($"{Csi}{row};{column}H", writer);
    }

    public static void MoveUp(int rows = 1, TextWriter? writer = null) => Move("A", rows, writer);

    public static void MoveDown(int rows = 1, TextWriter? writer = null) => Move("B", rows, writer);

    public static void MoveRight(int columns = 1, TextWriter? writer = null) =>
        Move("C", columns, writer);

    public static void MoveLeft(int columns = 1, TextWriter? writer = null) =>
        Move("D", columns, writer);

    /// <summary>
    /// Moves the cursor in the given direction by the given amount.
    /// </summary>
    /// <param name="code"></param>
    /// <param name="amount"></param>
    /// <param name="writer"></param>
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

    /// <summary>
    /// Writes text at the given row/column without altering other content.
    /// </summary>
    public static void WriteAt(int row, int column, string text, TextWriter? writer = null)
    {
        Goto(row, column, writer);
        Write(text, writer);
    }
    #endregion

    #region Cursor Save/Restore
    /// <summary>
    /// Saves and restores the cursor position (works on modern terminals).
    /// </summary>
    public static void SaveCursor(TextWriter? writer = null) => Write(Csi + "s", writer);

    public static void RestoreCursor(TextWriter? writer = null) => Write(Csi + "u", writer);

    public static void HideCursor(TextWriter? writer = null) => Write(Csi + "?25l", writer);

    public static void ShowCursor(TextWriter? writer = null) => Write(Csi + "?25h", writer);
    #endregion

    #region Scroll Region
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
    #endregion

    #region Styles and Colors
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

        Span<int> codes =
            styles.Length <= 8 ? stackalloc int[styles.Length] : new int[styles.Length];
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
    public static void SetColors(
        TextWriter? writer = null,
        AnsiColor? foreground = null,
        bool brightForeground = false,
        AnsiColor? background = null,
        bool brightBackground = false
    )
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
    #endregion

    #region Terminal Size
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

    /// <summary>
    /// Parses a cursor position response of the form ESC[row;columnR.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="rows"></param>
    /// <param name="columns"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Represents terminal dimensions in columns and rows.
    /// </summary>
    public readonly record struct TerminalSize(int Columns, int Rows);
    #endregion

    #region Enums
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
    #endregion

    #region Windows Input Mode Scope
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
                try
                {
                    SetConsoleMode(_hIn, _prevMode);
                }
                catch
                { /* ignore */
                }
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
    #endregion

    #region Spinner
    private static readonly string[] Symbols =
    {
        "⣾⣿",
        "⣽⣿",
        "⣻⣿",
        "⢿⣿",
        "⡿⣿",
        "⣟⣿",
        "⣯⣿",
        "⣷⣿",
        "⣿⣾",
        "⣿⣽",
        "⣿⣻",
        "⣿⢿",
        "⣿⡿",
        "⣿⣟",
        "⣿⣯",
        "⣿⣷"
    };

    /// <summary>
    /// Runs a synchronous action while showing an animated spinner.
    /// </summary>
    public static void Spinner(Action action, string label, TextWriter? writer = null)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        Spinner(
                () =>
                {
                    action();
                    return Task.CompletedTask;
                },
                label,
                writer
            )
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Runs an asynchronous operation while showing an animated spinner.
    /// </summary>
    public static Task Spinner(
        Func<Task> task,
        string label,
        TextWriter? writer = null,
        CancellationToken cancellationToken = default
    )
    {
        if (task is null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        return RunSpinnerAsync(task, label, writer, cancellationToken);
    }

    private static async Task RunSpinnerAsync(
        Func<Task> operation,
        string label,
        TextWriter? writer,
        CancellationToken cancellationToken
    )
    {
        if (label is null)
        {
            throw new ArgumentNullException(nameof(label));
        }

        var output = writer ?? Console.Out;
        bool canColor = ReferenceEquals(output, Console.Out);
        var originalColor = canColor ? Console.ForegroundColor : ConsoleColor.Gray;

        int lastFrameLength = 0;
        using var spinnerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var spinnerToken = spinnerCts.Token;

        var spinnerTask = Task.Run(
            async () =>
            {
                int index = 0;
                while (!spinnerToken.IsCancellationRequested)
                {
                    lastFrameLength = WriteFrame(output, label, Symbols[index], canColor);
                    index = (index + 1) % Symbols.Length;

                    try
                    {
                        await Task.Delay(100, spinnerToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            },
            spinnerToken
        );

        try
        {
            await operation();
        }
        finally
        {
            spinnerCts.Cancel();
            try
            {
                await spinnerTask;
            }
            catch (OperationCanceledException)
            {
                // expected when the spinner stops
            }
            finally
            {
                ClearFrame(output, lastFrameLength);
                if (canColor)
                {
                    Console.ForegroundColor = originalColor;
                }
            }
        }
    }

    private static int WriteFrame(TextWriter output, string label, string symbol, bool useColor)
    {
        if (useColor)
        {
            Console.ForegroundColor = ConsoleColor.Green;
        }

        output.Write("\r");
        output.Write(symbol);
        output.Write(' ');
        output.Write(label);
        output.Flush();

        return symbol.Length + 1 + label.Length;
    }

    private static void ClearFrame(TextWriter output, int length)
    {
        output.Write("\r");
        if (length > 0)
        {
            output.Write(new string(' ', length));
            output.Write("\r");
        }

        output.Flush();
    }
    #endregion
}
