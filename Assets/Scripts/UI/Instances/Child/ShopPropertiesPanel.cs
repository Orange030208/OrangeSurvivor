using Orange.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public class ShopPropertiesPanel : ViewPartBase
{
    [SerializeField] private MonoBehaviour motionSource;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Describer propertiesDescriber;

    private IUIRuntimeMotion motion;
    private readonly InfoDocumentService infoDocumentService = new();
    private PropertiesManager propertiesManager;
    private bool visible;
    private bool eventsBound;

    private void Awake()
    {
        ValidateConfiguration();
        motion = ResolveRuntimeMotion(motionSource, "properties sidebar");
        motion.RefreshDefaults();
    }

    private void OnDisable()
    {
        EndSession();
    }

    public void BeginSession(PropertiesManager manager)
    {
        BindEvents();
        BindPropertiesManager(manager);
        SetVisibleImmediate(false);
    }

    public void EndSession()
    {
        UnbindEvents();
        BindPropertiesManager(null);
        SetVisibleImmediate(false);
        motion?.Kill();
    }

    private void BindEvents()
    {
        if (eventsBound)
        {
            return;
        }

        toggleButton.onClick.AddListener(OnToggleRequested);
        eventsBound = true;
    }

    private void UnbindEvents()
    {
        if (!eventsBound)
        {
            return;
        }

        toggleButton.onClick.RemoveListener(OnToggleRequested);
        eventsBound = false;
    }

    private void BindPropertiesManager(PropertiesManager manager)
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged -= OnPropertiesChanged;
        }

        propertiesManager = manager;
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged += OnPropertiesChanged;
        }

        RefreshPropertiesDisplay();
    }

    private void OnToggleRequested()
    {
        AudioSfxBridge.RequestPlay(visible ? AudioSfxKey.UiCancel : AudioSfxKey.UiConfirm);
        SetVisible(!visible);
    }

    private void OnPropertiesChanged()
    {
        RefreshPropertiesDisplay();
    }

    private void RefreshPropertiesDisplay()
    {
        if (propertiesManager == null)
        {
            propertiesDescriber.Display((InfoDocument)null);
            return;
        }

        if (infoDocumentService.TryBuild(propertiesManager, out InfoDocument document))
        {
            propertiesDescriber.Display(document);
            return;
        }

        propertiesDescriber.Display((InfoDocument)null);
    }

    private void SetVisible(bool value)
    {
        visible = value;
        motion?.Play(visible ? UIMotionClipIds.SHOW : UIMotionClipIds.HIDE);
    }

    private void SetVisibleImmediate(bool value)
    {
        visible = value;
        motion?.SetImmediate(visible ? UIMotionClipIds.SHOW : UIMotionClipIds.HIDE);
    }

    private void ValidateConfiguration()
    {
        if (motionSource == null)
        {
            throw new MissingReferenceException($"{nameof(ShopPropertiesPanel)} '{name}' is missing motion source.");
        }

        if (toggleButton == null)
        {
            throw new MissingReferenceException($"{nameof(ShopPropertiesPanel)} '{name}' is missing toggle button.");
        }

        if (propertiesDescriber == null)
        {
            throw new MissingReferenceException($"{nameof(ShopPropertiesPanel)} '{name}' is missing properties describer.");
        }
    }

    private IUIRuntimeMotion ResolveRuntimeMotion(MonoBehaviour source, string fieldName)
    {
        if (source is IUIRuntimeMotion directMotion)
        {
            return directMotion;
        }

        MonoBehaviour[] behaviours = source.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IUIRuntimeMotion resolvedMotion)
            {
                return resolvedMotion;
            }
        }

        throw new MissingComponentException($"{nameof(ShopPropertiesPanel)} '{name}' expects {fieldName} to implement {nameof(IUIRuntimeMotion)}.");
    }
}
