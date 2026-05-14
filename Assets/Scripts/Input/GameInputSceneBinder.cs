using Orange.Input;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class GameInputSceneBinder : MonoBehaviour
{
    [SerializeField] private GameInput input;
    [SerializeField] private InputModuleRuntime inputRuntime;
    [SerializeField] private InputModuleProfile inputProfile;
    [SerializeField] private EventSystem eventSystem;

    private GameInput registeredInput;

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            Bind();
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            Unbind();
        }
    }

    public bool Bind()
    {
        if (input == null)
        {
            Debug.LogError($"{nameof(GameInputSceneBinder)} on '{name}' requires an explicit {nameof(GameInput)} reference.", this);
            return false;
        }

        if (eventSystem == null)
        {
            Debug.LogError($"{nameof(GameInputSceneBinder)} on '{name}' requires an explicit {nameof(EventSystem)} reference.", this);
            return false;
        }

        if (inputRuntime == null)
        {
            Debug.LogError($"{nameof(GameInputSceneBinder)} on '{name}' requires an explicit {nameof(InputModuleRuntime)} reference.", this);
            return false;
        }

        if (inputProfile == null)
        {
            Debug.LogError($"{nameof(GameInputSceneBinder)} on '{name}' requires an explicit {nameof(InputModuleProfile)} reference.", this);
            return false;
        }

        if (!inputRuntime.Initialize(inputProfile))
        {
            return false;
        }

        inputRuntime.LoadBindingOverridesFromStore();
        input.SetInputRuntime(inputRuntime);
        if (!GameInput.TryRegisterSceneInstance(input))
        {
            return false;
        }

        if (!inputRuntime.ConfigureUi(eventSystem))
        {
            GameInput.UnregisterSceneInstance(input);
            inputRuntime.ActionsAsset?.Disable();
            return false;
        }

        registeredInput = input;
        return true;
    }

    public void Unbind()
    {
        if (registeredInput == null)
        {
            return;
        }

        GameInput.UnregisterSceneInstance(registeredInput);
        registeredInput = null;
    }
}
