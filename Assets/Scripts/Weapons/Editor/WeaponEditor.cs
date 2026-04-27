#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 武器运行时调试 Inspector：
/// - 展示当前目标、攻击状态、运行时属性；
/// - 展示当前序列原始时长、实际播放时长和压缩比例；
/// - 在 Play Mode 下提供一键攻击和刷新属性按钮；
/// - 帮助在没有完整可视化工具时，快速调试攻击序列与弹射物配置。
/// 当前强制攻击通过反射调用受保护的 BeginAttack，仅用于编辑器调试。
/// </summary>
[CustomEditor(typeof(Weapon), true)]
public class WeaponEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Weapon weapon = (Weapon)target;

        EditorGUILayout.Space(8f);
        using (new EditorGUI.DisabledScope(weapon.WeaponData == null))
        {
            if (GUILayout.Button("打开攻击序列工作台"))
            {
                AttackSequenceStudioWindow.Open(weapon.WeaponData);
            }
        }

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Runtime Debug", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Level", weapon.Level.ToString());
        EditorGUILayout.LabelField("Is Attacking", weapon.IsAttacking ? "Yes" : "No");

        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Damage", weapon.Damage.ToString("0.##"));
            EditorGUILayout.LabelField("Attack Interval", weapon.AttackInterval.ToString("0.###"));
            EditorGUILayout.LabelField("Range", weapon.Range.ToString("0.##"));
            EditorGUILayout.LabelField("Critical Chance", weapon.CriticalChance.ToString("0.##"));
            EditorGUILayout.LabelField("Critical Multiplier", weapon.CriticalMultiplier.ToString("0.##"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Sequence Timing", EditorStyles.boldLabel);
            AttackSequenceDefinitionSO sequence = GetCurrentSequence(weapon);
            float originalDuration = sequence != null ? Mathf.Max(0f, sequence.Duration) : 0f;
            float timingWindow = Mathf.Max(0f, weapon.AttackInterval * (weapon.WeaponData != null ? weapon.WeaponData.AttackSequenceOccupancy : 0.85f));
            float effectiveDuration = sequence != null ? Mathf.Min(Mathf.Max(0.01f, sequence.Duration), Mathf.Max(0.01f, timingWindow)) : 0f;
            float compressionRatio = originalDuration <= 0.0001f ? 1f : effectiveDuration / originalDuration;
            EditorGUILayout.LabelField("Original Duration", originalDuration.ToString("0.###") + "s");
            EditorGUILayout.LabelField("Effective Duration", effectiveDuration.ToString("0.###") + "s");
            EditorGUILayout.LabelField("Timing Window", timingWindow.ToString("0.###") + "s");
            EditorGUILayout.LabelField("Compression Ratio", (compressionRatio * 100f).ToString("0.#") + "%");

            if (weapon is RangeWeapon)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Range Weapon Tips", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "调远程时建议：\n" +
                    "1. 先用 Force Attack Current Target 强制打一发；\n" +
                    "2. 在 AttackSequenceDefinitionSO 中确认是否真的配置了 SpawnProjectile 事件；\n" +
                    "3. 在 AttackSequenceDefinitionSO 中直接切 ProjectileDefinition；\n" +
                    "4. 用 ProjectileDefinitionSO 调整 speed / damage / lifetime 倍率；\n" +
                    "5. stop aiming 和动画占比现在统一在 WeaponDataSO 里调。",
                    MessageType.Info);
            }

            if (weapon is MeleeWeapon)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Melee Weapon Tips", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "调近战时建议：\n" +
                    "1. 先用 Force Attack Current Target 强制出手；\n" +
                    "2. 看序列里的 OpenHitWindow / CloseHitWindow 是否落在主挥击段；\n" +
                    "3. 配合 Scene 里的命中盒 Gizmo 看打击范围是否贴合动作。",
                    MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("进入 Play Mode 后可查看实时攻击参数、序列时长压缩结果，并使用调试按钮。", MessageType.Info);
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Refresh Runtime Stats"))
            {
                weapon.RefreshRuntimeStats();
                EditorUtility.SetDirty(weapon);
            }

            if (GUILayout.Button("Force Attack Current Target"))
            {
                TryForceAttackCurrentTarget(weapon);
                EditorUtility.SetDirty(weapon);
            }
        }
    }

    private static AttackSequenceDefinitionSO GetCurrentSequence(Weapon weapon)
    {
        if (weapon is MeleeWeapon meleeWeapon)
        {
            return meleeWeapon.DebugAttackSequence;
        }

        if (weapon is RangeWeapon rangeWeapon)
        {
            return rangeWeapon.DebugAttackSequence;
        }

        return null;
    }

    private static void TryForceAttackCurrentTarget(Weapon weapon)
    {
        var getTargetMethod = typeof(Weapon).GetMethod("GetCurrentTarget", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var beginAttackMethod = weapon.GetType().GetMethod("BeginAttack", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (getTargetMethod == null || beginAttackMethod == null)
        {
            Debug.LogWarning("Weapon debug inspector failed: required methods not found.", weapon);
            return;
        }

        Enemy currentTarget = getTargetMethod.Invoke(weapon, null) as Enemy;
        if (currentTarget == null)
        {
            Debug.LogWarning("Weapon debug inspector: no current target in range.", weapon);
            return;
        }

        beginAttackMethod.Invoke(weapon, new object[] { currentTarget });
    }
}
#endif
