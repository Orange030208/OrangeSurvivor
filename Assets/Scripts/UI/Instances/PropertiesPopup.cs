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
    private AttributeManager AttributeManager;
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
        BindAttributeManager(null);
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        AttributeManager manager = context.GetPayload<AttributeManager>()
            ?? throw new InvalidOperationException($"{nameof(PropertiesPopup)} requires {nameof(AttributeManager)} payload.");

        BindEvents();
        BindAttributeManager(manager);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        UnbindEvents();
        BindAttributeManager(null);
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

    private void BindAttributeManager(AttributeManager manager)
    {
        if (AttributeManager != null)
        {
            AttributeManager.OnAttributesChanged -= OnAttributesChanged;
        }

        AttributeManager = manager;
        if (AttributeManager != null)
        {
            AttributeManager.OnAttributesChanged += OnAttributesChanged;
        }

        RefreshPropertiesDisplay();
    }

    private void OnAttributesChanged()
    {
        RefreshPropertiesDisplay();
    }

    private void RefreshPropertiesDisplay()
    {
        if (AttributeManager == null)
        {
            propertiesDescriber.Display(null, compactRowsOnly: true);
            return;
        }

        if (infoDocumentService.TryBuild(AttributeManager, out InfoDocument document))
        {
            propertiesDescriber.Display(document, compactRowsOnly: true);
            return;
        }

        propertiesDescriber.Display(null, compactRowsOnly: true);
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
