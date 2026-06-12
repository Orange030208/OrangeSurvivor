using System.Collections;
using UnityEngine;

[RequireComponent(typeof(IAnimatable))]
public class Chest : Collection
{
    private const float DEFAULT_COLLECT_DELAY = 0.5f;
    private const float MIN_DETECT_INTERVAL = 0.01f;
    private const string PLAYER_LAYER_NAME = "Player";

    [SerializeField] private float collectDelayAfterOpen = DEFAULT_COLLECT_DELAY;
    [SerializeField] private float contactDetectInterval = 0.05f;

    private IAnimatable animatable;
    private EntityAnimationConfig animConfig;
    private readonly Collider2D[] playerBuffer = new Collider2D[4];
    private int playerLayerMask;
    private float detectTimer;

    private void Awake()
    {
        animatable = GetComponent<IAnimatable>();
        playerLayerMask = LayerMask.GetMask(PLAYER_LAYER_NAME);
    }

    private void Update()
    {
        if (isCollecting)
        {
            return;
        }

        detectTimer -= Time.deltaTime;
        if (detectTimer > 0f)
        {
            return;
        }

        DetectPlayerContact();
        detectTimer = Mathf.Max(MIN_DETECT_INTERVAL, contactDetectInterval);
    }

    public override void TryCollect(IEntity target)
    {
        if (target == null || isCollecting)
        {
            return;
        }

        if (target.Distance(this) > contactRadius)
        {
            return;
        }

        isCollecting = true;
        StartCoroutine(CollectAfterOpenSequence(target));
    }

    private void DetectPlayerContact()
    {
        if (playerLayerMask == 0)
        {
            return;
        }

        int hitCount = Physics2D.OverlapCircleNonAlloc(Center, contactRadius, playerBuffer, playerLayerMask);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D playerCollider = playerBuffer[i];
            if (playerCollider != null && playerCollider.TryGetComponent(out Player player))
            {
                TryCollect(player);
                return;
            }
        }
    }

    private IEnumerator CollectAfterOpenSequence(IEntity target)
    {
        EnsureAnimationReferences();
        PlayOpenAnimation();

        yield return WaitForOpenAnimation();
        yield return new WaitForSeconds(Mathf.Max(0f, collectDelayAfterOpen));

        Collect(target);
    }

    private void EnsureAnimationReferences()
    {
        if (animatable == null)
        {
            animatable = GetComponent<IAnimatable>();
        }

        animConfig = AnimationConfig;
    }

    private void PlayOpenAnimation()
    {
        if (animatable == null)
        {
            Debug.LogError($"[Chest] {nameof(IAnimatable)} is missing on {name}.", this);
            return;
        }

        if (animConfig == null)
        {
            Debug.LogError($"[Chest] {nameof(EntityAnimationConfig)} is missing on {name}.", this);
            return;
        }

        AudioSfxBridge.RequestPlay(AudioSfxKey.ChestOpened);
        animatable.PlayState(animConfig.OpenHash);
    }

    private IEnumerator WaitForOpenAnimation()
    {
        if (animatable == null || animConfig == null)
        {
            yield break;
        }

        yield return null;

        while (animatable.IsCurrentState(animConfig.OpenHash) &&
               animatable.GetCurrentStateNormalizedTime() < 1f)
        {
            yield return null;
        }
    }

    protected override void OnCollected(IEntity entity)
    {
        YokiFrame.EventKit.Enum.Send(RewardTrigger.ChestCollected);
    }
}
