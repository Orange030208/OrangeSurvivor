using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Orange.Input
{
    [CreateAssetMenu(menuName = "Orange/Input/Input Module Profile", fileName = "Input Module Profile")]
    public sealed class InputModuleProfile : ScriptableObject
    {
        [SerializeField] private InputActionAsset actionsAsset;
        [SerializeField] private string defaultContextId;
        [SerializeField] private InputContextDefinition[] contexts = Array.Empty<InputContextDefinition>();
        [SerializeField] private InputSystemUiActionPaths uiActionPaths = InputSystemUiActionPaths.Default;
        [SerializeField] private InputBindingOverrideStore bindingOverrideStore;

        public InputActionAsset ActionsAsset => actionsAsset;
        public string DefaultContextId => defaultContextId;
        public InputContextDefinition[] Contexts => contexts ?? Array.Empty<InputContextDefinition>();
        public InputSystemUiActionPaths UiActionPaths => uiActionPaths;
        public IInputBindingOverrideStore BindingOverrideStore => bindingOverrideStore;
    }

    [Serializable]
    public struct InputContextDefinition
    {
        [SerializeField] private string contextId;
        [SerializeField] private string[] actionMapNames;

        public InputContextDefinition(string contextId, string[] actionMapNames)
        {
            this.contextId = contextId;
            this.actionMapNames = actionMapNames ?? Array.Empty<string>();
        }

        public string ContextId => contextId;
        public string[] ActionMapNames => actionMapNames ?? Array.Empty<string>();
    }
}
