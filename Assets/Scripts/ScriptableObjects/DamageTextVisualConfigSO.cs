using UnityEngine;

[CreateAssetMenu(fileName = "Damage Text Visual Config", menuName = ScriptableObjectMenuPaths.DAMAGE_TEXT_VISUAL_CONFIG, order = 0)]
public class DamageTextVisualConfigSO : ScriptableObject
{
    public static readonly Vector2 DEFAULT_SPAWN_OFFSET = new(0f, 1.5f);

    [Header("Visibility")]
    [Tooltip("开启后只显示敌人受到的伤害，保持战斗 HUD 聚焦。")]
    [SerializeField] private bool showEnemyDamageOnly = true;
    [Tooltip("开启后会忽略 0 或更低的伤害数字。")]
    [SerializeField] private bool hideZeroDamage = true;

    [Header("Spawn")]
    [SerializeField] private Vector2 spawnOffset = DEFAULT_SPAWN_OFFSET;
    [SerializeField] [Min(0f)] private float spawnSpreadX = 0.18f;

    [Header("Styles")]
    [SerializeField] private DamageTextVisualStyle normalStyle = DamageTextVisualStyle.CreateDefaultNormal();
    [SerializeField] private DamageTextVisualStyle criticalStyle = DamageTextVisualStyle.CreateDefaultCritical();

    public bool ShowEnemyDamageOnly => showEnemyDamageOnly;
    public bool HideZeroDamage => hideZeroDamage;
    public Vector2 SpawnOffset => spawnOffset;
    public float SpawnSpreadX => Mathf.Max(0f, spawnSpreadX);

    public DamageTextVisualStyle GetStyle(bool isCritical)
    {
        if (isCritical)
        {
            return criticalStyle ?? DamageTextVisualStyle.CreateDefaultCritical();
        }

        return normalStyle ?? DamageTextVisualStyle.CreateDefaultNormal();
    }

    private void OnValidate()
    {
        spawnSpreadX = Mathf.Max(0f, spawnSpreadX);

        if (normalStyle == null)
        {
            normalStyle = DamageTextVisualStyle.CreateDefaultNormal();
        }

        if (criticalStyle == null)
        {
            criticalStyle = DamageTextVisualStyle.CreateDefaultCritical();
        }

        normalStyle.OnValidate();
        criticalStyle.OnValidate();
    }
}
