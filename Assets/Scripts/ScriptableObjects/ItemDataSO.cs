using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum ItemType
{
    Weapon,
    Accessory
}

public abstract class ItemDataSO : ScriptableObject,IDescribable
{
    [SerializeField] protected string itemName;
    [SerializeField] protected Sprite itemIcon;
    [SerializeField] protected int itemPrice;
    [SerializeField] protected ItemType itemType;
    [SerializeField] protected string itemDescription;

    public string ItemName => itemName;
    public Sprite ItemIcon => itemIcon;
    public int ItemPrice => itemPrice;
    public ItemType ItemType => itemType;

    public virtual string Title => itemName;
    public virtual Sprite Icon => itemIcon;
    public virtual string Description => itemDescription;
    public abstract IEnumerable<DescriptorInfo> GetExtraInfos();
}
