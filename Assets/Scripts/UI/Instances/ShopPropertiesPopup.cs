using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public class ShopPropertiesPopup : PopupBase
{
    [SerializeField] private Describer propertiesDescriber;

    private readonly InfoDocumentService infoDocumentService = new();
    private PropertiesManager propertiesManager;

    protected override void OnCreate()
    {
        base.OnCreate();
        ResolveViewParts();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        PropertiesManager manager = context.GetPayload<PropertiesManager>()
            ?? throw new InvalidOperationException($"{nameof(ShopPropertiesPopup)} requires {nameof(PropertiesManager)} payload.");

        BindPropertiesManager(manager);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        BindPropertiesManager(null);
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
            propertiesDescriber = GetComponentInChildren<Describer>(true);
        }
    }

    private void ValidateConfiguration()
    {
        if (propertiesDescriber == null)
        {
            throw new MissingReferenceException($"{nameof(ShopPropertiesPopup)} '{name}' is missing properties describer.");
        }
    }
}
