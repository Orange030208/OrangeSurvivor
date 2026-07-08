using Orange.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public class ShopPropertiesPanel : ViewPartBase
{
    [SerializeField] private UIMotionPlayer motionPlayer;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Describer propertiesDescriber;

    private readonly InfoDocumentService infoDocumentService = new();
    private PropertiesManager propertiesManager;
    private bool visible;
    private bool eventsBound;

    private void Awake()
    {
        ValidateConfiguration();
        motionPlayer.RefreshDefaults();
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
        motionPlayer?.Kill();
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
            DisplayPropertiesDocument(null);
            return;
        }

        if (infoDocumentService.TryBuild(propertiesManager, out InfoDocument document))
        {
            DisplayPropertiesDocument(document);
            return;
        }

        DisplayPropertiesDocument(null);
    }

    private void DisplayPropertiesDocument(InfoDocument document)
    {
        if (propertiesDescriber is PropertiesIconTextDescriber iconTextDescriber)
        {
            iconTextDescriber.Display(document, compactRowsOnly: true);
            return;
        }

        propertiesDescriber.Display(document);
    }

    private void SetVisible(bool value)
    {
        visible = value;
        motionPlayer?.Play(visible ? UIMotionClipIds.SHOW : UIMotionClipIds.HIDE);
    }

    private void SetVisibleImmediate(bool value)
    {
        visible = value;
        motionPlayer?.SetImmediate(visible ? UIMotionClipIds.SHOW : UIMotionClipIds.HIDE);
    }

    private void ValidateConfiguration()
    {
        if (motionPlayer == null)
        {
            throw new MissingReferenceException($"{nameof(ShopPropertiesPanel)} '{name}' is missing motion player.");
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

}
