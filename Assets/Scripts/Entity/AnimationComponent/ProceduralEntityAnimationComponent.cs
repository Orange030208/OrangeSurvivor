using System.Collections;
using UnityEngine;

public sealed class ProceduralEntityAnimationComponent : EntityComponentBase, IAnimatable, IEntityFacingController
{
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int HueShiftId = Shader.PropertyToID("_HueShift");
    private static readonly int GlowAmountId = Shader.PropertyToID("_GlowAmount");
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    private const float DEFAULT_SCALE_X = 1f;
    private const float DEFAULT_PLAYBACK_SPEED = 1f;
    private const float DEFAULT_DURATION = 0.8f;
    private const float HORIZONTAL_SCALE_RESPONSE = 0.35f;
    private const float VERTICAL_SCALE_RESPONSE = 0.7f;
    private const float MIN_VISUAL_SCALE = 0.35f;
    private static readonly int SpawnStateHash = Animator.StringToHash("Spawn");
    private static readonly int ChargeStateHash = Animator.StringToHash("Charge");

    [Header("程序动画")]
    [SerializeField] private ProceduralAnimationProfileSO profile;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Color flashColor = new Color(1f, 0.92f, 0.72f, 1f);
    [SerializeField] private Color glowColor = new Color(0f, 0.55f, 0.8f, 1f);

    private Entity owner;
    private HealthComponent healthComponent;
    private IAnimationConfigProvider animationConfigProvider;
    private IProceduralAnimationProfileProvider proceduralAnimationProfileProvider;
    private ICharacterSpriteProvider characterSpriteProvider;
    private EntityAnimationConfig animationConfig;
    private SpriteRenderer spriteRenderer;
    private Transform visualTransform;
    private MaterialPropertyBlock propertyBlock;
    private ProceduralAnimationProfileSO.StateDefinition currentState;
    private int currentStateHash;
    private float currentStateElapsedTime;
    private float playbackSpeed = DEFAULT_PLAYBACK_SPEED;
    private float hurtOverlayElapsedTime;
    private bool hurtOverlayActive;
    private float baseScaleX = DEFAULT_SCALE_X;
    private Vector3 baseVisualLocalScale = Vector3.one;
    private Vector3 baseVisualLocalPosition;
    private float baseVisualFacingSign = 1f;
    private float currentVisualFacingSign = 1f;
    private System.Action spawnCompletedCallback;
    private bool isSpawnSequenceRunning;

    private struct AnimationSample
    {
        public float Squash;
        public float Stretch;
        public float VerticalOffset;
        public float Flash;
        public float Dissolve;
        public float HueShift;
        public float GlowAmount;
    }

    public override Entity Owner => owner;
    public override int Priority => PriorityPreset.NoRely - 10;
    public bool HasSpawnState => profile != null && profile.TryGetState(SpawnStateHash, out _);

    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        healthComponent = owner.GetComponent<HealthComponent>();
        animationConfigProvider = owner.GetComponent<IAnimationConfigProvider>();
        proceduralAnimationProfileProvider = owner.GetComponent<IProceduralAnimationProfileProvider>();
        characterSpriteProvider = owner.GetComponent<ICharacterSpriteProvider>();
        animationConfig = animationConfigProvider?.AnimationConfig;
        profile = profile != null ? profile : proceduralAnimationProfileProvider?.ProceduralAnimationProfile;
        spriteRenderer = owner.EntityRenderer != null ? owner.EntityRenderer.SpriteRenderer : GetComponentInChildren<SpriteRenderer>();
        visualTransform = visualRoot != null
            ? visualRoot
            : spriteRenderer != null ? spriteRenderer.transform : null;
        propertyBlock = new MaterialPropertyBlock();

        CacheBaseScaleX();
        CacheBaseVisualTransformState();
        ApplyCharacterSprite();
        FaceDefault();

        if (animationConfig != null)
        {
            PlayState(animationConfig.IdleHash, 0f);
        }

