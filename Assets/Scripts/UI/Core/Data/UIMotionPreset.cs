using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.UI_MOTION_PRESET, fileName = "UIMotionPreset")]
public class UIMotionPreset : ScriptableObject
{
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private List<UIMotionClip> clips = new();

    public bool UseUnscaledTime => useUnscaledTime;
    public IReadOnlyList<UIMotionClip> Clips => clips;

    public virtual List<UIMotionClip> CreateRuntimeClips()
    {
        List<UIMotionClip> runtimeClips = new();
        if (clips == null)
        {
            return runtimeClips;
        }

        for (int i = 0; i < clips.Count; i++)
        {
            UIMotionClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            runtimeClips.Add(CloneClip(clip));
        }

        return runtimeClips;
    }

    private static UIMotionClip CloneClip(UIMotionClip clip)
    {
        return new UIMotionClip
        {
            action = clip.action,
            pose = ClonePose(clip.pose),
            duration = clip.duration,
            ease = clip.ease,
            deactivateOnComplete = clip.deactivateOnComplete
        };
    }

    private static UIMotionPose ClonePose(UIMotionPose pose)
    {
        if (pose == null)
        {
            return new UIMotionPose();
        }

        return new UIMotionPose
        {
            fade = pose.fade,
            alpha = pose.alpha,
            move = pose.move,
            offset = pose.offset,
            scale = pose.scale,
            scaleMultiplier = pose.scaleMultiplier,
            scaleX = pose.scaleX,
            scaleXMultiplier = pose.scaleXMultiplier,
            scaleY = pose.scaleY,
            scaleYMultiplier = pose.scaleYMultiplier,
            rotate = pose.rotate,
            rotationZ = pose.rotationZ
        };
    }
}
