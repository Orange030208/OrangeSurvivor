namespace Orange.UIFramework
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using UnityEngine;

    [DisallowMultipleComponent]
    // UI 动画系统的运行时入口：负责把组件内配置的 Clip 解析成 DOTween 播放序列。
    // 这个组件只负责播放任意 Clip；页面进出场等生命周期语义由 UIMotionTransition/UIMotionDirector 负责。
    public sealed class UIMotionPlayer : MonoBehaviour, IUIRuntimeMotion
    {
        private sealed class ActiveTween
        {
            public string Channel;
            public UIMotionClipDefinition Clip;
            public Tween Tween;
        }

        // 动效配置直接跟随组件序列化，避免单组件使用的动画还要额外维护 ScriptableObject 资产。
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private List<UIMotionClipDefinition> clips = new();

        // 打开对象时是否重新捕获默认状态。用于 UI 被外部逻辑调整后，再次启用时以当前状态作为 Initial。
        [SerializeField] private bool refreshDefaultsOnEnable = true;

        // 销毁时清理所有仍在播放的 Tween，避免回调继续访问已经销毁的 UI 对象。
        [SerializeField] private bool stopAllChannelsOnDestroy = true;

        // 以 Channel 为单位记录活跃 Tween，保证“显示/隐藏”“悬停/按下”等动画可以独立冲突处理。
        private readonly Dictionary<string, List<ActiveTween>> activeTweensByChannel = new(StringComparer.Ordinal);
        private readonly List<ActiveTween> activeTweenBuffer = new();
        private readonly List<UIMotionClipDefinition> clipBuffer = new();
        private readonly UIMotionTargetCache targetCache = new();
        private bool initialized;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void OnEnable()
        {
            InitializeIfNeeded();
            if (refreshDefaultsOnEnable)
            {
                RefreshDefaults();
            }

            PlayAutoClipsOnEnable();
        }

        private void OnDisable()
        {
            StopClipsOnDisable();
        }

        private void OnDestroy()
        {
            if (stopAllChannelsOnDestroy)
            {
                Kill();
            }
        }

        public Tween Play(string clipId, float delay = 0f)
        {
            InitializeIfNeeded();
            if (!TryGetClipWithFallback(clipId, out UIMotionClipDefinition clip))
            {
                Debug.LogWarning($"{nameof(UIMotionPlayer)} '{name}' could not find clip '{clipId}'.", this);
                return null;
            }

            // 播放前先按 Clip 的冲突策略处理旧动画。默认只停同 Channel，避免按钮 hover 打断页面进出场。
            ApplyConflictPolicy(clip);
            UIMotionPlaybackContext context = new(this, clip, UIMotionPlaybackMode.PlayToEnd, 0f,
                clip.DurationScale);
            Tween tween = BuildClipTween(clip, context);
            if (tween == null)
            {
                return null;
            }

            tween = ApplyLoopPolicy(clip, tween);
            tween = WrapInitialDelay(tween, delay);
            tween.SetUpdate(useUnscaledTime);
            RegisterChannelTween(clip.Channel, clip, tween);
            return tween;
        }

        public UniTask PlayAsync(string clipId, CancellationToken cancellationToken, float delay = 0f)
        {
            Tween tween = Play(clipId, delay);
            return tween.WaitForCompletionAsync(cancellationToken);
        }

        public void SetImmediate(string clipId, bool atEnd = true)
        {
            InitializeIfNeeded();
            if (!TryGetClipWithFallback(clipId, out UIMotionClipDefinition clip))
            {
                Debug.LogWarning(
                    $"{nameof(UIMotionPlayer)} '{name}' could not find clip '{clipId}' for immediate sampling.", this);
                return;
            }

            // 立即采样用于页面入场前的隐藏态、或跳过动画时的完成态。
            // 这里不注册 Tween，因为 SampleStart/SampleEnd 只写入目标属性，不产生持续播放的序列。
            StopChannel(clip.Channel);
            UIMotionPlaybackMode mode = atEnd ? UIMotionPlaybackMode.SampleEnd : UIMotionPlaybackMode.SampleStart;
            UIMotionPlaybackContext context = new(this, clip, mode, 0f, clip.DurationScale);
            BuildClipTween(clip, context);
        }

        public void StopChannel(string channel)
        {
            string resolvedChannel = ResolveChannel(channel);
            if (!activeTweensByChannel.TryGetValue(resolvedChannel, out List<ActiveTween> tweens))
            {
                return;
            }

            // 从后往前 Kill，配合 OnKill 回调移除注册项时不会影响尚未遍历的元素。
            for (int i = tweens.Count - 1; i >= 0; i--)
            {
                tweens[i]?.Tween?.Kill();
            }

            activeTweensByChannel.Remove(resolvedChannel);
        }

        public void RefreshDefaults()
        {
            InitializeIfNeeded();
            // Initial 值来自快照而不是 Prefab 原始值。需要以当前布局作为新起点时，主动调用这里刷新。
            targetCache.RefreshSnapshots(clips);
        }

        public void Kill()
        {
            List<string> channels = new(activeTweensByChannel.Keys);
            for (int i = 0; i < channels.Count; i++)
            {
                StopChannel(channels[i]);
            }
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            // 初始化会建立 Player 自身默认目标，以及首份默认快照。
            // 后续 Track 的 Initial/InitialPlusOffset 都依赖这份快照。
            targetCache.Initialize(transform);
            targetCache.RefreshSnapshots(clips);
            initialized = true;
        }

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

        public bool IsClipInfiniteLoop(string clipId)
        {
            return TryGetClipWithFallback(clipId, out UIMotionClipDefinition clip) && clip.IsInfiniteLoop;
        }

        private bool TryGetClipWithFallback(string clipId, out UIMotionClipDefinition clip)
        {
            if (TryGetClip(clipId, out clip))
            {
                return true;
            }

            // 常用状态 Clip 允许降级到对应动作 Clip，减少每个 Definition 必须重复配置的样板。
            // 例如没有 Visible 时可采样 Show 的末帧，没有 Hidden 时可采样 Hide 的末帧。
            string fallbackClipId = clipId switch
            {
                UIMotionClipIds.VISIBLE => UIMotionClipIds.SHOW,
                UIMotionClipIds.HIDDEN => UIMotionClipIds.HIDE,
                UIMotionClipIds.HOVER_OUT => UIMotionClipIds.RELEASE,
                _ => string.Empty
            };

            return !string.IsNullOrWhiteSpace(fallbackClipId) && TryGetClip(fallbackClipId, out clip);
        }

        private void PlayAutoClipsOnEnable()
        {
            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                UIMotionClipDefinition clip = clips[i];
                if (clip == null || !clip.AutoPlayOnEnable)
                {
                    continue;
                }

                Play(clip.ClipId);
            }
        }

        private void StopClipsOnDisable()
        {
            activeTweenBuffer.Clear();
            clipBuffer.Clear();

            foreach (KeyValuePair<string, List<ActiveTween>> pair in activeTweensByChannel)
            {
                List<ActiveTween> entries = pair.Value;
                if (entries == null)
                {
                    continue;
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    ActiveTween entry = entries[i];
                    if (entry?.Clip == null || !entry.Clip.StopOnDisable)
                    {
                        continue;
                    }

                    activeTweenBuffer.Add(entry);
                    if (entry.Clip.RestoreOnDisable && !clipBuffer.Contains(entry.Clip))
                    {
                        clipBuffer.Add(entry.Clip);
                    }
                }
            }

            for (int i = 0; i < activeTweenBuffer.Count; i++)
            {
                activeTweenBuffer[i].Tween?.Kill();
            }

            for (int i = 0; i < clipBuffer.Count; i++)
            {
                SampleClipStart(clipBuffer[i]);
            }

            activeTweenBuffer.Clear();
            clipBuffer.Clear();
        }

        private void SampleClipStart(UIMotionClipDefinition clip)
        {
            if (clip == null)
            {
                return;
            }

            UIMotionPlaybackContext context = new(this, clip, UIMotionPlaybackMode.SampleStart, 0f,
                clip.DurationScale);
            BuildClipTween(clip, context);
        }

        private Tween BuildClipTween(UIMotionClipDefinition clip, UIMotionPlaybackContext context)
        {
            IReadOnlyList<UIMotionTrackDefinition> tracks = clip.Tracks;
            if (tracks == null || tracks.Count == 0)
            {
                return null;
            }

            if (context.IsImmediate)
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    tracks[i]?.CreateTween(targetCache, context);
                }

                return null;
            }

            Sequence sequence = DOTween.Sequence();
            bool hasTween = false;
            for (int i = 0; i < tracks.Count; i++)
            {
                UIMotionTrackDefinition track = tracks[i];
                if (track == null)
                {
                    continue;
                }

                Tween trackTween = track.CreateTween(targetCache, context);
                if (trackTween == null)
                {
                    continue;
                }

                hasTween = true;
                // Clip 负责 Track 间的并行/顺序关系；Track 自己只关心单个目标属性的变化。
                if (clip.PlayMode == UIMotionClipPlayMode.Sequential)
                {
                    sequence.Append(trackTween);
                }
                else
                {
                    sequence.Join(trackTween);
                }
            }

            if (!hasTween)
            {
                sequence.Kill();
                return null;
            }

            return sequence;
        }

        private static Tween ApplyLoopPolicy(UIMotionClipDefinition clip, Tween tween)
        {
            int loopCount = clip.LoopCount;
            if (loopCount == 1)
            {
                return tween;
            }

            return tween.SetLoops(loopCount, clip.LoopType);
        }

        private static Tween WrapInitialDelay(Tween tween, float delay)
        {
            if (delay <= 0f)
            {
                return tween;
            }

            Sequence sequence = DOTween.Sequence();
            sequence.AppendInterval(delay);
            sequence.Append(tween);
            return sequence;
        }

        private void ApplyConflictPolicy(UIMotionClipDefinition clip)
        {
            // 冲突策略放在 Clip 上，便于同一个 Player 内同时支持“页面级动画互斥”和“交互反馈并行”。
            switch (clip.ConflictPolicy)
            {
                case UIMotionConflictPolicy.StopAllChannels:
                    Kill();
                    break;
                case UIMotionConflictPolicy.AllowParallel:
                    break;
                default:
                    StopChannel(clip.Channel);
                    break;
            }
        }

        private void RegisterChannelTween(string channel, UIMotionClipDefinition clip, Tween tween)
        {
            string resolvedChannel = ResolveChannel(channel);
            if (!activeTweensByChannel.TryGetValue(resolvedChannel, out List<ActiveTween> tweens))
            {
                tweens = new List<ActiveTween>();
                activeTweensByChannel.Add(resolvedChannel, tweens);
            }

            ActiveTween entry = new()
            {
                Channel = resolvedChannel,
                Clip = clip,
                Tween = tween
            };
            tweens.Add(entry);
            // DOTween 完成和 Kill 都可能结束播放；两边都注销可覆盖自然完成、手动中断和对象销毁。
            tween.OnKill(() => RemoveChannelTween(entry));
            tween.OnComplete(() => RemoveChannelTween(entry));
        }

        private void RemoveChannelTween(ActiveTween entry)
        {
            if (entry == null || !activeTweensByChannel.TryGetValue(entry.Channel, out List<ActiveTween> tweens))
            {
                return;
            }

            tweens.Remove(entry);
            if (tweens.Count == 0)
            {
                activeTweensByChannel.Remove(entry.Channel);
            }
        }

        private static string ResolveChannel(string channel)
        {
            return string.IsNullOrWhiteSpace(channel) ? UIMotionChannelIds.VISIBILITY : channel;
        }

    }
}
