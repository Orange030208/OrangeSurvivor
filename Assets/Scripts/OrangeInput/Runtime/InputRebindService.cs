using System;
using UnityEngine.InputSystem;

namespace Orange.Input
{
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

    public readonly struct InputRebindEntry
    {
        public InputRebindEntry(string actionPath, string compositePartName, string label, string controlScheme)
            : this(actionPath, compositePartName, label, controlScheme, null, null, null)
        {
        }

        public InputRebindEntry(
            string actionPath,
            string compositePartName,
            string label,
            string controlScheme,
            string bindingGroup,
            string requiredControlPath)
            : this(actionPath, compositePartName, label, controlScheme, bindingGroup, requiredControlPath, null)
        {
        }

        public InputRebindEntry(
            string actionPath,
            string compositePartName,
            string label,
            string controlScheme,
            string bindingGroup,
            string requiredControlPath,
            string[] cancelControlPaths)
        {
            ActionPath = actionPath;
            CompositePartName = compositePartName;
            Label = label;
            ControlScheme = controlScheme;
            BindingGroup = bindingGroup;
            RequiredControlPath = requiredControlPath;
            CancelControlPaths = cancelControlPaths ?? Array.Empty<string>();
        }

        public string ActionPath { get; }
        public string CompositePartName { get; }
        public string Label { get; }
        public string ControlScheme { get; }
        public string BindingGroup { get; }
        public string RequiredControlPath { get; }
        public string[] CancelControlPaths { get; }
        public string DisplayLabel => $"{Label} ({ControlScheme})";
    }

    public static class InputRebindService
    {
        public static InputRebindOperation StartInteractiveRebind(
            IInputActionProvider input,
            InputRebindEntry entry,
            Action<InputRebindResult, string> completed)
        {
            if (input == null || !input.TryFindAction(entry.ActionPath, out InputAction action))
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

            InputActionRebindingExtensions.RebindingOperation operation = action.PerformInteractiveRebinding(bindingIndex);
            string requiredControlPath = ResolveRequiredControlPath(entry);
            if (!string.IsNullOrWhiteSpace(requiredControlPath))
            {
                operation.WithControlsHavingToMatchPath(requiredControlPath);
            }

            string[] cancelControlPaths = entry.CancelControlPaths;
            bool hasCancelControlPath = false;
            for (int i = 0; i < cancelControlPaths.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(cancelControlPaths[i]))
                {
                    if (string.Equals(cancelControlPaths[i], "<Keyboard>/escape", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    operation.WithCancelingThrough(cancelControlPaths[i]);
                    hasCancelControlPath = true;
                }
            }

            if (!hasCancelControlPath && TryResolveDefaultCancelControlPath(entry, out string fallbackCancelControlPath))
            {
                operation.WithCancelingThrough(fallbackCancelControlPath);
            }

            operation = operation.OnCancel(rebindOperation =>
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

        public static void ResetBinding(IInputActionProvider input, InputRebindEntry entry)
        {
            if (input == null || !input.TryFindAction(entry.ActionPath, out InputAction action))
            {
                return;
            }

            int bindingIndex = ResolveBindingIndex(action, entry);
            if (bindingIndex >= 0)
            {
                action.RemoveBindingOverride(bindingIndex);
            }
        }

        public static void ResetAll(IInputActionProvider input)
        {
            input?.ClearBindingOverrides();
        }

        public static string GetDisplayString(IInputActionProvider input, InputRebindEntry entry)
        {
            if (input == null || !input.TryFindAction(entry.ActionPath, out InputAction action))
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

        private static int ResolveBindingIndex(InputAction action, InputRebindEntry entry)
        {
            if (action == null)
            {
                return -1;
            }

            string group = ResolveBindingGroup(entry);
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
            if (string.IsNullOrWhiteSpace(group))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(binding.groups) &&
                   binding.groups.IndexOf(group, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool MatchesEntryDevice(InputRebindEntry entry, string controlPath)
        {
            if (string.IsNullOrWhiteSpace(controlPath))
            {
                return false;
            }

            string deviceName = ResolveRequiredDeviceName(ResolveRequiredControlPath(entry));
            return string.IsNullOrWhiteSpace(deviceName) ||
                   controlPath.IndexOf(deviceName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ResolveBindingGroup(InputRebindEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.BindingGroup))
            {
                return entry.BindingGroup;
            }

            return string.Empty;
        }

        private static string ResolveRequiredControlPath(InputRebindEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.RequiredControlPath))
            {
                return entry.RequiredControlPath;
            }

            return string.Empty;
        }

        private static bool TryResolveDefaultCancelControlPath(InputRebindEntry entry, out string cancelControlPath)
        {
            cancelControlPath = string.Empty;

            if (entry.ControlScheme.IndexOf("Gamepad", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cancelControlPath = "<Gamepad>/buttonEast";
                return true;
            }

            if (entry.ControlScheme.IndexOf("Keyboard", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cancelControlPath = "<Keyboard>/backspace";
                return true;
            }

            return false;
        }

        private static string ResolveRequiredDeviceName(string requiredControlPath)
        {
            if (string.IsNullOrWhiteSpace(requiredControlPath))
            {
                return string.Empty;
            }

            string trimmed = requiredControlPath.Trim();
            if (trimmed[0] == '<')
            {
                int closeIndex = trimmed.IndexOf('>');
                return closeIndex > 1 ? trimmed.Substring(1, closeIndex - 1) : string.Empty;
            }

            int slashIndex = trimmed.IndexOf('/');
            return slashIndex > 0 ? trimmed.Substring(0, slashIndex) : trimmed;
        }
    }
}
