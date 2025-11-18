namespace AlfredBr;

public static partial class TinyTui
{
    /// <summary>
    /// Draws a rectangular box using 1-based coordinates with an optional title and content.
    /// </summary>
    public static void ShowBox(
        int x,
        int y,
        int width,
        int height,
        string? title,
        IEnumerable<string>? content,
        TextWriter? writer = null,
        AnsiColor? borderColor = null,
        bool brightBorder = false,
        AnsiColor? textColor = null,
        bool brightText = false
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
        var lines = PrepareContentLines(content, innerWidth, innerHeight);

        RenderTopBorder(
            output,
            x,
            y,
            innerWidth,
            title,
            borderColor,
            brightBorder,
            textColor,
            brightText
        );

        for (int row = 0; row < innerHeight; row++)
        {
            string rawLine = row < lines.Count ? lines[row] : string.Empty;
            string padded = innerWidth > 0 ? rawLine.PadRight(innerWidth) : string.Empty;

            RenderContentLine(
                output,
                x,
                y + 1 + row,
                padded,
                innerWidth,
                borderColor,
                brightBorder,
                textColor,
                brightText
            );
        }

        RenderBottomBorder(output, x, y + height - 1, innerWidth, borderColor, brightBorder);
    }

    private static List<string> PrepareContentLines(
        IEnumerable<string>? content,
        int innerWidth,
        int innerHeight
    )
    {
        var lines = new List<string>();
        if (content is null || innerHeight <= 0)
        {
            return lines;
        }

        foreach (var entry in content)
        {
            var normalized = (entry ?? string.Empty).ReplaceLineEndings("\n").Split('\n');
            foreach (var segment in normalized)
            {
                if (innerWidth <= 0 || segment.Length == 0)
                {
                    lines.Add(string.Empty);
                }
                else
                {
                    int start = 0;
                    while (start < segment.Length)
                    {
                        int len = Math.Min(innerWidth, segment.Length - start);
                        lines.Add(segment.Substring(start, len));
                        start += len;
                        if (lines.Count >= innerHeight)
                        {
                            return lines;
                        }
                    }
                }

                if (lines.Count >= innerHeight)
                {
                    return lines;
                }
            }
        }

        return lines;
    }

    private static void RenderTopBorder(
        TextWriter output,
        int x,
        int y,
        int innerWidth,
        string? title,
        AnsiColor? borderColor,
        bool brightBorder,
        AnsiColor? textColor,
        bool brightText
    )
    {
        Goto(y, x, output);
        string? decoratedTitle = null;
        if (!string.IsNullOrWhiteSpace(title) && innerWidth > 0)
        {
            decoratedTitle = $" {title.Trim()} ".ReplaceLineEndings(" ");
            if (decoratedTitle.Length > innerWidth)
            {
                decoratedTitle = decoratedTitle[..innerWidth];
            }
        }

        int titleLength = decoratedTitle?.Length ?? 0;
        int leftLength = decoratedTitle is null
            ? innerWidth
            : Math.Max(0, (innerWidth - titleLength) / 2);
        int rightLength = decoratedTitle is null
            ? 0
            : Math.Max(0, innerWidth - titleLength - leftLength);

        WriteWithColor(output, "┌", borderColor, brightBorder);
        if (leftLength > 0)
        {
            WriteWithColor(output, new string('─', leftLength), borderColor, brightBorder);
        }

        if (decoratedTitle is not null)
        {
            WriteWithColor(output, decoratedTitle, borderColor, brightBorder);
        }

        if (rightLength > 0)
        {
            WriteWithColor(output, new string('─', rightLength), borderColor, brightBorder);
        }

        WriteWithColor(output, "┐", borderColor, brightBorder);
    }

    private static void RenderContentLine(
        TextWriter output,
        int x,
        int y,
        string line,
        int innerWidth,
        AnsiColor? borderColor,
        bool brightBorder,
        AnsiColor? textColor,
        bool brightText
    )
    {
        Goto(y, x, output);
        WriteWithColor(output, "│", borderColor, brightBorder);
        if (innerWidth > 0)
        {
            if (textColor.HasValue)
            {
                WriteWithColor(output, line, textColor, brightText);
            }
            else
            {
                output.Write(line);
            }
        }

        WriteWithColor(output, "│", borderColor, brightBorder);
    }

    private static void RenderBottomBorder(
        TextWriter output,
        int x,
        int y,
        int innerWidth,
        AnsiColor? borderColor,
        bool brightBorder
    )
    {
        Goto(y, x, output);
        WriteWithColor(output, "└", borderColor, brightBorder);
        if (innerWidth > 0)
        {
            WriteWithColor(output, new string('─', innerWidth), borderColor, brightBorder);
        }

        WriteWithColor(output, "┘", borderColor, brightBorder);
    }

    private static void WriteWithColor(
        TextWriter output,
        string text,
        AnsiColor? color,
        bool bright
    )
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (color.HasValue)
        {
            SetColors(output, color.Value, brightForeground: bright);
            output.Write(text);
            ResetStyle(output);
        }
        else
        {
            output.Write(text);
        }
    }

    public static void ShowBox(
        string[] contents,
        ConsoleColor lineColor = ConsoleColor.DarkYellow,
        ConsoleColor textColor = ConsoleColor.White
    )
    {
        if (contents == null || contents.Length == 0)
        {
            return;
        }

        contents = contents.Where(t => t?.Length > 0).ToArray();
        var lengths = contents.Select(t => t.Length).ToArray();

        Console.SetCursorPosition(0, Console.CursorTop);
        Console.ForegroundColor = lineColor;
        Console.Write("┌");
        for (var l = 0; l < lengths.Length - 1; l++)
        {
            var length = lengths[l];
            Console.Write("─".PadRight(length + 2, '─'));
            Console.Write("┬");
        }
        Console.Write("─".PadRight(lengths.Last() + 2, '─'));
        Console.WriteLine("┐");

        foreach (var content in contents)
        {
            Console.ForegroundColor = lineColor;
            Console.Write("│ ");
            Console.ForegroundColor = textColor;
            Console.Write(content);
            Console.ForegroundColor = lineColor;
            Console.Write(" ");
        }
        Console.WriteLine("│");

        Console.ForegroundColor = lineColor;
        Console.Write("└");
        for (var l = 0; l < lengths.Length - 1; l++)
        {
            var length = lengths[l];
            Console.Write("─".PadRight(length + 2, '─'));
            Console.Write("┴");
        }
        Console.Write("─".PadRight(lengths.Last() + 2, '─'));
        Console.WriteLine("┘");
        Console.ForegroundColor = ConsoleColor.White;
    }
}
