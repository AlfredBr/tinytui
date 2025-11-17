using System;
using System.IO;
using System.Threading;
using AlfredBr;

TinyTui.UseUtf8();

static void Cleanup()
{
    TinyTui.ResetStyle();
    TinyTui.ShowCursor();
    var endSize = TinyTui.GetWindowSize();
    TinyTui.Goto(Math.Max(1, endSize.Rows), 1);
    Console.WriteLine();
}

Console.CancelKeyPress += (sender, args) =>
{
    args.Cancel = true;
    Cleanup();
    Environment.Exit(0);
};

TinyTui.HideCursor();

var windowSize = TinyTui.GetWindowSize();
var sampleProbe = TinyTui.ProbeSize(new StringReader("\u001B[24;80R"));

try
{
    TinyTui.ClearScreen();

    var header = " TinyTui demo ";
    if (header.Length < windowSize.Columns)
    {
        header = header.PadRight(windowSize.Columns);
    }

    TinyTui.SetStyle(null, TinyTui.AnsiStyle.Inverse);
    TinyTui.WriteAt(1, 1, header);
    TinyTui.ResetStyle();

    var row = 3;

    TinyTui.WriteAt(row++, 1, $"Console.Window => {windowSize.Columns}x{windowSize.Rows}");
    TinyTui.WriteAt(row++, 1, $"ProbeSize sample parse => {sampleProbe.Columns}x{sampleProbe.Rows} (simulated response)");
    if (!Console.IsInputRedirected)
    {
        TinyTui.WriteAt(row++, 1, "Run in a real terminal to call TinyTerminal.ProbeSize(Console.In) for live dimensions.");
    }

    row++;
    TinyTui.WriteAt(row++, 1, "Color palette:");
    foreach (TinyTui.AnsiColor color in Enum.GetValues(typeof(TinyTui.AnsiColor)))
    {
        TinyTui.WriteAt(row, 2, $"{color,-7}: ");
        TinyTui.SetColors(foreground: color);
        Console.Write("normal ");
        TinyTui.SetColors(foreground: color, brightForeground: true);
        Console.Write("bright");
        TinyTui.ResetStyle();
        row++;
    }

    row++;
    TinyTui.WriteAt(row++, 1, "Background colors:");
    TinyTui.SetColors(foreground: TinyTui.AnsiColor.White, background: TinyTui.AnsiColor.Blue);
    Console.Write(" standard bg ");
    TinyTui.SetColors(foreground: TinyTui.AnsiColor.Black, background: TinyTui.AnsiColor.Yellow, brightBackground: true);
    Console.Write(" bright bg ");
    TinyTui.ResetStyle();

    row += 2;
    TinyTui.WriteAt(row++, 1, "Styles:");
    TinyTui.SetStyle(null, TinyTui.AnsiStyle.Bold);
    Console.Write(" bold ");
    TinyTui.SetStyle(null, TinyTui.AnsiStyle.Underline);
    Console.Write(" underline ");
    TinyTui.SetStyle(null, TinyTui.AnsiStyle.Inverse);
    Console.Write(" inverse ");
    TinyTui.ResetStyle();

    row += 2;
    TinyTui.WriteAt(row, 1, "ClearLine demo: this disappears in 3 seconds...");
    Thread.Sleep(3000);
    TinyTui.ClearLine();
    TinyTui.WriteAt(row++, 1, "ClearLine demo complete.");

    TinyTui.WriteAt(row, 1, "Partial ClearLine demo >>> text");
    Thread.Sleep(5000);
    TinyTui.ClearLine(mode: TinyTui.LineClearMode.ToEnd);
    TinyTui.WriteAt(row++, 1, "Partial ClearLine demo complete.");

    row += 2;
    TinyTui.WriteAt(row++, 1, "Save/Restore cursor with progress:");
    TinyTui.SaveCursor();
    for (int step = 0; step <= 10; step++)
    {
        TinyTui.WriteAt(row - 1, 40, $"{step * 10,3}%");
        Thread.Sleep(300);
    }

    TinyTui.RestoreCursor();
    Console.Write(" cursor restored.");

    row += 3;
    TinyTui.WriteAt(row++, 1, "Relative moves:");
    TinyTui.Goto(row, 4);
    Console.Write("•");
    Thread.Sleep(1000);
    TinyTui.MoveRight(5);
    Console.Write("→");
    Thread.Sleep(1000);
    TinyTui.MoveDown(1);
    Console.Write("↓");
    Thread.Sleep(1000);
    TinyTui.MoveLeft(5);
    Console.Write("←");
    Thread.Sleep(1000);
    TinyTui.MoveUp(1);
    Console.Write("↑");

    row += 4;
    TinyTui.Goto(row, 1);
    TinyTui.Spinner(() => Thread.Sleep(5000), "Spinner demo");
    TinyTui.WriteAt(row + 1, 1, "Spinner demo complete.");
    row += 3;

    if (windowSize.Rows - row > 6)
    {
        int logTop = windowSize.Rows - 4;
        TinyTui.WriteAt(logTop - 1, 1, "Scroll region demo (last 4 rows):");
        TinyTui.SetScrollRegion(logTop, windowSize.Rows);
        TinyTui.Goto(logTop, 1);
        for (int i = 1; i <= 19; i++)
        {
            Console.WriteLine($"log message {i}");
            Thread.Sleep(100);
        }
        TinyTui.ResetScrollRegion();
    }
    else
    {
        TinyTui.WriteAt(row++, 1, "Resize taller to see scroll region demo.");
    }

    TinyTui.WriteAt(windowSize.Rows, 1, "Press any key to exit...");
    TinyTui.ShowCursor();
    Console.ReadLine();
}
finally
{
    Cleanup();
}
