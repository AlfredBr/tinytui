using System;
using System.IO;

namespace AlfredBr;

public static partial class TinyTui
{
    /// <summary>
    /// Draws a rectangular input box using 1-based coordinates with an optional title and default text.
    /// Returns the entered text when confirmed with Enter; returns null when cancelled with Escape.
    /// </summary>
    public static string? InputBox(
        int x,
        int y,
        int width,
        int height,
        string? title,
        string? defaultText = null,
        TextWriter? writer = null,
        AnsiColor? borderColor = null,
        bool brightBorder = false,
        AnsiColor? textColor = null,
        bool brightText = false,
        AnsiColor? titleColor = null,
        bool? brightTitle = null
    )
    {
        if (x < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if (y < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        if (width < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var output = writer ?? Console.Out;
        int innerWidth = width - 2;
        int innerHeight = height - 2;
        if (innerWidth <= 0 || innerHeight <= 0)
        {
            return defaultText;
        }

        RenderTopBorder(output, x, y, innerWidth, title, borderColor, brightBorder, titleColor, brightTitle);

        for (int row = 0; row < innerHeight; row++)
        {
            RenderContentLine(
                output,
                x,
                y + 1 + row,
                new string(' ', innerWidth),
                innerWidth,
                borderColor,
                brightBorder,
                textColor,
                brightText
            );
        }

        RenderBottomBorder(output, x, y + height - 1, innerWidth, borderColor, brightBorder);

        string buffer = (defaultText ?? string.Empty).ReplaceLineEndings(" ");
        if (buffer.Length > innerWidth)
        {
            buffer = buffer[..innerWidth];
        }

        bool? previousCursorVisible = null;
        try
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    previousCursorVisible = Console.CursorVisible;
                    Console.CursorVisible = true;
                }
                catch
                {
                    previousCursorVisible = null;
                }
            }

            RenderInputLine(output, x, y, innerWidth, buffer, textColor, brightText);

            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        return buffer;
                    case ConsoleKey.Escape:
                        return null;
                    case ConsoleKey.Backspace:
                        if (buffer.Length > 0)
                        {
                            buffer = buffer[..^1];
                            RenderInputLine(output, x, y, innerWidth, buffer, textColor, brightText);
                        }
                        break;
                    default:
                        if (!char.IsControl(key.KeyChar) && buffer.Length < innerWidth)
                        {
                            buffer += key.KeyChar;
                            RenderInputLine(output, x, y, innerWidth, buffer, textColor, brightText);
                        }
                        break;
                }
            }
        }
        finally
        {
            if (previousCursorVisible.HasValue && OperatingSystem.IsWindows())
            {
                try
                {
                    Console.CursorVisible = previousCursorVisible.Value;
                }
                catch
                {
                    // Ignore restore failures.
                }
            }
        }
    }

    private static void RenderInputLine(
        TextWriter output,
        int x,
        int y,
        int innerWidth,
        string buffer,
        AnsiColor? textColor,
        bool brightText
    )
    {
        // Goto is (row, column)
        Goto(y + 1, x + 1, output);

        string visible = buffer.Length > innerWidth ? buffer[..innerWidth] : buffer;
        string padded = visible.PadRight(innerWidth);

        if (textColor.HasValue)
        {
            WriteWithColor(output, padded, textColor.Value, brightText);
        }
        else
        {
            output.Write(padded);
        }

        Goto(y + 1, x + 1 + visible.Length, output);
        output.Flush();
    }
}
