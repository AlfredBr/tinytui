namespace AlfredBr;

public static class Program
{
    public static void Main()
    {
        int menuSelection = 1;

        TinyTui.Startup();

        while (menuSelection != 0)
        {
            TinyTui.ClearScreen();

            var windowSize = TinyTui.GetWindowSize();

            switch (menuSelection)
            {
                case 1:
                    SimpleBox();
                    break;
                case 2:
                    AdvancedBox();
                    break;
                case 3:
                    Spinner();
                    break;
            }

            TinyTui.Goto(15, 1);

            var result = TinyTui.ShowMenu(
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
            menuSelection = (int)(result.PrimaryItem?.Value ?? 0);
        }
        //Console.WriteLine($"You selected: '{result.PrimaryItem?.Name}' with value: '{result.PrimaryItem?.Value}'");
        TinyTui.Cleanup();
    }

    private static void SimpleBox()
    {
        TinyTui.HomeCursor();
        TinyTui.ShowBox(new[] { "This is a TinyTui simple box.", "Just text in a simple box." });
    }

    private static void AdvancedBox()
    {
        TinyTui.HomeCursor();
        var windowSize = TinyTui.GetWindowSize();

        TinyTui.ShowBox(
            x: windowSize.Columns - 39,
            y: 1,
            width: 40,
            height: 4,
            title: "Box Title",
            content: new[] { "This is an advanced box.", "With more text and positioning." },
            borderColor: TinyTui.AnsiColor.Cyan,
            brightBorder: false,
            textColor: TinyTui.AnsiColor.White,
            brightText: true,
            titleColor: TinyTui.AnsiColor.Yellow,
            brightTitle: false
        );
    }

    private static void Spinner()
    {
        TinyTui.Spinner(
            () =>
            {
                // Simulate some work
                System.Threading.Thread.Sleep(3000);
            },
            "Processing with spinner..."
        );
    }
}
