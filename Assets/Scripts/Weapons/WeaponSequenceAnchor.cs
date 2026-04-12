using UnityEngine;

/// <summary>
/// 可选的序列锚点组件。
/// 用于把武器的“逻辑根节点”和“实际播放动作的可视节点”拆开：
/// - 逻辑根节点仍然负责索敌、朝向、碰撞等；
/// - AnimatedTransform 则专门负责播放攻击动作。
/// 当前主流程里还没有强制接入这个组件，属于可选辅助结构；
/// 如果后续需要更复杂的武器层级动画，可以把 WeaponSequenceBridge 直接接到它上面。
/// </summary>
public class WeaponSequenceAnchor : MonoBehaviour
{
    [Header("Inspector")]
    [Tooltip("真正执行序列位移/旋转的目标。为空时默认使用当前物体。")]
    [SerializeField] private Transform animatedTransform;

    public Transform AnimatedTransform => animatedTransform != null ? animatedTransform : transform;

    private void Reset()
    {
        animatedTransform = transform;
    }
}
