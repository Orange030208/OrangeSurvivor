using UnityEngine;
using UnityEngine.InputSystem;

namespace Orange.Input
{
    [DisallowMultipleComponent]
    public class InputActionRuntime : MonoBehaviour, IInputActionProvider
    {
        [SerializeField] private InputActionAsset actionsAsset;
        [SerializeField] private bool dontDestroyOnLoad = true;

        public InputActionAsset ActionsAsset => actionsAsset;

        protected virtual void Awake()
        {
            if (dontDestroyOnLoad && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        protected virtual void OnDisable()
        {
            actionsAsset?.Disable();
        }

        public virtual void Initialize()
        {
            if (actionsAsset == null)
            {
                Debug.LogError($"{nameof(InputActionRuntime)} on '{name}' requires an explicit {nameof(InputActionAsset)} reference.", this);
            }
        }

        public bool TryFindAction(string actionPath, out InputAction action)
        {
            action = null;
            if (actionsAsset == null || string.IsNullOrWhiteSpace(actionPath))
            {
                return false;
            }

            action = actionsAsset.FindAction(actionPath, throwIfNotFound: false);
            return action != null;
        }

        public string SaveBindingOverrides()
        {
            return actionsAsset != null ? actionsAsset.SaveBindingOverridesAsJson() : string.Empty;
        }

        public void LoadBindingOverrides(string overridesJson)
        {
            Initialize();
            if (actionsAsset == null)
            {
                return;
            }

            actionsAsset.RemoveAllBindingOverrides();
            if (!string.IsNullOrWhiteSpace(overridesJson))
            {
                actionsAsset.LoadBindingOverridesFromJson(overridesJson);
            }
        }

        public void ClearBindingOverrides()
        {
            actionsAsset?.RemoveAllBindingOverrides();
        }
    }
}
