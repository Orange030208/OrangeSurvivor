using Orange.Input;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class GameInputSceneBinder : MonoBehaviour
{
    [SerializeField] private GameInput input;
    [SerializeField] private InputActionRuntime inputRuntime;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private InputSystemUiActionPaths uiActionPaths = InputSystemUiActionPaths.Default;

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
            Debug.LogError($"{nameof(GameInputSceneBinder)} on '{name}' requires an explicit {nameof(InputActionRuntime)} reference.", this);
            return false;
        }

        input.SetInputRuntime(inputRuntime);
        if (!GameInput.TryRegisterSceneInstance(input))
        {
            return false;
        }

        input.EnableDefaultMaps();
        if (!InputSystemUiBinder.Configure(eventSystem, inputRuntime, uiActionPaths))
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
