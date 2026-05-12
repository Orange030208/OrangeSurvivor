using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public class ShopPropertiesPopup : PopupBase
{
    [SerializeField] private Describer propertiesDescriber;

    private PropertiesManager propertiesManager;

    protected override void Awake()
    {
        base.Awake();
        ResolveViewParts();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        ShopPropertiesPopupContext propertiesContext = context.GetPayload<ShopPropertiesPopupContext>();
        if (propertiesContext == null)
        {
            ShopPageContext shopPageContext = context.GetPayload<ShopPageContext>();
            if (shopPageContext == null)
            {
                throw new ArgumentException($"{nameof(ShopPropertiesPopup)} requires {nameof(ShopPropertiesPopupContext)} payload.");
            }

            propertiesContext = new ShopPropertiesPopupContext(shopPageContext.PropertiesManager);
        }

        BindPropertiesManager(propertiesContext.PropertiesManager);
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
        propertiesDescriber.Display(propertiesManager);
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
