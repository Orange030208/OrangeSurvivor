using UnityEngine;

public class PlayerWeapons : MonoBehaviour
{
    [SerializeField] private WeaponPosition[] weaponPositions;
    [SerializeField] private Transform weaponsParentTransform; // 武器实例化的父节点
    
    public void AddWeapon(WeaponDataSO weaponData, int level)
    {
        print($"选中了{weaponData.name} 等级:{level}");
        weaponPositions[Random.Range(0,weaponPositions.Length)].AssignWeapon(weaponData.WeaponPrefab,level);
    }
}