        ApplyAnimationProperties(0f);
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
        ResetVisualTransform();
        ResetShaderProperties();
    }

    public override void OnTick(float deltaTime)
    {
        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        TickPrimaryState(safeDeltaTime);
        TickHurtOverlay(safeDeltaTime);
        TickSpawnSequence();
        ApplyAnimationProperties(GetCurrentStateNormalizedTime());
    }

    public void SetBool(int id, bool value) { }
    public void SetTrigger(int id) { }
    public void SetFloat(int id, float value) { }
    public void SetInteger(int id, int value) { }

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
        ApplyAnimationProperties(GetCurrentStateNormalizedTime());
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

    private void ApplyAnimationProperties(float normalizedTime)
    {
        AnimationSample sample = SampleAnimation(normalizedTime);
        ApplyVisualTransform(sample);
        ApplyShaderProperties(sample);
    }

    private AnimationSample SampleAnimation(float normalizedTime)
    {
        float sampleTime = currentState != null && currentState.Loop
            ? Mathf.Repeat(normalizedTime, 1f)
            : Mathf.Clamp01(normalizedTime);

        AnimationSample sample = new()
        {
            Squash = currentState != null ? currentState.EvaluateSquash(sampleTime) : 0f,
            Stretch = currentState != null ? currentState.EvaluateStretch(sampleTime) : 0f,
            VerticalOffset = currentState != null ? currentState.EvaluateVerticalOffset(sampleTime) : 0f,
            Flash = currentState != null ? currentState.EvaluateFlash(sampleTime) : 0f,
            Dissolve = currentState != null ? currentState.EvaluateDissolve(sampleTime) : 0f,
            HueShift = currentState != null ? currentState.HueShift : 0f,
            GlowAmount = currentState != null ? currentState.GlowAmount : 0f
        };

        if (hurtOverlayActive && profile != null)
        {
            float hurtTime = Mathf.Clamp01(hurtOverlayElapsedTime / profile.HurtOverlayDuration);
            sample.Squash += profile.EvaluateHurtSquash(hurtTime);
            sample.Stretch += profile.EvaluateHurtStretch(hurtTime);
            sample.Flash = Mathf.Max(sample.Flash, profile.EvaluateHurtFlash(hurtTime));
        }

        return sample;
    }

    private void ApplyVisualTransform(AnimationSample sample)
    {
        if (visualTransform == null)
        {
            return;
        }

        Vector3 targetScale = baseVisualLocalScale;
        // Charge 只保留纵向拉伸，避免蓄力时出现横向“撑开”的感觉。
        bool isChargeState = currentStateHash == ChargeStateHash;
        ResolveVisualScale(sample.Squash, sample.Stretch, isChargeState, out float horizontalScale, out float verticalScale);
        bool visualTransformIsOwner = IsVisualTransformOwner();
        if (UsesExplicitVisualRoot())
        {
            targetScale.x = Mathf.Abs(baseVisualLocalScale.x) * horizontalScale * currentVisualFacingSign;
        }
        else if (visualTransformIsOwner)
        {
            float facingSign = visualTransform.localScale.x < 0f ? -1f : 1f;
            targetScale.x = Mathf.Abs(baseVisualLocalScale.x) * horizontalScale * facingSign;
        }
        else
        {
            targetScale.x = baseVisualLocalScale.x * horizontalScale;
        }

        targetScale.y = baseVisualLocalScale.y * verticalScale;
        visualTransform.localScale = targetScale;

        if (!visualTransformIsOwner)
        {
            Vector3 targetPosition = baseVisualLocalPosition;
            targetPosition.y += sample.VerticalOffset;
            visualTransform.localPosition = targetPosition;
        }
    }

    private void ApplyShaderProperties(AnimationSample sample)
    {
        if (spriteRenderer == null || propertyBlock == null)
        {
            return;
        }

        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(FlashAmountId, Mathf.Clamp01(sample.Flash));
        propertyBlock.SetFloat(DissolveAmountId, Mathf.Clamp01(sample.Dissolve));
        propertyBlock.SetFloat(HueShiftId, sample.HueShift);
        propertyBlock.SetFloat(GlowAmountId, sample.GlowAmount);
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
        propertyBlock.SetFloat(FlashAmountId, 0f);
        propertyBlock.SetFloat(DissolveAmountId, 0f);
        propertyBlock.SetFloat(HueShiftId, 0f);
        propertyBlock.SetFloat(GlowAmountId, 0f);
        propertyBlock.SetColor(GlowColorId, glowColor);
        propertyBlock.SetColor(FlashColorId, flashColor);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    private static void ResolveVisualScale(float squash, float stretch, bool verticalOnly, out float horizontalScale, out float verticalScale)
    {
        if (verticalOnly)
        {
            horizontalScale = 1f;
            verticalScale = Mathf.Max(MIN_VISUAL_SCALE, 1f + (squash - stretch) * VERTICAL_SCALE_RESPONSE);
            return;
        }

        float horizontalInfluence = (-squash + stretch) * HORIZONTAL_SCALE_RESPONSE;
        float verticalInfluence = (squash - stretch) * VERTICAL_SCALE_RESPONSE;
        horizontalScale = Mathf.Max(MIN_VISUAL_SCALE, 1f + horizontalInfluence);
        verticalScale = Mathf.Max(MIN_VISUAL_SCALE, 1f + verticalInfluence);
    }

    private void CacheBaseVisualTransformState()
    {
        if (visualTransform == null)
        {
            baseVisualLocalScale = Vector3.one;
            baseVisualLocalPosition = Vector3.zero;
            return;
        }

        baseVisualLocalScale = visualTransform.localScale;
        baseVisualLocalPosition = visualTransform.localPosition;
        baseVisualFacingSign = baseVisualLocalScale.x < 0f ? -1f : 1f;
        currentVisualFacingSign = baseVisualFacingSign;
    }

    private void ResetVisualTransform()
    {
        if (visualTransform == null)
        {
            return;
        }

        Vector3 targetScale = baseVisualLocalScale;
        if (UsesExplicitVisualRoot())
        {
            targetScale.x = Mathf.Abs(baseVisualLocalScale.x) * currentVisualFacingSign;
        }
        else if (IsVisualTransformOwner())
        {
            float facingSign = visualTransform.localScale.x < 0f ? -1f : 1f;
            targetScale.x = Mathf.Abs(baseVisualLocalScale.x) * facingSign;
        }

        visualTransform.localScale = targetScale;
        if (!IsVisualTransformOwner())
        {
            visualTransform.localPosition = baseVisualLocalPosition;
        }
    }

    private bool IsVisualTransformOwner()
    {
        return owner != null && visualTransform == owner.transform;
    }

    private bool UsesExplicitVisualRoot()
    {
        return visualRoot != null && visualTransform == visualRoot;
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
            ApplyAnimationProperties(GetCurrentStateNormalizedTime());
            yield return null;
        }

        ApplyAnimationProperties(1f);
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
        if (UsesExplicitVisualRoot())
        {
            currentVisualFacingSign = shouldFlip ? -baseVisualFacingSign : baseVisualFacingSign;
        }
        else
        {
            Transform ownerTransform = owner.transform;
            Vector3 scale = ownerTransform.localScale;
            scale.x = shouldFlip ? -baseScaleX : baseScaleX;
            ownerTransform.localScale = scale;
        }

        ApplyAnimationProperties(GetCurrentStateNormalizedTime());
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyAnimationProperties(GetCurrentStateNormalizedTime());
        }
    }

    private void ApplyCharacterSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Sprite characterSprite = characterSpriteProvider != null ? characterSpriteProvider.CharacterSprite : null;
        if (characterSprite != null)
        {
            spriteRenderer.sprite = characterSprite;
        }
    }
}
