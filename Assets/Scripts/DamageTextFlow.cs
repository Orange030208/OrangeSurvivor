using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamageTextFlow : MonoBehaviour
{
    [Header("基础飘字设置")]
    public float floatTime = 0.8f;
    public Color normalColor = Color.white;

    [Header("视觉效果")]
    public float startScale = 0.5f;
    public bool useFade = true;

    [Header("暴击专属效果")]
    public Color criticalColor = new Color(1f, 0.6f, 0f);
    public float criticalStartScale = 1.2f;
    public float criticalShakeStrength = 0.3f;

    [SerializeField] private TextMeshPro _damageText;
    private Sequence _animSequence;
    private bool _isCritical;

    private void OnEnable() { }

    public void SetDamage(int damage, bool isCritical)
    {
        _damageText.text = damage.ToString();
        _isCritical = isCritical;
        Play();
    }

    private void Play()
    {
        // 1. 初始化参数
        float currentStartScale = _isCritical ? criticalStartScale : startScale;
        Color currentColor = _isCritical ? criticalColor : normalColor;

        transform.localScale = Vector3.one * currentStartScale;
        _damageText.color = currentColor;
        _damageText.alpha = 1;

        // 2. 清理旧动画
        _animSequence?.Kill();
        _animSequence = DOTween.Sequence();

        // 3. 播放动画 (只有缩放 + 震动 + 渐隐)
        Ease scaleEase = _isCritical ? Ease.OutElastic : Ease.OutBack;
        float scaleDuration = _isCritical ? floatTime * 0.5f : floatTime * 0.3f;

        // 缩放动画
        _animSequence.Append(transform.DOScale(Vector3.one, scaleDuration).SetEase(scaleEase));

        // 暴击震动
        if (_isCritical)
        {
            _animSequence.Join(transform.DOShakePosition(
                scaleDuration, 
                criticalShakeStrength, 
                20, 
                90f, 
                false, 
                true
            ));
        }

        // 渐隐效果
        if (useFade)
        {
            float fadeDelay = _isCritical ? floatTime * 0.4f : floatTime * 0.5f;
            _animSequence.Append(_damageText.DOFade(0, floatTime * 0.5f).SetDelay(fadeDelay));
        }

        // 4. 结束回收
        _animSequence.OnComplete(DestroyDamageText);
    }

    private void DestroyDamageText()
    {
        _animSequence?.Kill();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        _animSequence?.Kill();
    }
}