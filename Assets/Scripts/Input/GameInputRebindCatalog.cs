using System.Collections.Generic;
using Orange.Input;

public static class GameInputRebindCatalog
{
    private static readonly string[] keyboardCancelPaths = { "<Keyboard>/backspace" };
    private static readonly string[] gamepadCancelPaths = { "<Gamepad>/buttonEast" };

    private static readonly InputRebindEntry[] entries =
    {
        new("Gameplay/Move", "Up", "Move Up", "Keyboard", "Keyboard&Mouse", "<Keyboard>", keyboardCancelPaths),
        new("Gameplay/Move", "Down", "Move Down", "Keyboard", "Keyboard&Mouse", "<Keyboard>", keyboardCancelPaths),
        new("Gameplay/Move", "Left", "Move Left", "Keyboard", "Keyboard&Mouse", "<Keyboard>", keyboardCancelPaths),
        new("Gameplay/Move", "Right", "Move Right", "Keyboard", "Keyboard&Mouse", "<Keyboard>", keyboardCancelPaths),
        new("Gameplay/Pause", null, "Pause", "Gamepad", "Gamepad", "<Gamepad>", gamepadCancelPaths),
        new("UI/Submit", null, "UI Submit", "Keyboard", "Keyboard&Mouse", "<Keyboard>", keyboardCancelPaths),
        new("UI/Submit", null, "UI Submit", "Gamepad", "Gamepad", "<Gamepad>", gamepadCancelPaths),
        new("UI/Cancel", null, "UI Cancel", "Keyboard", "Keyboard&Mouse", "<Keyboard>", keyboardCancelPaths),
        new("UI/Cancel", null, "UI Cancel", "Gamepad", "Gamepad", "<Gamepad>", gamepadCancelPaths)
    };

    public static IReadOnlyList<InputRebindEntry> Entries => entries;
}
