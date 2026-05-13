using System;
using Orange.Input;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class GameInput : MonoBehaviour, IInputActionProvider
{
    private static GameInput instance;

    [SerializeField] private InputActionRuntime inputRuntime;
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private string moveActionPath = "Gameplay/Move";
    [SerializeField] private string pauseActionPath = "Gameplay/Pause";
    [SerializeField] private string uiCancelActionPath = "UI/Cancel";

    private InputActionMap gameplayMap;
    private InputActionMap uiMap;
    private InputAction moveAction;
    private InputAction pauseAction;
    private InputAction uiCancelAction;

    public static GameInput Instance => instance;

    public InputActionAsset ActionsAsset => inputRuntime != null ? inputRuntime.ActionsAsset : null;
    public InputActionRuntime InputRuntime => inputRuntime;
    public InputAction MoveAction => moveAction;
    public InputAction PauseAction => pauseAction;
    public InputAction UiCancelAction => uiCancelAction;
    public Vector2 Move => moveAction != null && moveAction.enabled ? moveAction.ReadValue<Vector2>() : Vector2.zero;

    public event Action PausePerformed;
    public event Action UiCancelPerformed;

    public static bool TryRegisterSceneInstance(GameInput service)
    {
        if (service == null)
        {
            Debug.LogError($"{nameof(GameInput)} cannot register a null scene instance.");
            return false;
        }

        if (instance != null && instance != service)
        {
            Debug.LogError(
                $"{nameof(GameInput)} duplicate found on '{service.name}'. Keep exactly one scene or scene binder-owned instance.",
                service);
            return false;
        }

        if (!service.Initialize())
        {
            return false;
        }

        instance = service;
        return true;
    }

    public static void UnregisterSceneInstance(GameInput service)
    {
        if (service != null && instance == service)
        {
            instance = null;
        }
    }

    private void Awake()
    {
        if (dontDestroyOnLoad && transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
        }

        if (uiCancelAction != null)
        {
            uiCancelAction.performed -= OnUiCancelPerformed;
        }

        UnregisterSceneInstance(this);
    }

    public void SetInputRuntime(InputActionRuntime runtime)
    {
        inputRuntime = runtime;
    }

    public bool Initialize()
    {
        if (inputRuntime == null)
        {
            Debug.LogError($"{nameof(GameInput)} on '{name}' requires an explicit {nameof(InputActionRuntime)} reference.", this);
            return false;
        }

        inputRuntime.Initialize();
        if (ActionsAsset == null)
        {
            return false;
        }

        gameplayMap = ResolveActionMap(moveActionPath) ?? ResolveActionMap(pauseActionPath);
        uiMap = ResolveActionMap(uiCancelActionPath);
        moveAction = ResolveRequiredAction(moveActionPath);
        BindPerformedAction(ref pauseAction, ResolveRequiredAction(pauseActionPath), OnPausePerformed);
        BindPerformedAction(ref uiCancelAction, ResolveRequiredAction(uiCancelActionPath), OnUiCancelPerformed);
        return moveAction != null && pauseAction != null && uiCancelAction != null;
    }

    public void EnableGameplay()
    {
        if (!Initialize())
        {
            return;
        }

        uiMap?.Disable();
        gameplayMap?.Enable();
    }

    public void EnableUI()
    {
        if (!Initialize())
        {
            return;
        }

        gameplayMap?.Disable();
        uiMap?.Enable();
    }

    public void EnableDefaultMaps()
    {
        if (!Initialize())
        {
            return;
        }

        gameplayMap?.Enable();
        uiMap?.Enable();
    }

    public string SaveBindingOverrides()
    {
        return inputRuntime != null ? inputRuntime.SaveBindingOverrides() : string.Empty;
    }

    public void LoadBindingOverrides(string overridesJson)
    {
        if (inputRuntime == null)
        {
            Debug.LogError($"{nameof(GameInput)} on '{name}' cannot load binding overrides without {nameof(InputActionRuntime)}.", this);
            return;
        }

        inputRuntime.LoadBindingOverrides(overridesJson);
        Initialize();
    }

    public void ClearBindingOverrides()
    {
        if (inputRuntime == null)
        {
            return;
        }

        inputRuntime.ClearBindingOverrides();
    }

    public bool TryFindAction(string actionPath, out InputAction action)
    {
        action = null;
        return inputRuntime != null && inputRuntime.TryFindAction(actionPath, out action);
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        PausePerformed?.Invoke();
    }

    private void OnUiCancelPerformed(InputAction.CallbackContext context)
    {
        UiCancelPerformed?.Invoke();
    }

    private InputAction ResolveRequiredAction(string actionPath)
    {
        if (TryFindAction(actionPath, out InputAction action))
        {
            return action;
        }

        Debug.LogError($"{nameof(GameInput)} on '{name}' cannot find required action '{actionPath}'.", this);
        return null;
    }

    private InputActionMap ResolveActionMap(string actionPath)
    {
        InputActionAsset asset = ActionsAsset;
        if (asset == null || string.IsNullOrWhiteSpace(actionPath))
        {
            return null;
        }

        int separatorIndex = actionPath.IndexOf('/');
        if (separatorIndex <= 0)
        {
            Debug.LogError($"{nameof(GameInput)} on '{name}' has invalid action path '{actionPath}'. Expected 'Map/Action'.", this);
            return null;
        }

        string mapName = actionPath.Substring(0, separatorIndex);
        return asset.FindActionMap(mapName, throwIfNotFound: false);
    }

    private static void BindPerformedAction(
        ref InputAction currentAction,
        InputAction resolvedAction,
        Action<InputAction.CallbackContext> callback)
    {
        if (currentAction != null)
        {
            currentAction.performed -= callback;
        }

        currentAction = resolvedAction;
        if (currentAction != null)
        {
            currentAction.performed -= callback;
            currentAction.performed += callback;
        }
    }
}
