using System.Collections.Generic;
using Orange.Input;

public static class GameInputRebindCatalog
{
    private static readonly InputRebindEntry[] entries =
    {
        new("Gameplay/Move", "Up", "Move Up", "Keyboard"),
        new("Gameplay/Move", "Down", "Move Down", "Keyboard"),
        new("Gameplay/Move", "Left", "Move Left", "Keyboard"),
        new("Gameplay/Move", "Right", "Move Right", "Keyboard"),
        new("Gameplay/Pause", null, "Pause", "Keyboard"),
        new("Gameplay/Pause", null, "Pause", "Gamepad"),
        new("UI/Submit", null, "UI Submit", "Keyboard"),
        new("UI/Submit", null, "UI Submit", "Gamepad"),
        new("UI/Cancel", null, "UI Cancel", "Keyboard"),
        new("UI/Cancel", null, "UI Cancel", "Gamepad")
    };

    public static IReadOnlyList<InputRebindEntry> Entries => entries;
}
