using UnityEngine;
using UnityEngine.Serialization;

public enum ItemType
{
    Weapon,
    Accessory
}

public abstract class ItemDataSO : ScriptableObject
{
    [SerializeField] protected string itemName;
    [SerializeField] protected Sprite itemIcon;
    [SerializeField] protected int itemPrice;
    [SerializeField] protected ItemType itemType;

    public string ItemName => itemName;
    public Sprite ItemIcon => itemIcon;
    public int ItemPrice => itemPrice;
    public ItemType ItemType => itemType;
}
