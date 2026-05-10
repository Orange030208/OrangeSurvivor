using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class DamageFlashComponent : EntityComponentBase
{
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    [Header("闪烁反馈")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;

    private Entity owner;
    private HealthComponent healthComponent;
    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;
    private float flashTimer;
    private bool isFlashing;

    public override Entity Owner => owner;
    public override int Priority => PriorityPreset.Latest;

    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        healthComponent = owner.GetComponent<HealthComponent>();
        EntityRenderer entityRenderer = owner.EntityRenderer;
        spriteRenderer = entityRenderer.SpriteRenderer;
        propertyBlock = new MaterialPropertyBlock();
        SetFlashAmount(0f);
    }

    public override void OnEnableComponent()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDamaged += OnDamaged;
            healthComponent.OnDeathSequenceStarted += OnDeathSequenceStarted;
        }
    }

    public override void OnDisableComponent()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDamaged -= OnDamaged;
            healthComponent.OnDeathSequenceStarted -= OnDeathSequenceStarted;
        }

        SetFlashAmount(0f);
    }

    public override void OnTick(float deltaTime)
    {
        if (!isFlashing || spriteRenderer == null)
        {
            return;
        }

        flashTimer -= deltaTime;
        if (flashTimer > 0f)
        {
            return;
        }

        SetFlashAmount(0f);
        flashTimer = 0f;
        isFlashing = false;
    }

    private void OnDamaged(HitResult result)
    {
        if (result.Target != owner ||
            result.IsCancelled ||
            result.IsDodged ||
            result.FinalDamage <= 0f ||
            spriteRenderer == null)
        {
            return;
        }

        flashTimer = Mathf.Max(0.01f, flashDuration);
        isFlashing = true;
        SetFlashAmount(1f);
    }

    private void OnDeathSequenceStarted()
    {
        flashTimer = 0f;
        isFlashing = false;
        SetFlashAmount(0f);
    }

    private void SetFlashAmount(float amount)
    {
        if (spriteRenderer == null || propertyBlock == null)
        {
            return;
        }

        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(FlashAmountId, Mathf.Clamp01(amount));
        propertyBlock.SetColor(FlashColorId, flashColor);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnValidate()
    {
        flashDuration = Mathf.Max(0.01f, flashDuration);
    }
}
