using UnityEngine;

#if YOKIFRAME_INPUTSYSTEM_SUPPORT
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using YokiFrame;
#endif

[DisallowMultipleComponent]
public sealed class InputBootstrap : MonoBehaviour
{
#if YOKIFRAME_INPUTSYSTEM_SUPPORT
    private const string GAMEPLAY_ACTION_MAP = "Gameplay";
    private const string UI_ACTION_MAP = "UI";

    private static InputBootstrap instance;

    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private InputSystemUIInputModule uiInputModule;
    [SerializeField] private bool configureUiInputModule = true;
    [SerializeField] private float uiMoveRepeatDelay = 0.45f;
    [SerializeField] private float uiMoveRepeatRate = 0.08f;

    private bool ownsInputKitLifecycle;
    private InputActionReference[] runtimeUiReferences = Array.Empty<InputActionReference>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (dontDestroyOnLoad && transform.parent == default)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            Initialize();
        }
    }

    private void Update()
    {
        if (!Application.isPlaying || !InputKit.IsInitialized)
        {
            return;
        }

        InputKit.UpdateCombo();
        InputKit.UpdateHaptic();
        InputKit.CleanupBuffer();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ClearUiInputModuleReferences();

        if (!ownsInputKitLifecycle)
        {
            return;
        }

        InputKit.Dispose();
        ownsInputKitLifecycle = false;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public bool Initialize()
    {
        if (!InputKit.IsRegistered<SurvivorsInputActions>())
        {
            InputKit.Register<SurvivorsInputActions>();
        }

        InputKit.SetPersistenceKey(GameSettingsService.INPUT_REBINDS_JSON_KEY);

        if (!InputKit.IsInitialized)
        {
            InputKit.Initialize();
            ownsInputKitLifecycle = true;
        }

        InputKit.EnableActionMaps(GAMEPLAY_ACTION_MAP, UI_ACTION_MAP);

        SurvivorsInputActions input = InputKit.Get<SurvivorsInputActions>();
        return !configureUiInputModule || ConfigureUiInputModule(input);
    }

    private bool ConfigureUiInputModule(SurvivorsInputActions input)
    {
        if (uiInputModule == default)
        {
            Debug.LogError($"{nameof(InputBootstrap)} on '{name}' requires an explicit {nameof(InputSystemUIInputModule)} reference.", this);
            return false;
        }

        if (input == default)
        {
            Debug.LogError($"{nameof(InputBootstrap)} on '{name}' could not resolve {nameof(SurvivorsInputActions)} from {nameof(InputKit)}.", this);
            return false;
        }

        ClearUiInputModuleReferences();

        InputActionReference point = InputActionReference.Create(input.UI.Point);
        InputActionReference click = InputActionReference.Create(input.UI.Click);
        InputActionReference scroll = InputActionReference.Create(input.UI.Scroll);
        InputActionReference move = InputActionReference.Create(input.UI.Navigate);
        InputActionReference submit = InputActionReference.Create(input.UI.Submit);
        InputActionReference cancel = InputActionReference.Create(input.UI.Cancel);

        runtimeUiReferences = new[]
        {
            point,
            click,
            scroll,
            move,
            submit,
            cancel
        };

        uiInputModule.actionsAsset = input.asset;
        uiInputModule.point = point;
        uiInputModule.leftClick = click;
        uiInputModule.scrollWheel = scroll;
        uiInputModule.move = move;
        uiInputModule.submit = submit;
        uiInputModule.cancel = cancel;
        uiInputModule.moveRepeatDelay = uiMoveRepeatDelay;
        uiInputModule.moveRepeatRate = uiMoveRepeatRate;
        uiInputModule.enabled = true;
        return true;
    }

    private void ClearUiInputModuleReferences()
    {
        if (uiInputModule != default)
        {
            uiInputModule.point = null;
            uiInputModule.leftClick = null;
            uiInputModule.scrollWheel = null;
            uiInputModule.move = null;
            uiInputModule.submit = null;
            uiInputModule.cancel = null;
        }

        for (int i = 0; i < runtimeUiReferences.Length; i++)
        {
            if (runtimeUiReferences[i] != default)
            {
                Destroy(runtimeUiReferences[i]);
            }
        }

        runtimeUiReferences = Array.Empty<InputActionReference>();
    }
#else
    private void Awake()
    {
        Debug.LogWarning($"{nameof(InputBootstrap)} requires YOKIFRAME_INPUTSYSTEM_SUPPORT.");
    }
#endif
}
