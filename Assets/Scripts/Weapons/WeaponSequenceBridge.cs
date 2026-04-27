using System;
using UnityEngine;

/// <summary>
/// 把 ScriptableObject 定义的攻击序列，桥接到场景中的 Transform。
/// 它只负责两件事：
/// 1. 驱动武器/子节点的位移与旋转序列；
/// 2. 把序列中的事件帧转发给武器逻辑层。
/// </summary>
public class WeaponSequenceBridge : MonoBehaviour
{
    [Header("Inspector")]
    [Tooltip("真正执行位移/旋转序列的目标。为空时默认驱动当前物体本身。")]
    [SerializeField] private Transform animatedTransform;

    private WeaponMotionSequencePlayer sequencePlayer;

    public bool IsPlaying => sequencePlayer != null && sequencePlayer.IsPlaying;
    public event Action<WeaponSequenceEventType, int> SequenceEventTriggered;
    public event Action SequenceCompleted;

    private void Awake()
    {
        // 如果没有显式指定动画目标，就默认驱动当前物体自身。
        // 如果后续要把逻辑根节点和可视节点拆开，这里可以接 WeaponSequenceAnchor.AnimatedTransform。
        Transform target = animatedTransform != null ? animatedTransform : transform;
        sequencePlayer = new WeaponMotionSequencePlayer(target);
        sequencePlayer.EventTriggered += OnSequenceEventTriggered;
        sequencePlayer.Completed += OnSequenceCompleted;
    }

    private void Update()
    {
        if (!GameManager.Instance.IsSimulationRunning)
        {
            return;
        }

        sequencePlayer.Tick(Time.deltaTime);
    }

    /// <summary>
    /// 把当前姿态缓存为“默认待机姿态”。
    /// 如果运行时动态切换了武器模型或初始角度，调用它可以重置序列播放基准。
    /// </summary>
    public void CacheDefaultPose()
    {
        sequencePlayer.CacheDefaultPose();
    }

    public void Play(AttackSequenceDefinitionSO sequence, float durationOverride = -1f)
    {
        sequencePlayer.Play(sequence, durationOverride);
    }

    public void Play(AttackSequenceDefinitionSO sequence, Vector2 targetLocalOffset, float durationOverride = -1f)
    {
        sequencePlayer.Play(sequence, targetLocalOffset, durationOverride);
    }

    public void Stop(bool restoreDefaultPose = true)
    {
        sequencePlayer.Stop(restoreDefaultPose);
    }

    private void OnDestroy()
    {
        if (sequencePlayer == null)
        {
            return;
        }

        sequencePlayer.EventTriggered -= OnSequenceEventTriggered;
        sequencePlayer.Completed -= OnSequenceCompleted;
    }

    private void OnSequenceEventTriggered(WeaponSequenceEventType eventType, int eventKey)
    {
        SequenceEventTriggered?.Invoke(eventType, eventKey);
    }

    private void OnSequenceCompleted()
    {
        SequenceCompleted?.Invoke();
    }
}
