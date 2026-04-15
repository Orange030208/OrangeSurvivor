using DG.Tweening;
using UnityEngine;

/// <summary>
/// 命中反馈管理器：
/// - 监听受伤与死亡事件；
/// - 在世界空间生成极轻量的命中/暴击/击杀反馈；
/// - 只负责表现，不参与伤害与状态逻辑。
/// </summary>
public class HitFeedbackManager : MonoBehaviour
{
    [Header("命中反馈")]
    [SerializeField] private float hitMarkerDuration = 0.16f;
    [SerializeField] private float hitMarkerStartScale = 0.45f;
    [SerializeField] private float criticalHitScale = 0.75f;
    [SerializeField] private Color hitMarkerColor = new(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color criticalHitColor = new(1f, 0.76f, 0.28f, 1f);

    [Header("击杀反馈")]
    [SerializeField] private float killBurstDuration = 0.24f;
    [SerializeField] private float killBurstScale = 0.8f;
    [SerializeField] private Color killBurstColor = new(1f, 0.38f, 0.12f, 0.95f);

    private void OnEnable()
    {
        GameEventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
        GameEventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<EntityDamagedEvent>(OnEntityDamaged);
        GameEventBus.Unsubscribe<EntityDiedEvent>(OnEntityDied);
    }

    private void OnEntityDamaged(EntityDamagedEvent eventData)
    {
        if (eventData.Entity is not Enemy)
        {
            return;
        }

        CreateHitMarker(
            eventData.DamageInfo.position,
            eventData.DamageInfo.isCritical ? criticalHitColor : hitMarkerColor,
            eventData.DamageInfo.isCritical ? criticalHitScale : hitMarkerStartScale,
            hitMarkerDuration);
    }

    private void OnEntityDied(EntityDiedEvent eventData)
    {
        if (eventData.Entity is not Enemy)
        {
            return;
        }

        CreateHitMarker(eventData.Position, killBurstColor, killBurstScale, killBurstDuration);
    }

    private void CreateHitMarker(Vector2 position, Color color, float startScale, float duration)
    {
        GameObject marker = new("HitFeedbackMarker");
        marker.transform.SetParent(transform, false);
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * startScale;

        SpriteRenderer spriteRenderer = marker.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateSquareSprite();
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = 999;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(marker.transform.DOScale(Vector3.one * (startScale * 2.1f), duration).SetEase(Ease.OutQuad));
        sequence.Join(spriteRenderer.DOFade(0f, duration));
        sequence.OnComplete(() => Destroy(marker));
    }

    private static Sprite runtimeSquareSprite;

    private static Sprite CreateSquareSprite()
    {
        if (runtimeSquareSprite != null)
        {
            return runtimeSquareSprite;
        }

        Texture2D texture = new(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        runtimeSquareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return runtimeSquareSprite;
    }
}
