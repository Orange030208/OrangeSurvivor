using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ProceduralAnimationProfile",
    menuName = ScriptableObjectMenuPaths.PROCEDURAL_ANIMATION_PROFILE,
    order = 0)]
public sealed class ProceduralAnimationProfileSO : ScriptableObject
{
    [Serializable]
    public sealed class StateDefinition
    {
        [SerializeField] private string stateName = "Idle";
        [SerializeField, Min(0.01f)] private float duration = 0.8f;
        [SerializeField] private bool loop = true;
        [SerializeField, Min(0f)] private float playbackSpeedMultiplier = 1f;
        [SerializeField] private AnimationCurve squashCurve = AnimationCurve.Constant(0f, 1f, 0f);
        [SerializeField] private AnimationCurve stretchCurve = AnimationCurve.Constant(0f, 1f, 0f);
        [SerializeField] private AnimationCurve verticalOffsetCurve = AnimationCurve.Constant(0f, 1f, 0f);
        [SerializeField] private AnimationCurve flashCurve = AnimationCurve.Constant(0f, 1f, 0f);
        [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.Constant(0f, 1f, 0f);
        [SerializeField, Range(-1f, 1f)] private float hueShift;
        [SerializeField, Min(0f)] private float glowAmount;

        [NonSerialized] private int stateHash;
        [NonSerialized] private string cachedStateName;

        public StateDefinition()
        {
        }

        public StateDefinition(
            string stateName,
            float duration,
            bool loop,
            AnimationCurve squashCurve,
            AnimationCurve stretchCurve,
            AnimationCurve verticalOffsetCurve,
            AnimationCurve flashCurve,
            AnimationCurve dissolveCurve,
            float playbackSpeedMultiplier = 1f,
            float hueShift = 0f,
            float glowAmount = 0f)
        {
            this.stateName = stateName;
            this.duration = duration;
            this.loop = loop;
            this.playbackSpeedMultiplier = playbackSpeedMultiplier;
            this.squashCurve = squashCurve;
            this.stretchCurve = stretchCurve;
            this.verticalOffsetCurve = verticalOffsetCurve;
            this.flashCurve = flashCurve;
            this.dissolveCurve = dissolveCurve;
            this.hueShift = hueShift;
            this.glowAmount = glowAmount;
            Validate();
        }

        public string StateName => stateName;
        public float Duration => Mathf.Max(0.01f, duration);
        public bool Loop => loop;
        public float PlaybackSpeedMultiplier => Mathf.Max(0f, playbackSpeedMultiplier);
        public float HueShift => hueShift;
        public float GlowAmount => Mathf.Max(0f, glowAmount);
        public int StateHash
        {
            get
            {
                if (!string.Equals(cachedStateName, stateName, StringComparison.Ordinal))
                {
                    cachedStateName = stateName;
                    stateHash = string.IsNullOrWhiteSpace(stateName)
                        ? 0
                        : Animator.StringToHash(stateName);
                }

                return stateHash;
            }
        }

        public float EvaluateSquash(float normalizedTime) => Evaluate(squashCurve, normalizedTime);
        public float EvaluateStretch(float normalizedTime) => Evaluate(stretchCurve, normalizedTime);
        public float EvaluateVerticalOffset(float normalizedTime) => Evaluate(verticalOffsetCurve, normalizedTime);
        public float EvaluateFlash(float normalizedTime) => Evaluate(flashCurve, normalizedTime);
        public float EvaluateDissolve(float normalizedTime) => Evaluate(dissolveCurve, normalizedTime);

        public void Validate()
        {
            duration = Mathf.Max(0.01f, duration);
            playbackSpeedMultiplier = Mathf.Max(0f, playbackSpeedMultiplier);
            hueShift = Mathf.Clamp(hueShift, -1f, 1f);
            glowAmount = Mathf.Max(0f, glowAmount);
            cachedStateName = null;
        }

        private static float Evaluate(AnimationCurve curve, float normalizedTime)
        {
            return curve != null && curve.length > 0
                ? curve.Evaluate(Mathf.Clamp01(normalizedTime))
                : 0f;
        }
    }

    [SerializeField] private List<StateDefinition> states = new();
    [SerializeField, Min(0.01f)] private float hurtOverlayDuration = 0.12f;
    [SerializeField] private AnimationCurve hurtSquashCurve = new(
        new Keyframe(0f, 0.12f),
        new Keyframe(1f, 0f));
    [SerializeField] private AnimationCurve hurtStretchCurve = new(
        new Keyframe(0f, -0.05f),
        new Keyframe(1f, 0f));
    [SerializeField] private AnimationCurve hurtFlashCurve = new(
        new Keyframe(0f, 0.35f),
        new Keyframe(1f, 0f));

    private readonly Dictionary<int, StateDefinition> stateLookup = new();

    public float HurtOverlayDuration => Mathf.Max(0.01f, hurtOverlayDuration);
    public IReadOnlyList<StateDefinition> States => states;

    private void OnEnable()
    {
        EnsureDefaultStates();
        RebuildLookup();
    }

