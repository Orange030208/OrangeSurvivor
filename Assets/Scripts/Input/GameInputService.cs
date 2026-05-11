using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

[DisallowMultipleComponent]
public sealed class GameInputService : MonoBehaviour
{
    private const string INPUT_ACTIONS_RESOURCE_PATH = "Input/SurvivorsInputActions";
    private const string SERVICE_OBJECT_NAME = "GameInputService";

    private static GameInputService instance;

    [SerializeField] private InputActionAsset actionsAsset;
    [SerializeField] private bool dontDestroyOnLoad = true;

    private InputActionMap gameplayMap;
    private InputActionMap uiMap;
    private InputAction moveAction;
    private InputAction pauseAction;
    private InputAction uiCancelAction;

    public static GameInputService Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    public InputActionAsset ActionsAsset => actionsAsset;
    public InputAction MoveAction => moveAction;
    public InputAction PauseAction => pauseAction;
    public InputAction UiCancelAction => uiCancelAction;
    public Vector2 Move => moveAction != null && moveAction.enabled ? moveAction.ReadValue<Vector2>() : Vector2.zero;

    public event Action PausePerformed;
    public event Action UiCancelPerformed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        if (!Application.isPlaying)
        {
            return;
        }

        GameInputService existing = FindFirstObjectByType<GameInputService>();
        if (existing != null)
        {
            instance = existing;
            instance.Initialize();
            return;
        }

        GameObject serviceObject = new GameObject(SERVICE_OBJECT_NAME);
        instance = serviceObject.AddComponent<GameInputService>();
        instance.Initialize();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Initialize();

        if (dontDestroyOnLoad && transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        Initialize();
        EnableDefaultMaps();
    }

    private void OnDisable()
    {
        actionsAsset?.Disable();
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

        if (instance == this)
        {
            instance = null;
        }
    }

    public void Initialize()
    {
        if (actionsAsset == null)
        {
            actionsAsset = Resources.Load<InputActionAsset>(INPUT_ACTIONS_RESOURCE_PATH);
        }

        if (actionsAsset == null)
        {
            Debug.LogError($"{nameof(GameInputService)} failed to load InputActionAsset at Resources/{INPUT_ACTIONS_RESOURCE_PATH}.");
            return;
        }

        gameplayMap = actionsAsset.FindActionMap("Gameplay", throwIfNotFound: true);
        uiMap = actionsAsset.FindActionMap("UI", throwIfNotFound: true);
        moveAction = gameplayMap.FindAction("Move", throwIfNotFound: true);
        InputAction resolvedPauseAction = gameplayMap.FindAction("Pause", throwIfNotFound: true);
        InputAction resolvedUiCancelAction = uiMap.FindAction("Cancel", throwIfNotFound: true);

        if (pauseAction != resolvedPauseAction)
        {
            if (pauseAction != null)
            {
                pauseAction.performed -= OnPausePerformed;
            }

            pauseAction = resolvedPauseAction;
            pauseAction.performed += OnPausePerformed;
        }
        else
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.performed += OnPausePerformed;
        }

        if (uiCancelAction != resolvedUiCancelAction)
        {
            if (uiCancelAction != null)
            {
                uiCancelAction.performed -= OnUiCancelPerformed;
            }

            uiCancelAction = resolvedUiCancelAction;
            uiCancelAction.performed += OnUiCancelPerformed;
        }
        else
        {
            uiCancelAction.performed -= OnUiCancelPerformed;
            uiCancelAction.performed += OnUiCancelPerformed;
        }
    }

    public void EnableGameplay()
    {
        Initialize();
        uiMap?.Disable();
        gameplayMap?.Enable();
    }

    public void EnableUI()
    {
        Initialize();
        gameplayMap?.Disable();
        uiMap?.Enable();
    }

    public void EnableDefaultMaps()
    {
        Initialize();
        gameplayMap?.Enable();
        uiMap?.Enable();
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
        if (actionsAsset == null)
        {
            return;
        }

        actionsAsset.RemoveAllBindingOverrides();
    }

    public bool TryFindAction(string actionPath, out InputAction action)
    {
        Initialize();
        action = null;
        if (actionsAsset == null || string.IsNullOrWhiteSpace(actionPath))
        {
            return false;
        }

        string[] parts = actionPath.Split('/');
        if (parts.Length != 2)
        {
            return false;
        }

        InputActionMap map = actionsAsset.FindActionMap(parts[0], throwIfNotFound: false);
        action = map != null ? map.FindAction(parts[1], throwIfNotFound: false) : null;
        return action != null;
    }

    public static void ConfigureEventSystem(EventSystem eventSystem)
    {
        if (eventSystem == null || !Application.isPlaying)
        {
            return;
        }

        EnsureInstance();
        if (instance == null)
        {
            return;
        }

        GameInputService inputService = instance;
        StandaloneInputModule standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneModule != null)
        {
            standaloneModule.enabled = false;
        }

        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        InputActionAsset asset = inputService.ActionsAsset;
        inputModule.actionsAsset = asset;
        inputModule.point = InputActionReference.Create(asset.FindAction("UI/Point", throwIfNotFound: true));
        inputModule.leftClick = InputActionReference.Create(asset.FindAction("UI/Click", throwIfNotFound: true));
        inputModule.scrollWheel = InputActionReference.Create(asset.FindAction("UI/Scroll", throwIfNotFound: true));
        inputModule.move = InputActionReference.Create(asset.FindAction("UI/Navigate", throwIfNotFound: true));
        inputModule.submit = InputActionReference.Create(asset.FindAction("UI/Submit", throwIfNotFound: true));
        inputModule.cancel = InputActionReference.Create(asset.FindAction("UI/Cancel", throwIfNotFound: true));
        inputModule.moveRepeatDelay = 0.45f;
        inputModule.moveRepeatRate = 0.08f;
        inputModule.enabled = true;
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        PausePerformed?.Invoke();
    }

    private void OnUiCancelPerformed(InputAction.CallbackContext context)
    {
        UiCancelPerformed?.Invoke();
    }
}
