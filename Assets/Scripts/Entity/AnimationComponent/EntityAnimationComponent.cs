using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EntityAnimationComponent : EntityComponentBase, IAnimatable
{
    private Animator anim;

    private Entity owner;

    public override Entity Owner => owner;
    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        anim = GetComponent<Animator>();
        anim.runtimeAnimatorController =
            this.owner.GetComponent<IAnimationConfigProvider>().AnimationConfig.AnimatorController;
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
}