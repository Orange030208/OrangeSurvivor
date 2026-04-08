using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public abstract class ContainerBaseUI<T> : MonoBehaviour,IContainerColorRender,IDisposable,IPointerClickHandler,IConfigurable<T>
{
    [Header("--")]
    [SerializeField] protected Image iconImage;
    [SerializeField] protected TextMeshProUGUI nameText;
    
    [SerializeField] protected Graphic[] colorDependencyGraphics;

    public event Action<PointerEventData> OnClicked;

    public virtual void Dispose()
    {
        CleanClickEvent();
    }

    public void CleanClickEvent()
    {
        OnClicked = null;
    }
    
    public void RenderColor(ItemDataSO itemData, int colorDependency)
    {
        iconImage.sprite = itemData.ItemIcon;
        foreach (Graphic g in colorDependencyGraphics)
        {
            switch (itemData.ItemType)
            {
                case ItemType.Accessory:
                    g.color = ColorHelper.GetColorByRarity(colorDependency);
                    break;
                case ItemType.Weapon:
                    g.color = ColorHelper.GetColorByLevel(colorDependency);
                    break;
                default:
                    Debug.LogWarning($"需要配置{itemData.ItemType}的颜色");
                    break;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(eventData);
    }

    public abstract void Configure(T resource);

    private void OnDestroy()
    {
        Dispose();
    }
}

public interface IConfigurable<T>
{
    public void Configure(T resource);
}