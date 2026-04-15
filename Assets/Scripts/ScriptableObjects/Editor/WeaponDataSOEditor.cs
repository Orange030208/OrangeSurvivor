#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponDataSO))]
public class WeaponDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        WeaponDataSO weaponData = (WeaponDataSO)target;
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Weapon Data Guide", EditorStyles.boldLabel);

        Weapon weaponPrefab = weaponData.WeaponPrefab;
        if (weaponPrefab is MeleeWeapon)
        {
            EditorGUILayout.HelpBox(
                "当前 Weapon Prefab 是近战武器：\n" +
                "• Construction Scheme = Default 时，会按 WeaponDataSO 自动下发 icon 与默认前向角度；\n" +
                "• Melee Hit Detection > Melee Hit Box Size 会作为命中盒尺寸；\n" +
                "• Melee Hit Detection > Melee Hit Offset 会作为 hitDetectionTransform 的局部偏移；\n" +
                "• 近战命中检测现在不再依赖场景里的 BoxCollider2D；\n" +
                "• 命中范围由 hitDetectionTransform 的位置/旋转 + 尺寸共同决定；\n" +
                "• 可在 Scene 视图里通过 Gizmos 查看最终命中盒。",
                MessageType.Info);
        }
        else if (weaponPrefab is RangeWeapon)
        {
            EditorGUILayout.HelpBox(
                "当前 Weapon Prefab 是远程武器：\n" +
                "• Construction Scheme = Default 时，会按 WeaponDataSO 自动下发 icon 与默认前向角度；\n" +
                "• Melee Hit Box Size / Melee Hit Offset 对远程攻击不会生效；\n" +
                "• 可以忽略这些字段，保留默认值即可。",
                MessageType.None);
        }
        else if (weaponPrefab == null)
        {
            EditorGUILayout.HelpBox(
                "尚未指定 Weapon Prefab。\n" +
                "• 先绑定 weaponPrefab，Inspector 才能提示当前是近战还是远程配置。",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "当前 Weapon Prefab 不是已知的近战/远程类型。\n" +
                "• Construction Scheme 用来选择运行时装配路径；\n" +
                "• 如果它使用近战命中窗口逻辑，可以配置 Melee Hit Box Size / Melee Hit Offset；\n" +
                "• 否则可忽略这些字段。",
                MessageType.None);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
