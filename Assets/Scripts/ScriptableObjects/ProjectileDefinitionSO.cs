using UnityEngine;

/// <summary>
/// 可复用的弹射物定义资源。
/// 它不限定只能给远程武器使用，近战武器后续如果需要在命中时生成一圈弹射物，
/// 也可以直接复用同一份定义。
/// 当前主要负责描述：
/// - 这是一种什么弹射物；
/// - 它使用哪个弹射物模板；
/// - 可选的图标、调试颜色、默认表现资源与基础倍率。
/// 这些字段未必都会立刻被运行时全部消费，但先集中在同一份定义里，
/// 后续扩展命中表现、发射表现时就不用再把配置拆散到多个地方。
/// </summary>
[CreateAssetMenu(fileName = "Projectile Definition", menuName = "SO/ProjectileDefinition", order = 0)]
public class ProjectileDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("给策划和开发看的稳定 id，例如 normal-shot / explosive-ring。")]
    [SerializeField] private string id;

    [Tooltip("Inspector 里的可读名称，仅用于配置可读性。")]
    [SerializeField] private string displayName;

    [Header("Runtime")]
    [Tooltip("这类弹射物运行时使用的模板。具体 prefab 由统一资源入口按模板解析。")]
    [SerializeField] private ProjectileTemplateKind templateKind = ProjectileTemplateKind.Common;
    [Tooltip("基础伤害倍率。当前 Bullet 已开始读取它。")]
    [SerializeField] private float damageMultiplier = 1f;
    [Tooltip("基础速度倍率。当前 Bullet 已开始读取它。")]
    [SerializeField] private float speedMultiplier = 1f;
    [Tooltip("基础寿命倍率。当前 Bullet 已开始读取它。")]
    [SerializeField] private float lifetimeMultiplier = 1f;

    [Header("Presentation")]
    [Tooltip("可选图标，后续 UI 或调试面板可直接复用。")]
    [SerializeField] private Sprite icon;
    [Tooltip("调试颜色，方便以后做弹射物调试可视化。")]
    [SerializeField] private Color debugColor = Color.white;
    [Tooltip("可选发射音效键。")]
    [SerializeField] private AudioSfxKey launchSfxKey = AudioSfxKey.None;
    [Tooltip("可选发射特效预制体。当前主流程尚未自动生成，先作为定义层预留。")]
    [SerializeField] private GameObject launchVfxPrefab;
    [Tooltip("可选命中特效预制体。当前主流程尚未自动生成，先作为定义层预留。")]
    [SerializeField] private GameObject impactVfxPrefab;

    public string Id => id;
    public string DisplayName => displayName;
    public ProjectileTemplateKind TemplateKind => templateKind;
    public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);
    public float SpeedMultiplier => Mathf.Max(0f, speedMultiplier);
    public float LifetimeMultiplier => Mathf.Max(0f, lifetimeMultiplier);
    public Sprite Icon => icon;
    public Color DebugColor => debugColor;
    public AudioSfxKey LaunchSfxKey => launchSfxKey;
    public GameObject LaunchVfxPrefab => launchVfxPrefab;
    public GameObject ImpactVfxPrefab => impactVfxPrefab;
}
