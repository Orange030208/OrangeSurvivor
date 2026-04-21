using UnityEngine;

[CreateAssetMenu(fileName = "Projectile Definition", menuName = "SO/ProjectileDefinition", order = 0)]
public class ProjectileDefinitionSO : ScriptableObject
{
    [Header("标识")]
    [Tooltip("用于配置和调试的稳定子弹唯一ID，例如 projectile_common 或 fire_ball")]
    [SerializeField] private string id;
    [Tooltip("在检视面板中显示的易读名称，供策划和调试使用")]
    [SerializeField] private string displayName;

    [Header("模板")]
    [Tooltip("选择用于解析运行时子弹预制体的共享行为模板")]
    [SerializeField] private ProjectileTemplateKind templateKind = ProjectileTemplateKind.Common;
    [Tooltip("可选的预制体覆盖。常规情况留空，优先使用共享模板路径")]
    [SerializeField] private Projectile projectilePrefab;

    [Header("运行时")]
    [Tooltip("对发射上下文的基础伤害施加的倍率")]
    [SerializeField] private float damageMultiplier = 1f;
    [Tooltip("对模板预制体的基础子弹速度施加的倍率")]
    [SerializeField] private float speedMultiplier = 1f;
    [Tooltip("对子弹过期前的模板基础生命周期施加的倍率")]
    [SerializeField] private float lifetimeMultiplier = 1f;
    [Tooltip("对生成的子弹根节点施加的统一缩放倍率")]
    [SerializeField] private float scaleMultiplier = 1f;

    [Header("表现效果")]
    [Tooltip("为模板的精灵渲染器提供的可选精灵覆盖")]
    [SerializeField] private Sprite sprite;
    [Tooltip("发射时注入到模板动画器的可选动画控制器")]
    [SerializeField] private RuntimeAnimatorController animatorController;
    [Tooltip("为子弹精灵渲染器提供的可选材质覆盖")]
    [SerializeField] private Material material;
    [Tooltip("子弹精灵渲染器的排序层级覆盖")]
    [SerializeField] private int sortingOrder;
    [Tooltip("供UI、调试面板或内容工具使用的可选图标")]
    [SerializeField] private Sprite icon;
    [Tooltip("用于场景辅助图标或子弹可视化工具的调试颜色")]
    [SerializeField] private Color debugColor = Color.white;
    [Tooltip("子弹发射时播放的音效")]
    [SerializeField] private AudioSfxKey launchSfxKey = AudioSfxKey.None;
    [Tooltip("子弹命中有效目标时播放的音效")]
    [SerializeField] private AudioSfxKey impactSfxKey = AudioSfxKey.None;
    [Tooltip("发射时生成的可选特效预制体")]
    [SerializeField] private GameObject launchVfxPrefab;
    [Tooltip("命中时生成的可选特效预制体")]
    [SerializeField] private GameObject impactVfxPrefab;

    [Header("朝向设置")]
    [Tooltip("启用后，子弹会旋转以匹配自身的移动方向")]
    [SerializeField] private bool useDirectionFacing = true;
    [Tooltip("在方向朝向逻辑后施加的额外Z轴旋转偏移")]
    [SerializeField] private float rotationOffset;
    [Tooltip("子弹发射时，在注入的动画器上触发的可选触发器名称")]
    [SerializeField] private string launchAnimationTrigger;

    public string Id => id;
    public string DisplayName => displayName;
    public ProjectileTemplateKind TemplateKind => templateKind;
    public Projectile PrefabOverride => projectilePrefab;
    public Projectile ProjectilePrefab => ProjectileFactory.ResolveProjectilePrefab(this);
    public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);
    public float SpeedMultiplier => Mathf.Max(0f, speedMultiplier);
    public float LifetimeMultiplier => Mathf.Max(0f, lifetimeMultiplier);
    public float ScaleMultiplier => Mathf.Max(0.01f, scaleMultiplier);
    public Sprite Sprite => sprite;
    public RuntimeAnimatorController AnimatorController => animatorController;
    public Material Material => material;
    public int SortingOrder => sortingOrder;
    public Sprite Icon => icon;
    public Color DebugColor => debugColor;
    public AudioSfxKey LaunchSfxKey => launchSfxKey;
    public AudioSfxKey ImpactSfxKey => impactSfxKey;
    public GameObject LaunchVfxPrefab => launchVfxPrefab;
    public GameObject ImpactVfxPrefab => impactVfxPrefab;
    public bool UseDirectionFacing => useDirectionFacing;
    public float RotationOffset => rotationOffset;
    public string LaunchAnimationTrigger => launchAnimationTrigger;
}