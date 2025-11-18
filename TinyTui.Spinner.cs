public static partial class TinyTui
{
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
}
