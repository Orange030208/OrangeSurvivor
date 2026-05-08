using UnityEngine;

public abstract class Collection : Entity, IAnimationConfigProvider
{
    [SerializeField] protected float contactRadius = 0.8f;
    protected CollectionSO collectionData;
    protected bool isCollecting;

    public EntityAnimationConfig AnimationConfig => collectionData != null ? collectionData.AnimationConfig : null;

    public void Configure(CollectionSO data)
    {
        collectionData = data;
    }

    private void Start()
    {
        InitializeComponent();
        EnableAllComponents();
    }

    public virtual void TryCollect(IEntity target)
    {
        if (target == null || isCollecting)
        {
            return;
        }

        isCollecting = true;
        Collect(target);
    }

    protected void Collect(IEntity entity)
    {
        isCollecting = false;
        OnCollected(entity);
        PlayCollectSfx();
        Destroy(gameObject);
    }

    private void PlayCollectSfx()
    {
        if (collectionData == null || collectionData.CollectSfxKey == AudioSfxKey.None)
        {
            return;
        }

        AudioSfxBridge.RequestPlay(collectionData.CollectSfxKey);
    }

    /// <summary>
    /// 写收集的逻辑
    /// </summary>
    /// <param name="entity">收集自己的实体</param>
    protected abstract void OnCollected(IEntity entity);
}
