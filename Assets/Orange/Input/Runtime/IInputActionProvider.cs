using UnityEngine.InputSystem;

namespace Orange.Input
{
    public interface IInputActionProvider
    {
        InputActionAsset ActionsAsset { get; }

        bool TryFindAction(string actionPath, out InputAction action);

        string SaveBindingOverrides();

        void LoadBindingOverrides(string overridesJson);

        void ClearBindingOverrides();
    }
}
