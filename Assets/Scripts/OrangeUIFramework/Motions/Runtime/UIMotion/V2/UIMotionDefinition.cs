
namespace Orange.UIFramework
{
    using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = UIFrameworkConstants.MOTION_DEFINITION_MENU_PATH, fileName = "UIMotionDefinition")]
// UI 动画配置资产。一个 Definition 通常对应一类 UI 或一个 Prefab，内部用 ClipId 暴露可播放动作。
public sealed class UIMotionDefinition : ScriptableObject
{
    // UI 动画默认使用非缩放时间，避免暂停菜单、结算界面等在 Time.timeScale = 0 时无法播放。
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private List<UIMotionClipDefinition> clips = new();

    public bool UseUnscaledTime => useUnscaledTime;
    public IReadOnlyList<UIMotionClipDefinition> Clips => clips;

    public bool TryGetClip(string clipId, out UIMotionClipDefinition clip)
    {
        clip = null;
        if (string.IsNullOrWhiteSpace(clipId) || clips == null)
        {
            return false;
        }

        // ClipId 作为外部调用契约，保持区分大小写的精确匹配，避免同名配置被意外命中。
        for (int i = 0; i < clips.Count; i++)
        {
            UIMotionClipDefinition candidate = clips[i];
            if (candidate == null)
            {
                continue;
            }

            if (!string.Equals(candidate.ClipId, clipId, StringComparison.Ordinal))
            {
                continue;
            }

            clip = candidate;
            return true;
        }

        return false;
    }

    public List<string> GetClipIds()
    {
        List<string> clipIds = new();
        if (clips == null)
        {
            return clipIds;
        }

        // 供 Inspector/配置面板生成选项列表；去重可以避免重复 ClipId 造成 UI 选择歧义。
        for (int i = 0; i < clips.Count; i++)
        {
            UIMotionClipDefinition clip = clips[i];
            if (clip == null || string.IsNullOrWhiteSpace(clip.ClipId))
            {
                continue;
            }

            if (!clipIds.Contains(clip.ClipId))
            {
                clipIds.Add(clip.ClipId);
            }
        }

        return clipIds;
    }
}
}
