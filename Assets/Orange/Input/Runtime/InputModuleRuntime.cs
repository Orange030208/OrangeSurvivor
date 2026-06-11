using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Orange.Input
{
    [DisallowMultipleComponent]
    public sealed class InputModuleRuntime : MonoBehaviour, IInputActionProvider
    {
        [SerializeField] private InputModuleProfile profile;
        [SerializeField] private bool dontDestroyOnLoad = true;

        private readonly System.Collections.Generic.List<ContextStackEntry> contextStack = new();
        private int nextContextToken;
        private bool initialized;

        public InputModuleProfile Profile => profile;
        public InputActionAsset ActionsAsset => profile != null ? profile.ActionsAsset : null;
        public string ActiveContextId => contextStack.Count > 0 ? contextStack[^1].ContextId : null;
        public int ContextStackCount => contextStack.Count;
        public bool IsInitialized => initialized;

        private void Awake()
        {
            if (dontDestroyOnLoad && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDisable()
        {
            ActionsAsset?.Disable();
        }

        public bool Initialize()
        {
            return Initialize(profile);
        }

        public bool Initialize(InputModuleProfile inputProfile)
        {
            if (inputProfile != null && inputProfile != profile)
            {
                ActionsAsset?.Disable();
                profile = inputProfile;
                initialized = false;
                contextStack.Clear();
            }
            else if (inputProfile != null)
            {
                profile = inputProfile;
            }

            if (profile == null)
            {
                Debug.LogError($"{nameof(InputModuleRuntime)} on '{name}' requires an explicit {nameof(InputModuleProfile)} reference.", this);
                return false;
            }

            if (profile.ActionsAsset == null)
            {
                Debug.LogError($"{nameof(InputModuleRuntime)} profile '{profile.name}' requires an explicit {nameof(InputActionAsset)} reference.", profile);
                return false;
            }

            if (initialized)
            {
                ApplyTopContext();
                return true;
            }

            initialized = true;
            if (!string.IsNullOrWhiteSpace(profile.DefaultContextId))
            {
                if (SetContextInternal(profile.DefaultContextId))
                {
                    return true;
                }

                initialized = false;
                contextStack.Clear();
                return false;
            }

            profile.ActionsAsset.Disable();
            return true;
        }

        public bool TryFindAction(string actionPath, out InputAction action)
        {
            action = null;
            if (ActionsAsset == null || string.IsNullOrWhiteSpace(actionPath))
            {
                return false;
            }

            action = ActionsAsset.FindAction(actionPath, throwIfNotFound: false);
            return action != null;
        }

        public string SaveBindingOverrides()
        {
            return ActionsAsset != null ? ActionsAsset.SaveBindingOverridesAsJson() : string.Empty;
        }

        public void LoadBindingOverrides(string overridesJson)
        {
            if (!Initialize())
            {
                return;
            }

            ActionsAsset.RemoveAllBindingOverrides();
            if (!string.IsNullOrWhiteSpace(overridesJson))
            {
                ActionsAsset.LoadBindingOverridesFromJson(overridesJson);
            }

            if (StripLegacyEscapeOverrides() && profile.BindingOverrideStore != null)
            {
                profile.BindingOverrideStore.SaveBindingOverrides(SaveBindingOverrides());
            }
        }

        public void ClearBindingOverrides()
        {
            ActionsAsset?.RemoveAllBindingOverrides();
        }

        public bool LoadBindingOverridesFromStore()
        {
            if (!Initialize() || profile.BindingOverrideStore == null)
            {
                return false;
            }

            LoadBindingOverrides(profile.BindingOverrideStore.LoadBindingOverrides());
            return true;
        }

        public bool SaveBindingOverridesToStore()
        {
            if (!Initialize() || profile.BindingOverrideStore == null)
            {
                return false;
            }

            profile.BindingOverrideStore.SaveBindingOverrides(SaveBindingOverrides());
            return true;
        }

        public bool ClearBindingOverrideStore()
        {
            if (!Initialize() || profile.BindingOverrideStore == null)
            {
                return false;
            }

            profile.BindingOverrideStore.ClearBindingOverrides();
            return true;
        }

        private bool StripLegacyEscapeOverrides()
        {
            if (ActionsAsset == null)
            {
                return false;
            }

            bool removedAny = false;
            foreach (InputActionMap actionMap in ActionsAsset.actionMaps)
            {
                foreach (InputAction action in actionMap.actions)
                {
                    for (int bindingIndex = action.bindings.Count - 1; bindingIndex >= 0; bindingIndex--)
                    {
                        InputBinding binding = action.bindings[bindingIndex];
                        if (!string.Equals(binding.effectivePath, "<Keyboard>/escape", System.StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        action.RemoveBindingOverride(bindingIndex);
                        removedAny = true;
                    }
                }
            }

            return removedAny;
        }

        public bool ConfigureUi(EventSystem eventSystem)
        {
            return Initialize() && InputSystemUiBinder.Configure(eventSystem, this, profile.UiActionPaths);
        }

        public InputContextHandle PushContext(string contextId)
        {
            if (!Initialize() || !TryResolveContext(contextId, out InputContextDefinition context))
            {
                return null;
            }

            ContextStackEntry entry = new(context.ContextId, ++nextContextToken);
            contextStack.Add(entry);
            ApplyTopContext();
            return new InputContextHandle(this, entry.ContextId, entry.Token);
        }

        public bool SetContext(string contextId)
        {
            if (!Initialize())
            {
                return false;
            }

            return SetContextInternal(contextId);
        }

        private bool SetContextInternal(string contextId)
        {
            if (!TryResolveContext(contextId, out InputContextDefinition context))
            {
                return false;
            }

            contextStack.Clear();
            contextStack.Add(new ContextStackEntry(context.ContextId, ++nextContextToken));
            ApplyTopContext();
            return true;
        }

        public bool PopContext(string contextId)
        {
            if (string.IsNullOrWhiteSpace(contextId))
            {
                return false;
            }

            for (int i = contextStack.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(contextStack[i].ContextId, contextId, System.StringComparison.Ordinal))
                {
                    continue;
                }

                contextStack.RemoveAt(i);
                ApplyTopContext();
                return true;
            }

            return false;
        }

        internal bool PopContext(InputContextHandle handle)
        {
            if (handle == null)
            {
                return false;
            }

            for (int i = contextStack.Count - 1; i >= 0; i--)
            {
                ContextStackEntry entry = contextStack[i];
                if (entry.Token != handle.Token)
                {
                    continue;
                }

                contextStack.RemoveAt(i);
                ApplyTopContext();
                return true;
            }

            return false;
        }

        private bool TryResolveContext(string contextId, out InputContextDefinition context)
        {
            context = default;
            if (profile == null || string.IsNullOrWhiteSpace(contextId))
            {
                return false;
            }

            InputContextDefinition[] contexts = profile.Contexts;
            for (int i = 0; i < contexts.Length; i++)
            {
                if (!string.Equals(contexts[i].ContextId, contextId, System.StringComparison.Ordinal))
                {
                    continue;
                }

                context = contexts[i];
                return true;
            }

            Debug.LogError($"{nameof(InputModuleRuntime)} profile '{profile.name}' does not define input context '{contextId}'.", profile);
            return false;
        }

        private void ApplyTopContext()
        {
            InputActionAsset asset = ActionsAsset;
            if (asset == null)
            {
                return;
            }

            asset.Disable();
            if (contextStack.Count == 0)
            {
                return;
            }

            if (!TryResolveContext(contextStack[^1].ContextId, out InputContextDefinition context))
            {
                return;
            }

            string[] actionMapNames = context.ActionMapNames;
            for (int i = 0; i < actionMapNames.Length; i++)
            {
                string actionMapName = actionMapNames[i];
                if (string.IsNullOrWhiteSpace(actionMapName))
                {
                    continue;
                }

                InputActionMap map = asset.FindActionMap(actionMapName, throwIfNotFound: false);
                if (map == null)
                {
                    Debug.LogError($"{nameof(InputModuleRuntime)} profile '{profile.name}' context '{context.ContextId}' references missing action map '{actionMapName}'.", profile);
                    continue;
                }

                map.Enable();
            }
        }

        private readonly struct ContextStackEntry
        {
            public ContextStackEntry(string contextId, int token)
            {
                ContextId = contextId;
                Token = token;
            }

            public string ContextId { get; }
            public int Token { get; }
        }
    }

    public sealed class InputContextHandle : System.IDisposable
    {
        private readonly InputModuleRuntime runtime;
        private bool disposed;

        internal InputContextHandle(InputModuleRuntime runtime, string contextId, int token)
        {
            this.runtime = runtime;
            ContextId = contextId;
            Token = token;
        }

        public string ContextId { get; }
        internal int Token { get; }
        public bool IsDisposed => disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            runtime?.PopContext(this);
        }
    }
}
