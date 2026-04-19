using UnityEngine;

/// <summary>
/// 武器槽位：
/// WeaponsHolder 不直接实例化武器，而是把实例化职责交给每个 WeaponPosition。
/// 这样玩家身上可以有多个明确的挂点，每个挂点决定武器的初始局部位置与朝向。
/// 如果后续要做左右手、肩炮、背挂武器，也可以继续复用这层结构。
/// </summary>
public class WeaponPosition : MonoBehaviour
{
    public Weapon Weapon { get; private set; }

    /// <summary>
    /// 在当前挂点下实例化武器 prefab，并设置初始等级。
    /// </summary>
    public Weapon AssignWeapon(WeaponDataSO weaponData, int level)
    {
        if (weaponData == null || weaponData.WeaponPrefab == null)
        {
            return null;
        }

        Weapon = Instantiate(weaponData.WeaponPrefab, transform);
        Weapon.transform.localPosition = Vector3.zero;
        Weapon.transform.localRotation = Quaternion.identity;
        Weapon.SetWeaponData(weaponData);
        Weapon.SetLevel(level);
        return Weapon;
    }

    /// <summary>
    /// 从当前挂点移除指定武器实例。
    /// </summary>
    public bool RemoveWeapon(Weapon weapon)
    {
        if (Weapon == null || weapon == null || Weapon != weapon)
        {
            return false;
        }

        Destroy(Weapon.gameObject);
        Weapon = null;
        return true;
    }
}