    private void OnValidate()
    {
        EnsureDefaultStates();
        hurtOverlayDuration = Mathf.Max(0.01f, hurtOverlayDuration);
        for (int i = 0; i < states.Count; i++)
        {
            states[i]?.Validate();
        }

        RebuildLookup();
    }

    public bool TryGetState(int stateHash, out StateDefinition state)
    {
        RebuildLookupIfNeeded();
        return stateLookup.TryGetValue(stateHash, out state);
    }

    public bool TryGetState(string stateName, out StateDefinition state)
    {
        int stateHash = string.IsNullOrWhiteSpace(stateName)
            ? 0
            : Animator.StringToHash(stateName);
        return TryGetState(stateHash, out state);
    }

    public float EvaluateHurtSquash(float normalizedTime) => EvaluateOverlay(hurtSquashCurve, normalizedTime);
    public float EvaluateHurtStretch(float normalizedTime) => EvaluateOverlay(hurtStretchCurve, normalizedTime);
    public float EvaluateHurtFlash(float normalizedTime) => EvaluateOverlay(hurtFlashCurve, normalizedTime);

    private void RebuildLookupIfNeeded()
    {
        if (stateLookup.Count == 0 && states.Count > 0)
        {
            RebuildLookup();
        }
    }

    private void RebuildLookup()
    {
        stateLookup.Clear();
        for (int i = 0; i < states.Count; i++)
        {
            StateDefinition state = states[i];
            if (state == null || state.StateHash == 0)
            {
                continue;
            }

            stateLookup[state.StateHash] = state;
        }
    }

    private void EnsureDefaultStates()
    {
        if (states.Count > 0)
        {
            return;
        }

        states.Add(new StateDefinition(
            "Spawn",
            0.55f,
            false,
            new AnimationCurve(new Keyframe(0f, -0.22f), new Keyframe(0.2f, 0.06f), new Keyframe(0.7f, 0.03f), new Keyframe(1f, 0f)),
            new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(0.25f, -0.06f), new Keyframe(0.7f, -0.02f), new Keyframe(1f, 0f)),
            new AnimationCurve(new Keyframe(0f, -0.08f), new Keyframe(0.32f, 0.02f), new Keyframe(1f, 0f)),
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.35f, 0.28f), new Keyframe(1f, 0f)),
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.22f, 0.35f), new Keyframe(1f, 0f)),
            playbackSpeedMultiplier: 1f,
            glowAmount: 0.14f));

        states.Add(new StateDefinition(
            "Idle",
            0.9f,
            true,
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 0.025f), new Keyframe(1f, 0f)),
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, -0.012f), new Keyframe(1f, 0f)),
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 0.01f), new Keyframe(1f, 0f)),
            AnimationCurve.Constant(0f, 1f, 0f),
            AnimationCurve.Constant(0f, 1f, 0f),
            glowAmount: 0.03f));

        states.Add(new StateDefinition(
            "Move",
            0.45f,
            true,
            new AnimationCurve(new Keyframe(0f, 0.06f), new Keyframe(0.5f, -0.03f), new Keyframe(1f, 0.06f)),
            new AnimationCurve(new Keyframe(0f, -0.025f), new Keyframe(0.5f, 0.035f), new Keyframe(1f, -0.025f)),
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 0.025f), new Keyframe(1f, 0f)),
            AnimationCurve.Constant(0f, 1f, 0f),
            AnimationCurve.Constant(0f, 1f, 0f),
            glowAmount: 0.05f));

        states.Add(new StateDefinition(
            "Attack",
            0.55f,
            false,
            new AnimationCurve(new Keyframe(0f, -0.06f), new Keyframe(0.36f, 0.16f), new Keyframe(0.65f, -0.07f), new Keyframe(1f, 0f)),
            new AnimationCurve(new Keyframe(0f, 0.07f), new Keyframe(0.36f, -0.06f), new Keyframe(0.65f, 0.08f), new Keyframe(1f, 0f)),
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.36f, -0.015f), new Keyframe(0.65f, 0.04f), new Keyframe(1f, 0f)),
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.55f, 0.06f), new Keyframe(1f, 0f)),
            AnimationCurve.Constant(0f, 1f, 0f),
            glowAmount: 0.08f));

        states.Add(new StateDefinition(
            "Hurt",
            0.12f,
            false,
            new AnimationCurve(new Keyframe(0f, 0.12f), new Keyframe(1f, 0f)),
            new AnimationCurve(new Keyframe(0f, -0.05f), new Keyframe(1f, 0f)),
            AnimationCurve.Constant(0f, 1f, 0f),
            new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(1f, 0f)),
            AnimationCurve.Constant(0f, 1f, 0f),
            glowAmount: 0.06f));

        states.Add(new StateDefinition(
            "Death",
            0.75f,
            false,
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 0.08f), new Keyframe(1f, 0.16f)),
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, -0.08f)),
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, -0.03f)),
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.2f, 0.25f), new Keyframe(1f, 0f)),
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f)),
            glowAmount: 0.1f));
    }

    private static float EvaluateOverlay(AnimationCurve curve, float normalizedTime)
    {
        return curve != null && curve.length > 0
            ? curve.Evaluate(Mathf.Clamp01(normalizedTime))
            : 0f;
    }
}
