using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamageTextFlow : MonoBehaviour
{
    [Header("基础飘字设置")]
    public float floatHeight = 1.2f;
    public float floatTime = 0.8f;
    public float randomOffsetX = 0.3f;

    [Header("视觉效果")]
    public float startScale = 0.5f;
    public bool useFade = true;

    [SerializeField]private TextMeshPro _damageText;
    private Sequence _animSequence; // 用Sequence管理动画，安全无报错

    private void OnEnable()
    {
        transform.localScale = Vector3.one * startScale;
        // 随机目标位置
        Vector3 targetPos = transform.position + new Vector3(
            Random.Range(-randomOffsetX, randomOffsetX),
            floatHeight,
            0
        );

        // 3. 创建动画序列（修复Join报错的核心！）
        _animSequence = DOTween.Sequence();

        // 并行添加：位移 + 缩放
        _animSequence.Append(transform.DOMove(targetPos, floatTime).SetEase(Ease.OutCubic));
        _animSequence.Join(transform.DOScale(Vector3.one, floatTime * 0.3f).SetEase(Ease.OutBack));

        // 渐隐效果
        if (useFade)
        {
            _damageText.alpha = 1;
            _animSequence.Append(_damageText.DOFade(0, floatTime * 0.5f).SetDelay(floatTime * 0.5f));
        }

        // 动画结束销毁
        _animSequence.OnComplete(DestroyDamageText);
    }

    /// <summary>
    /// 设置伤害数字（外部调用）
    /// </summary>
    public void SetDamage(int damage)
    {
        _damageText.text = damage.ToString();
    }

    // 安全销毁
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