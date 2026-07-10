using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public class ShopPropertiesPopup : PopupBase
{
    [SerializeField] private Describer propertiesDescriber;

    private readonly InfoDocumentService infoDocumentService = new();
    private AttributeManager AttributeManager;

    protected override void OnCreate()
    {
        base.OnCreate();
        ResolveViewParts();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        AttributeManager manager = context.GetPayload<AttributeManager>()
            ?? throw new InvalidOperationException($"{nameof(ShopPropertiesPopup)} requires {nameof(AttributeManager)} payload.");

        BindAttributeManager(manager);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        BindAttributeManager(null);
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
            DisplayPropertiesDocument(null);
            return;
        }

        if (infoDocumentService.TryBuild(AttributeManager, out InfoDocument document))
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
