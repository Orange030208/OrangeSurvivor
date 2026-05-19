using System.Collections;
using UnityEngine;

public sealed class ProceduralEntityAnimationComponent : EntityComponentBase, IAnimatable, IEntityFacingController
{
    private static readonly int SquashId = Shader.PropertyToID("_Squash");
    private static readonly int StretchId = Shader.PropertyToID("_Stretch");
    private static readonly int VerticalOffsetId = Shader.PropertyToID("_VerticalOffset");
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int HueShiftId = Shader.PropertyToID("_HueShift");
    private static readonly int GlowAmountId = Shader.PropertyToID("_GlowAmount");
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    private const float DEFAULT_SCALE_X = 1f;
    private const float DEFAULT_PLAYBACK_SPEED = 1f;
    private const float DEFAULT_DURATION = 0.8f;
    private static readonly int SpawnStateHash = Animator.StringToHash("Spawn");

    [Header("程序动画")]
    [SerializeField] private ProceduralAnimationProfileSO profile;
    [SerializeField] private Color flashColor = new Color(1f, 0.92f, 0.72f, 1f);
    [SerializeField] private Color glowColor = new Color(0f, 0.55f, 0.8f, 1f);
    [SerializeField] private bool recordAnimatorParameterDebug;

    private Entity owner;
    private HealthComponent healthComponent;
    private IAnimationConfigProvider animationConfigProvider;
    private EntityAnimationConfig animationConfig;
    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;
    private ProceduralAnimationProfileSO.StateDefinition currentState;
    private int currentStateHash;
    private float currentStateElapsedTime;
    private float playbackSpeed = DEFAULT_PLAYBACK_SPEED;
    private float hurtOverlayElapsedTime;
    private bool hurtOverlayActive;
    private float baseScaleX = DEFAULT_SCALE_X;
    private System.Action spawnCompletedCallback;
    private bool isSpawnSequenceRunning;

    public override Entity Owner => owner;
    public override int Priority => PriorityPreset.NoRely - 10;
    public bool HasSpawnState => profile != null && profile.TryGetState(SpawnStateHash, out _);

    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        healthComponent = owner.GetComponent<HealthComponent>();
        animationConfigProvider = owner.GetComponent<IAnimationConfigProvider>();
        animationConfig = animationConfigProvider?.AnimationConfig;
        spriteRenderer = owner.EntityRenderer != null ? owner.EntityRenderer.SpriteRenderer : GetComponentInChildren<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();

        CacheBaseScaleX();
        FaceDefault();

        if (animationConfig != null)
        {
            PlayState(animationConfig.IdleHash, 0f);
        }

