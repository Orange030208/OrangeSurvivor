using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EntityAnimationComponent : EntityComponentBase, IAnimatable
{
    private Animator anim;

    private Entity owner;
    private HealthComponent healthComponent;
    private IAnimationConfigProvider animationConfigProvider;

    public override Entity Owner => owner;

    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        anim = GetComponent<Animator>();
        healthComponent = GetComponent<HealthComponent>();
        animationConfigProvider = this.owner.GetComponent<IAnimationConfigProvider>();
        anim.runtimeAnimatorController = animationConfigProvider.AnimationConfig.AnimatorController;
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
        if (anim == null) return;
        anim.Play(stateHash);
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
