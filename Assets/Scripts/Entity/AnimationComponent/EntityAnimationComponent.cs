using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EntityAnimationComponent : EntityComponentBase, IAnimatable, IEntityFacingController
{
    private const float DEFAULT_SCALE_X = 1f;
    private const float DEFAULT_PLAYBACK_SPEED = 1f;

    private Animator anim;

    private Entity owner;
    private HealthComponent healthComponent;
    private IAnimationConfigProvider animationConfigProvider;
    private EntityAnimationConfig animationConfig;
    private float baseScaleX = DEFAULT_SCALE_X;

    public override Entity Owner => owner;

    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        anim = GetComponent<Animator>();
        healthComponent = GetComponent<HealthComponent>();
        animationConfigProvider = this.owner.GetComponent<IAnimationConfigProvider>();

        animationConfig = animationConfigProvider.AnimationConfig;
        anim.runtimeAnimatorController = animationConfig.AnimatorController;
        CacheBaseScaleX();
        FaceDefault();
    }

    public override void OnEnableComponent()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDeathSequenceRequested += PlayDeathSequence;
        }
    }

    public override void OnDisableComponent()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDeathSequenceRequested -= PlayDeathSequence;
        }
    }

    #region 底层哈希ID操作

    public void SetBool(int id, bool value)
    {
        anim.SetBool(id, value);
    }

    public void SetTrigger(int id)
    {
        anim.SetTrigger(id);
    }

    public void SetFloat(int id, float value)
    {
        anim.SetFloat(id, value);
    }

    public void SetInteger(int id, int value)
    {
        anim.SetInteger(id, value);
    }

    #endregion

    #region 字符串操作

    public void SetBool(string paramName, bool value) => SetBool(Animator.StringToHash(paramName), value);
    public void SetTrigger(string paramName) => SetTrigger(Animator.StringToHash(paramName));
    public void SetFloat(string paramName, float value) => SetFloat(Animator.StringToHash(paramName), value);
    public void SetInteger(string paramName, int value) => SetInteger(Animator.StringToHash(paramName), value);

    #endregion

    public void PlayState(string stateName)
    {
        if (anim == null || string.IsNullOrEmpty(stateName)) return;
        anim.Play(stateName);
    }

    public void PlayState(int stateHash)
    {
        PlayState(stateHash, 0f);
    }

    public void PlayState(int stateHash, float normalizedTime, int layerIndex = 0)
    {
        if (anim == null) return;
        anim.Play(stateHash, layerIndex, normalizedTime);
    }

    public void SetPlaybackSpeed(float speed)
    {
        if (anim == null)
        {
            return;
        }

        anim.speed = Mathf.Max(0f, speed);
    }

    public void ResetPlaybackSpeed()
    {
        SetPlaybackSpeed(DEFAULT_PLAYBACK_SPEED);
    }

    public bool IsCurrentState(int stateHash, int layerIndex = 0)
    {
        if (anim == null)
        {
            return false;
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(layerIndex);
        return stateInfo.shortNameHash == stateHash;
    }

    public AnimationStateProgress GetStateProgress(int stateHash, int layerIndex = 0)
    {
        if (anim == null)
        {
            return new AnimationStateProgress(false, 0f);
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(layerIndex);
        bool isPlaying = stateInfo.shortNameHash == stateHash;
        return new AnimationStateProgress(isPlaying, isPlaying ? stateInfo.normalizedTime : 0f);
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

    public float GetCurrentStateNormalizedTime(int layerIndex = 0)
    {
        if (anim == null)
        {
            return 0f;
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(layerIndex);
        //开启了 Loop，normalizedTime 会 一直累加，不会停在 1
        return stateInfo.normalizedTime;
    }

    private IEnumerator PlayDeathSequence()
    {
        var animationConfig = animationConfigProvider?.AnimationConfig;
        if (animationConfig == null)
        {
            yield break;
        }

        PlayState(animationConfig.DeathHash);
        yield return null;

        while (IsCurrentState(animationConfig.DeathHash) && GetCurrentStateNormalizedTime() < 1f)
        {
            yield return null;
        }
    }
}
