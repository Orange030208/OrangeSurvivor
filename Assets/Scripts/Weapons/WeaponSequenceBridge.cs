using System;
using UnityEngine;

public class WeaponSequenceBridge : MonoBehaviour
{
    [SerializeField] private Transform animatedTransform;

    private WeaponMotionSequencePlayer sequencePlayer;

    public bool IsPlaying => sequencePlayer != null && sequencePlayer.IsPlaying;
    public event Action<WeaponSequenceEventContext> SequenceEventTriggered;
    public event Action SequenceCompleted;

    private void Awake()
    {
        Transform target = animatedTransform != null ? animatedTransform : transform;
        sequencePlayer = new WeaponMotionSequencePlayer(target);
        sequencePlayer.EventTriggered += OnSequenceEventTriggered;
        sequencePlayer.Completed += OnSequenceCompleted;
    }

    private void Update()
    {
        sequencePlayer?.Tick(Time.deltaTime);
    }

    public void CacheDefaultPose()
    {
        sequencePlayer?.CacheDefaultPose();
    }

    public void Play(AttackSequenceDefinitionSO sequence)
    {
        sequencePlayer?.Play(sequence);
    }

    public void Stop(bool restoreDefaultPose = true)
    {
        sequencePlayer?.Stop(restoreDefaultPose);
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

    private void OnSequenceEventTriggered(WeaponSequenceEventContext eventContext)
    {
        SequenceEventTriggered?.Invoke(eventContext);
    }

    private void OnSequenceCompleted()
    {
        SequenceCompleted?.Invoke();
    }
}
