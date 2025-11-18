using System;
using System.Collections.Generic;
using System.Linq;

namespace AlfredBr;

public sealed class MenuItem
{
    public MenuItem(string name, object? value = null, bool isSelected = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }

        Name = name;
        Value = value;
        IsSelected = isSelected;
    }

    public string Name { get; }

    public object? Value { get; }

    public bool IsSelected { get; set; }
}

public enum MenuExitReason
{
    Confirmed,
    Escape,
    Quit
}

public sealed record MenuSelectionResult(
    IReadOnlyList<MenuItem> SelectedItems,
    MenuExitReason Reason
)
{
    public MenuItem? PrimaryItem => SelectedItems.Count > 0 ? SelectedItems[0] : null;

    public IEnumerable<object?> Values => SelectedItems.Select(item => item.Value ?? item.Name);
}

public static partial class TinyTui
{
    public static MenuSelectionResult ShowMenu2(
        string? prompt,
        IReadOnlyList<MenuItem> menuItems,
        bool multiSelect = false,
        ConsoleColor? promptColor = null,
        ConsoleColor? highlightColor = null,
        ConsoleColor? selectionColor = null
    )
    {
        if (menuItems is null)
        {
            throw new ArgumentNullException(nameof(menuItems));
        }

        if (menuItems.Count == 0)
        {
            throw new ArgumentException("Menu must contain at least one item.", nameof(menuItems));
        }

        var session = new MenuSession(
            prompt,
            menuItems,
            multiSelect,
            promptColor ?? ConsoleColor.Gray,
            highlightColor ?? ConsoleColor.White,
            selectionColor ?? ConsoleColor.Cyan
        );

        return session.Run();
    }

    private sealed class MenuSession
    {
        private readonly IReadOnlyList<MenuItem> _items;
        private readonly bool _multiSelect;
        private readonly string _prompt;
        private readonly ConsoleColor _promptColor;
        private readonly ConsoleColor _highlightColor;
        private readonly ConsoleColor _selectionColor;
        private ConsoleColor _originalColor;
        private bool? _previousCursorVisibility;
        private int _cursorIndex;
        private int _menuTopRow;

        public MenuSession(
            string? prompt,
            IReadOnlyList<MenuItem> items,
            bool multiSelect,
            ConsoleColor promptColor,
            ConsoleColor highlightColor,
            ConsoleColor selectionColor
        )
        {
            _prompt = string.IsNullOrWhiteSpace(prompt)
                ? (multiSelect ? "Select one or more options:" : "Select an option:")
                : prompt.Trim();
            _items = items;
            _multiSelect = multiSelect;
            _promptColor = promptColor;
            _highlightColor = highlightColor;
            _selectionColor = selectionColor;
        }

        public MenuSelectionResult Run()
        {
            CaptureConsoleState();

            try
            {
                Console.WriteLine();
                Console.ForegroundColor = _promptColor;
                Console.WriteLine(_prompt);
                Console.ForegroundColor = _originalColor;

                _menuTopRow = Console.CursorTop;
                RenderMenu();

                return EventLoop();
            }
            finally
            {
                RestoreConsoleState();
            }
        }

        private void CaptureConsoleState()
        {
            _originalColor = Console.ForegroundColor;
            if (!OperatingSystem.IsWindows())
            {
                _previousCursorVisibility = null;
                return;
            }

            try
            {
                _previousCursorVisibility = Console.CursorVisible;
                Console.CursorVisible = false;
            }
            catch
            {
                _previousCursorVisibility = null;
            }
        }

        private void RestoreConsoleState()
        {
            Console.ForegroundColor = _originalColor;
            if (_previousCursorVisibility.HasValue && OperatingSystem.IsWindows())
            {
                try
                {
                    Console.CursorVisible = _previousCursorVisibility.Value;
                }
                catch
                {
                    // Ignore failures when the host does not allow cursor visibility changes.
                }
            }

            Console.SetCursorPosition(0, _menuTopRow + _items.Count + 2);
            Console.WriteLine();
        }

        private MenuSelectionResult EventLoop()
        {
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                switch (key.Key)
                {
                    case ConsoleKey.DownArrow:
                        MoveCursor(1);
                        break;
                    case ConsoleKey.UpArrow:
                        MoveCursor(-1);
                        break;
                    case ConsoleKey.Spacebar when _multiSelect:
                        ToggleSelection();
                        break;
                    case ConsoleKey.Enter:
                        return BuildResult(MenuExitReason.Confirmed);
                    case ConsoleKey.Escape:
                        return BuildResult(MenuExitReason.Escape);
                    case ConsoleKey.Q:
                        return BuildResult(MenuExitReason.Quit);
                    default:
                        continue;
                }

                RenderMenu();
            }
        }

        private void MoveCursor(int delta)
        {
            _cursorIndex = Math.Clamp(_cursorIndex + delta, 0, _items.Count - 1);
        }

        private void ToggleSelection()
        {
            var current = _items[_cursorIndex];
            current.IsSelected = !current.IsSelected;
        }

        private MenuSelectionResult BuildResult(MenuExitReason reason)
        {
            if (reason != MenuExitReason.Confirmed)
            {
                return new MenuSelectionResult(Array.Empty<MenuItem>(), reason);
            }

            if (_multiSelect)
            {
                var selected = _items.Where(item => item.IsSelected).ToArray();
                if (selected.Length > 0)
                {
                    return new MenuSelectionResult(selected, reason);
                }
            }

            return new MenuSelectionResult(new[] { _items[_cursorIndex] }, reason);
        }

        private void RenderMenu()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                Console.SetCursorPosition(0, _menuTopRow + i);
                Console.Write("\r");
                TinyTui.ClearLine();
                Console.SetCursorPosition(0, _menuTopRow + i);

                bool isCurrent = i == _cursorIndex;
                Console.ForegroundColor = isCurrent ? _highlightColor : _originalColor;

                Console.Write(isCurrent ? "> " : "  ");
                if (_multiSelect)
                {
                    Console.ForegroundColor = _selectionColor;
                    Console.Write(_items[i].IsSelected ? "[x] " : "[ ] ");
                    Console.ForegroundColor = isCurrent ? _highlightColor : _originalColor;
                }

                Console.Write(_items[i].Name);
            }

            int instructionsRow = _menuTopRow + _items.Count;
            Console.SetCursorPosition(0, instructionsRow);
            Console.Write("\r");
            TinyTui.ClearLine();
            Console.SetCursorPosition(0, instructionsRow);
            Console.ForegroundColor = _originalColor;
            Console.Write(
                _multiSelect
                    ? "Enter=Confirm  Space=Toggle  Esc=Cancel  Q=Quit"
                    : "Enter=Confirm  Esc=Cancel  Q=Quit"
            );
        }
    }
}
