namespace AlfredBr;

public static class Program
{
    public static void Main()
    {
        TinyTui.Startup();
        TinyTui.ClearScreen();

        var windowSize = TinyTui.GetWindowSize();

        TinyTui.ShowBox(new[] { "This is a TinyTui simple box.", "Just text in a simple box." });
        TinyTui.ShowBox(
            x: windowSize.Columns - 39,
            y: 1,
            width: 40,
            height: 4,
            title: "Box Title",
            content: new[] { "This is an advanced box.", "With more text and positioning." },
            borderColor: TinyTui.AnsiColor.Cyan,
            brightBorder: false,
            textColor: TinyTui.AnsiColor.Yellow,
            brightText: true
        );

        TinyTui.Goto(15, 1);

        var result = TinyTui.ShowMenu2(
            "You are using the TinyTui Menu.  Choose an option below:",
            new List<TinyTui.MenuItem>
            {
                new TinyTui.MenuItem("Show a Simple Box", 1),
                new TinyTui.MenuItem("Show an Advanced Box", 2),
                new TinyTui.MenuItem("Show a Spinner", 3),
                new TinyTui.MenuItem("Exit the program", 0)
            },
            multiSelect: false,
            promptColor: ConsoleColor.DarkYellow,
            highlightColor: ConsoleColor.Cyan,
            selectionColor: ConsoleColor.Yellow
        );
        Console.WriteLine(
            $"You selected: '{result.PrimaryItem?.Name}' with value: '{result.PrimaryItem?.Value}'"
        );

        TinyTui.Cleanup();
    }
}