        ApplyShaderProperties(0f);
    }

    public override void OnEnableComponent()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDeathSequenceRequested += PlayDeathSequence;
            healthComponent.OnDamaged += OnDamaged;
            healthComponent.OnDeathSequenceStarted += OnDeathSequenceStarted;
        }
    }

    public override void OnDisableComponent()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDeathSequenceRequested -= PlayDeathSequence;
            healthComponent.OnDamaged -= OnDamaged;
            healthComponent.OnDeathSequenceStarted -= OnDeathSequenceStarted;
        }

        isSpawnSequenceRunning = false;
        spawnCompletedCallback = null;
        ResetShaderProperties();
    }

    public override void OnTick(float deltaTime)
    {
        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        TickPrimaryState(safeDeltaTime);
        TickHurtOverlay(safeDeltaTime);
        TickSpawnSequence();
        ApplyShaderProperties(GetCurrentStateNormalizedTime());
    }

    public void SetBool(int id, bool value) => RecordParameterDebug(nameof(SetBool), id, value);
    public void SetTrigger(int id) => RecordParameterDebug(nameof(SetTrigger), id, true);
    public void SetFloat(int id, float value) => RecordParameterDebug(nameof(SetFloat), id, value);
    public void SetInteger(int id, int value) => RecordParameterDebug(nameof(SetInteger), id, value);

    public void SetBool(string paramName, bool value) => SetBool(Animator.StringToHash(paramName), value);
    public void SetTrigger(string paramName) => SetTrigger(Animator.StringToHash(paramName));
    public void SetFloat(string paramName, float value) => SetFloat(Animator.StringToHash(paramName), value);
    public void SetInteger(string paramName, int value) => SetInteger(Animator.StringToHash(paramName), value);

    public void PlayState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        PlayState(Animator.StringToHash(stateName), 0f);
    }

    public void PlayState(int stateHash)
    {
        PlayState(stateHash, 0f);
    }

    public void PlayState(int stateHash, float normalizedTime, int layerIndex = 0)
    {
        if (layerIndex != 0 || stateHash == 0)
        {
            return;
        }

        currentStateHash = stateHash;
        currentState = ResolveState(stateHash);
        currentStateElapsedTime = Mathf.Clamp01(normalizedTime) * ResolveDuration(currentState);
        ApplyShaderProperties(GetCurrentStateNormalizedTime());
    }

    public void PlaySpawnSequence(System.Action onCompleted = null)
    {
        if (!HasSpawnState)
        {
            onCompleted?.Invoke();
            return;
        }

        spawnCompletedCallback = onCompleted;
        isSpawnSequenceRunning = true;
        PlayState(SpawnStateHash, 0f);
    }

    public void SetPlaybackSpeed(float speed)
    {
        playbackSpeed = Mathf.Max(0f, speed);
    }

    public void ResetPlaybackSpeed()
    {
        playbackSpeed = DEFAULT_PLAYBACK_SPEED;
    }

    public bool IsCurrentState(int stateHash, int layerIndex = 0)
    {
        return layerIndex == 0 && currentStateHash == stateHash;
    }

    public float GetCurrentStateNormalizedTime(int layerIndex = 0)
    {
        if (layerIndex != 0 || currentStateHash == 0)
        {
            return 0f;
        }

        float duration = ResolveDuration(currentState);
        if (duration <= 0f)
        {
            return 1f;
        }

        float normalizedTime = currentStateElapsedTime / duration;
        return currentState != null && currentState.Loop
            ? normalizedTime
            : Mathf.Clamp01(normalizedTime);
    }

    public AnimationStateProgress GetStateProgress(int stateHash, int layerIndex = 0)
    {
        bool isPlaying = IsCurrentState(stateHash, layerIndex);
        return new AnimationStateProgress(isPlaying, isPlaying ? GetCurrentStateNormalizedTime(layerIndex) : 0f);
    }

    public void FaceDefault()
    {
        if (animationConfig == null)
        {
            return;
        }

        ApplyHorizontalFacing(animationConfig.DefaultFacingDirection);
    }

    public void FaceTarget(Entity target)
    {
        if (target == null || owner == null)
        {
            return;
        }

        FaceDirection(target.Center - owner.Center);
    }

    public void FaceDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) <= Mathf.Epsilon)
        {
            return;
        }

        EntityAnimationConfig.FacingDirection desiredDirection = direction.x < 0f
            ? EntityAnimationConfig.FacingDirection.Left
            : EntityAnimationConfig.FacingDirection.Right;
        ApplyHorizontalFacing(desiredDirection);
    }

    public void FaceMoveDirection(IMovable movable)
    {
        if (movable == null || !movable.IsMoving)
        {
            return;
        }

        FaceDirection(movable.MoveDirection);
    }

    private void TickPrimaryState(float deltaTime)
    {
        if (currentStateHash == 0)
        {
            return;
        }

        float speed = playbackSpeed * (currentState != null ? currentState.PlaybackSpeedMultiplier : DEFAULT_PLAYBACK_SPEED);
        currentStateElapsedTime += deltaTime * speed;

        if (currentState != null && currentState.Loop)
        {
            float duration = ResolveDuration(currentState);
            if (duration > 0f && currentStateElapsedTime >= duration)
            {
                currentStateElapsedTime %= duration;
            }
        }
    }

    private void TickHurtOverlay(float deltaTime)
    {
        if (!hurtOverlayActive || profile == null)
        {
            return;
        }

        hurtOverlayElapsedTime += deltaTime;
        if (hurtOverlayElapsedTime >= profile.HurtOverlayDuration)
        {
            hurtOverlayElapsedTime = 0f;
            hurtOverlayActive = false;
        }
    }

    private void TickSpawnSequence()
    {
        if (!isSpawnSequenceRunning)
        {
            return;
        }

        if (!IsCurrentState(SpawnStateHash) || GetCurrentStateNormalizedTime() >= 1f)
        {
            isSpawnSequenceRunning = false;
            System.Action callback = spawnCompletedCallback;
            spawnCompletedCallback = null;
            callback?.Invoke();
        }
    }

    private ProceduralAnimationProfileSO.StateDefinition ResolveState(int stateHash)
    {
        return profile != null && profile.TryGetState(stateHash, out ProceduralAnimationProfileSO.StateDefinition state)
            ? state
            : null;
    }

    private float ResolveDuration(ProceduralAnimationProfileSO.StateDefinition state)
    {
        return state != null ? state.Duration : DEFAULT_DURATION;
    }

    private void ApplyShaderProperties(float normalizedTime)
    {
        if (spriteRenderer == null || propertyBlock == null)
        {
            return;
        }

        float sampleTime = currentState != null && currentState.Loop
            ? Mathf.Repeat(normalizedTime, 1f)
            : Mathf.Clamp01(normalizedTime);

        float squash = currentState != null ? currentState.EvaluateSquash(sampleTime) : 0f;
        float stretch = currentState != null ? currentState.EvaluateStretch(sampleTime) : 0f;
        float verticalOffset = currentState != null ? currentState.EvaluateVerticalOffset(sampleTime) : 0f;
        float flash = currentState != null ? currentState.EvaluateFlash(sampleTime) : 0f;
        float dissolve = currentState != null ? currentState.EvaluateDissolve(sampleTime) : 0f;
        float hueShift = currentState != null ? currentState.HueShift : 0f;
        float glowAmount = currentState != null ? currentState.GlowAmount : 0f;

        if (hurtOverlayActive && profile != null)
        {
            float hurtTime = Mathf.Clamp01(hurtOverlayElapsedTime / profile.HurtOverlayDuration);
            squash += profile.EvaluateHurtSquash(hurtTime);
            stretch += profile.EvaluateHurtStretch(hurtTime);
            flash = Mathf.Max(flash, profile.EvaluateHurtFlash(hurtTime));
        }

        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(SquashId, squash);
        propertyBlock.SetFloat(StretchId, stretch);
        propertyBlock.SetFloat(VerticalOffsetId, verticalOffset);
        propertyBlock.SetFloat(FlashAmountId, Mathf.Clamp01(flash));
        propertyBlock.SetFloat(DissolveAmountId, Mathf.Clamp01(dissolve));
        propertyBlock.SetFloat(HueShiftId, hueShift);
        propertyBlock.SetFloat(GlowAmountId, glowAmount);
        propertyBlock.SetColor(GlowColorId, glowColor);
        propertyBlock.SetColor(FlashColorId, flashColor);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    private void ResetShaderProperties()
    {
        if (spriteRenderer == null || propertyBlock == null)
        {
            return;
        }

        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(SquashId, 0f);
        propertyBlock.SetFloat(StretchId, 0f);
        propertyBlock.SetFloat(VerticalOffsetId, 0f);
        propertyBlock.SetFloat(FlashAmountId, 0f);
        propertyBlock.SetFloat(DissolveAmountId, 0f);
        propertyBlock.SetFloat(HueShiftId, 0f);
        propertyBlock.SetFloat(GlowAmountId, 0f);
        propertyBlock.SetColor(GlowColorId, glowColor);
        propertyBlock.SetColor(FlashColorId, flashColor);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDamaged(HitResult result)
    {
        if (result.Target != owner ||
            result.IsCancelled ||
            result.IsDodged ||
            result.IsBlocked ||
            result.FinalDamage <= 0f ||
            profile == null)
        {
            return;
        }

        hurtOverlayElapsedTime = 0f;
        hurtOverlayActive = true;
    }

    private void OnDeathSequenceStarted()
    {
        isSpawnSequenceRunning = false;
        spawnCompletedCallback = null;
        hurtOverlayElapsedTime = 0f;
        hurtOverlayActive = false;
    }

    private IEnumerator PlayDeathSequence()
    {
        if (animationConfig == null)
        {
            yield break;
        }

        PlayState(animationConfig.DeathHash, 0f);
        yield return null;

        while (IsCurrentState(animationConfig.DeathHash) && GetCurrentStateNormalizedTime() < 1f)
        {
            float deltaTime = Mathf.Max(0f, Time.deltaTime);
            TickPrimaryState(deltaTime);
            TickHurtOverlay(deltaTime);
            ApplyShaderProperties(GetCurrentStateNormalizedTime());
            yield return null;
        }

        ApplyShaderProperties(1f);
    }

    private void CacheBaseScaleX()
    {
        if (owner == null)
        {
            baseScaleX = DEFAULT_SCALE_X;
            return;
        }

        baseScaleX = Mathf.Abs(owner.transform.localScale.x);
        if (baseScaleX <= Mathf.Epsilon)
        {
            baseScaleX = DEFAULT_SCALE_X;
        }
    }

    private void ApplyHorizontalFacing(EntityAnimationConfig.FacingDirection desiredDirection)
    {
        if (owner == null || animationConfig == null)
        {
            return;
        }

        bool shouldFlip = desiredDirection != animationConfig.DefaultFacingDirection;
        Transform ownerTransform = owner.transform;
        Vector3 scale = ownerTransform.localScale;
        scale.x = shouldFlip ? -baseScaleX : baseScaleX;
        ownerTransform.localScale = scale;
    }

    private void RecordParameterDebug(string operation, int id, object value)
    {
        if (!recordAnimatorParameterDebug)
        {
            return;
        }

        Debug.Log($"[{nameof(ProceduralEntityAnimationComponent)}] Ignore Animator parameter {operation}({id}, {value}) on {name}.", this);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyShaderProperties(GetCurrentStateNormalizedTime());
        }
    }
}
