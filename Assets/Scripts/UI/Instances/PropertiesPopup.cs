using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public sealed class PropertiesPopup : PopupBase
{
    [SerializeField] private Button closeButton;
    [SerializeField] private PropertiesIconTextDescriber propertiesDescriber;

    private readonly InfoDocumentService infoDocumentService = new();
    private PropertiesManager propertiesManager;
    private bool eventsBound;

    protected override void OnCreate()
    {
        base.OnCreate();
        ResolveViewParts();
        ValidateConfiguration();
    }

    private void OnDestroy()
    {
        UnbindEvents();
        BindPropertiesManager(null);
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        PropertiesManager manager = context.GetPayload<PropertiesManager>()
            ?? throw new InvalidOperationException($"{nameof(PropertiesPopup)} requires {nameof(PropertiesManager)} payload.");

        BindEvents();
        BindPropertiesManager(manager);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        UnbindEvents();
        BindPropertiesManager(null);
    }

    private void BindEvents()
    {
        if (eventsBound)
        {
            return;
        }

        closeButton.onClick.AddListener(OnCloseClicked);
        eventsBound = true;
    }

    private void UnbindEvents()
    {
        if (!eventsBound)
        {
            return;
        }

        closeButton.onClick.RemoveListener(OnCloseClicked);
        eventsBound = false;
    }

    private void OnCloseClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
        Handle.CloseAsync(CloseReason.Normal, this.GetCancellationTokenOnDestroy()).Forget();
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

    private void ResolveViewParts()
    {
        if (propertiesDescriber == null)
        {
            propertiesDescriber = GetComponentInChildren<PropertiesIconTextDescriber>(true);
        }
    }

    private void ValidateConfiguration()
    {
        if (closeButton == null)
        {
            throw new MissingReferenceException($"{nameof(PropertiesPopup)} '{name}' is missing close button.");
        }

        if (propertiesDescriber == null)
        {
            throw new MissingReferenceException($"{nameof(PropertiesPopup)} '{name}' is missing properties describer.");
        }
    }
}
