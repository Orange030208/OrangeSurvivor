using Orange.UIFramework;
using UnityEngine;

public class WaveTransitionChestPanel : ViewPartBase
{
    [SerializeField] private Transform root;
    [SerializeField] private AccessoryOperateContainer accessoryOperateContainer;

    private void Awake()
    {
        ResolveViewParts();
        ValidateConfiguration();
    }

    public void Show(AccessoryDataSO accessoryData)
    {
        ResolveViewParts();
        if (accessoryData == null)
        {
            Hide();
            return;
        }

        root.gameObject.SetActive(true);
        accessoryOperateContainer.gameObject.SetActive(true);
        accessoryOperateContainer.Configure(accessoryData);
    }

    public void SetVisible(bool visible)
    {
        ResolveViewParts();
        root.gameObject.SetActive(visible);
        accessoryOperateContainer.gameObject.SetActive(visible);
        if (!visible)
        {
            accessoryOperateContainer.CleanUp();
        }
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void Clear()
    {
        accessoryOperateContainer.CleanUp();
    }

    private void ResolveViewParts()
    {
        if (root == null)
        {
            root = transform;
        }

        if (accessoryOperateContainer == null)
        {
            accessoryOperateContainer = GetComponentInChildren<AccessoryOperateContainer>(true);
        }
    }

    private void ValidateConfiguration()
    {
        if (root == null)
        {
            throw new MissingReferenceException($"{nameof(WaveTransitionChestPanel)} '{name}' is missing root.");
        }

        if (accessoryOperateContainer == null)
        {
            throw new MissingReferenceException($"{nameof(WaveTransitionChestPanel)} '{name}' is missing accessory operate container.");
        }
    }
}
