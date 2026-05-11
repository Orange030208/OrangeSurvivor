using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public enum InputRebindResult
{
    Success,
    Canceled,
    Conflict,
    InvalidTarget,
    Failed
}

public sealed class InputRebindOperation : IDisposable
{
    private readonly InputActionRebindingExtensions.RebindingOperation operation;

    public InputRebindOperation(InputActionRebindingExtensions.RebindingOperation operation)
    {
        this.operation = operation;
    }

    public void Cancel()
    {
        operation?.Cancel();
    }

    public void Dispose()
    {
        operation?.Dispose();
    }
}

public static class InputRebindService
{
    private static readonly RebindEntry[] rebindEntries =
    {
        new RebindEntry("Gameplay/Move", "Up", "Move Up", "Keyboard"),
        new RebindEntry("Gameplay/Move", "Down", "Move Down", "Keyboard"),
        new RebindEntry("Gameplay/Move", "Left", "Move Left", "Keyboard"),
        new RebindEntry("Gameplay/Move", "Right", "Move Right", "Keyboard"),
        new RebindEntry("Gameplay/Pause", null, "Pause", "Keyboard"),
        new RebindEntry("Gameplay/Pause", null, "Pause", "Gamepad"),
        new RebindEntry("UI/Submit", null, "UI Submit", "Keyboard"),
        new RebindEntry("UI/Submit", null, "UI Submit", "Gamepad"),
        new RebindEntry("UI/Cancel", null, "UI Cancel", "Keyboard"),
        new RebindEntry("UI/Cancel", null, "UI Cancel", "Gamepad")
    };

    public static IReadOnlyList<RebindEntry> RebindEntries => rebindEntries;

    public static InputRebindOperation StartInteractiveRebind(
        GameInputService inputService,
        RebindEntry entry,
        Action<InputRebindResult, string> completed)
    {
        if (inputService == null || !inputService.TryFindAction(entry.ActionPath, out InputAction action))
        {
            completed?.Invoke(InputRebindResult.InvalidTarget, "Invalid action.");
            return null;
        }

        int bindingIndex = ResolveBindingIndex(action, entry);
        if (bindingIndex < 0)
        {
            completed?.Invoke(InputRebindResult.InvalidTarget, "Invalid binding.");
            return null;
        }

        bool wasEnabled = action.enabled;
        if (wasEnabled)
        {
            action.Disable();
        }

        string requiredControlPath = entry.ControlScheme == "Gamepad" ? "<Gamepad>" : "<Keyboard>";
        InputActionRebindingExtensions.RebindingOperation operation = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsHavingToMatchPath(requiredControlPath)
            .WithCancelingThrough("<Keyboard>/escape")
            .WithCancelingThrough("<Gamepad>/buttonEast")
            .OnCancel(rebindOperation =>
            {
                if (wasEnabled)
                {
                    action.Enable();
                }

                rebindOperation.Dispose();
                completed?.Invoke(InputRebindResult.Canceled, "Canceled.");
            })
            .OnComplete(rebindOperation =>
            {
                if (wasEnabled)
                {
                    action.Enable();
                }

                string newPath = action.bindings[bindingIndex].effectivePath;
                if (!MatchesEntryDevice(entry, newPath))
                {
                    action.RemoveBindingOverride(bindingIndex);
                    rebindOperation.Dispose();
                    completed?.Invoke(InputRebindResult.InvalidTarget, "Binding does not match target device.");
                    return;
                }

                if (HasConflict(action, bindingIndex, newPath))
                {
                    action.RemoveBindingOverride(bindingIndex);
                    rebindOperation.Dispose();
                    completed?.Invoke(InputRebindResult.Conflict, "Binding already used by this action.");
                    return;
                }

                rebindOperation.Dispose();
                completed?.Invoke(InputRebindResult.Success, newPath);
            });

        operation.Start();
        return new InputRebindOperation(operation);
    }

    public static void ResetBinding(GameInputService inputService, RebindEntry entry)
    {
        if (inputService == null || !inputService.TryFindAction(entry.ActionPath, out InputAction action))
        {
            return;
        }

        int bindingIndex = ResolveBindingIndex(action, entry);
        if (bindingIndex >= 0)
        {
            action.RemoveBindingOverride(bindingIndex);
        }
    }

    public static void ResetAll(GameInputService inputService)
    {
        inputService?.ClearBindingOverrides();
    }

    public static string GetDisplayString(GameInputService inputService, RebindEntry entry)
    {
        if (inputService == null || !inputService.TryFindAction(entry.ActionPath, out InputAction action))
        {
            return "-";
        }

        int bindingIndex = ResolveBindingIndex(action, entry);
        return bindingIndex >= 0
            ? action.GetBindingDisplayString(bindingIndex, out _, out _)
            : "-";
    }

    public static bool HasConflict(InputAction action, int bindingIndex, string path)
    {
        if (action == null || string.IsNullOrWhiteSpace(path) || bindingIndex < 0)
        {
            return false;
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (i == bindingIndex)
            {
                continue;
            }

            InputBinding binding = action.bindings[i];
            if (binding.isComposite)
            {
                continue;
            }

            string otherPath = binding.effectivePath;
            if (string.Equals(otherPath, path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static int ResolveBindingIndex(InputAction action, RebindEntry entry)
    {
        if (action == null)
        {
            return -1;
        }

        string group = entry.ControlScheme == "Gamepad" ? "Gamepad" : "Keyboard&Mouse";
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.isComposite || !BindingMatchesGroup(binding, group))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(entry.CompositePartName))
            {
                if (binding.isPartOfComposite &&
                    string.Equals(binding.name, entry.CompositePartName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }

                continue;
            }

            if (!binding.isPartOfComposite)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool BindingMatchesGroup(InputBinding binding, string group)
    {
        return !string.IsNullOrWhiteSpace(binding.groups) &&
               binding.groups.IndexOf(group, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool MatchesEntryDevice(RebindEntry entry, string controlPath)
    {
        if (string.IsNullOrWhiteSpace(controlPath))
        {
            return false;
        }

        return entry.ControlScheme == "Gamepad"
            ? controlPath.IndexOf("Gamepad", StringComparison.OrdinalIgnoreCase) >= 0
            : controlPath.IndexOf("Keyboard", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public readonly struct RebindEntry
    {
        public RebindEntry(string actionPath, string compositePartName, string label, string controlScheme)
        {
            ActionPath = actionPath;
            CompositePartName = compositePartName;
            Label = label;
            ControlScheme = controlScheme;
        }

        public string ActionPath { get; }
        public string CompositePartName { get; }
        public string Label { get; }
        public string ControlScheme { get; }
        public string DisplayLabel => $"{Label} ({ControlScheme})";
    }
}
