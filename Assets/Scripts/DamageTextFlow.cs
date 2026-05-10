using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamageTextFlow : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private TextMeshPro _damageText;

    [Header("旧逻辑兜底")]
    [SerializeField, Min(0.01f)] private float floatTime = 0.8f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField, Min(0f)] private float startScale = 0.5f;
    [SerializeField] private bool useFade = true;
    [SerializeField] private Color criticalColor = new(1f, 0.6f, 0f);
    [SerializeField, Min(0f)] private float criticalStartScale = 1.2f;
    [SerializeField, Min(0f)] private float criticalShakeStrength = 0.3f;

    private Sequence animSequence;
    private DamageTextVisualStyle normalFallbackStyle;
    private DamageTextVisualStyle criticalFallbackStyle;

    public void SetDamage(float damage, bool isCritical)
    {
        DamageTextVisualStyle fallbackStyle = GetLegacyFallbackStyle(isCritical);
        Play(new DamageTextViewData(damage, isCritical, transform.position), fallbackStyle);
    }

    public void Play(DamageTextViewData viewData, DamageTextVisualStyle style)
    {
        if (style == null)
        {
            style = GetLegacyFallbackStyle(viewData.IsCritical);
        }

        if (_damageText == null)
        {
            Debug.LogWarning($"{nameof(DamageTextFlow)} 缺少 TextMeshPro 引用，无法播放伤害飘字。", this);
            Destroy(gameObject);
            return;
        }

        transform.position = viewData.WorldPosition;
        ResetVisual(viewData, style);
        PlaySequence(style);
    }

    public void Stop()
    {
        animSequence?.Kill();
        animSequence = null;
    }

    private void ResetVisual(DamageTextViewData viewData, DamageTextVisualStyle style)
    {
        transform.localScale = Vector3.one * style.StartScale;

        _damageText.text = style.FormatDamage(viewData.Damage);
        _damageText.fontSize = style.FontSize;
        _damageText.fontStyle = style.FontStyle;
        _damageText.color = style.TextColor;
        _damageText.alpha = 1f;
        _damageText.enableVertexGradient = style.UseVertexGradient;
        _damageText.colorGradient = new VertexGradient(
            style.GradientTopColor,
            style.GradientTopColor,
            style.GradientBottomColor,
            style.GradientBottomColor);
    }

    private void PlaySequence(DamageTextVisualStyle style)
    {
        animSequence?.Kill();

        Vector3 startPosition = transform.position;
        float direction = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        float drift = style.HorizontalDrift > 0f ? UnityEngine.Random.Range(style.HorizontalDrift * 0.35f, style.HorizontalDrift) : 0f;
        Vector3 endPosition = startPosition + new Vector3(drift * direction, style.FloatDistance, 0f);

        animSequence = DOTween.Sequence();
        animSequence.Append(transform.DOScale(Vector3.one * style.PeakScale, style.PopDuration).SetEase(style.PopEase));
        animSequence.Append(transform.DOScale(Vector3.one * style.EndScale, style.SettleDuration).SetEase(style.SettleEase));
        animSequence.Insert(0f, transform.DOMove(endPosition, style.Lifetime).SetEase(style.MoveEase));

        if (style.ShakeStrength > 0f)
        {
            animSequence.Insert(0f, transform.DOShakePosition(
                style.ShakeDuration,
                style.ShakeStrength,
                style.ShakeVibrato,
                style.ShakeRandomness,
                false,
                true));
        }

        if (style.UseFade)
        {
            float fadeDuration = Mathf.Max(0.01f, style.Lifetime - style.FadeDelay);
            animSequence.Insert(style.FadeDelay, _damageText.DOFade(0f, fadeDuration).SetEase(style.FadeEase));
        }

        animSequence.OnComplete(HandleAnimationCompleted);
    }

    private void HandleAnimationCompleted()
    {
        animSequence = null;
        Destroy(gameObject);
    }

    private DamageTextVisualStyle GetLegacyFallbackStyle(bool isCritical)
    {
        if (normalFallbackStyle == null || criticalFallbackStyle == null)
        {
            normalFallbackStyle = DamageTextVisualStyle.CreateLegacyNormal(floatTime, normalColor, startScale, useFade);
            normalFallbackStyle.OnValidate();

            criticalFallbackStyle = DamageTextVisualStyle.CreateLegacyCritical(
                floatTime,
                criticalColor,
                criticalStartScale,
                useFade,
                criticalShakeStrength);
            criticalFallbackStyle.OnValidate();
        }

        DamageTextVisualStyle style = isCritical ? criticalFallbackStyle : normalFallbackStyle;
        style.OnValidate();
        return style;
    }

    private void OnDisable()
    {
        Stop();
    }

    private void OnDestroy()
    {
        Stop();
    }

    private void OnValidate()
    {
        if (_damageText == null)
        {
            _damageText = GetComponentInChildren<TextMeshPro>();
        }

        floatTime = Mathf.Max(0.01f, floatTime);
        startScale = Mathf.Max(0f, startScale);
        criticalStartScale = Mathf.Max(0f, criticalStartScale);
        criticalShakeStrength = Mathf.Max(0f, criticalShakeStrength);
    }
}
