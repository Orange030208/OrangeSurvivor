using UnityEngine;

public sealed class GolemMechaStoneLaserVisual : MonoBehaviour
{
    [SerializeField] private LineRenderer coreLine;
    [SerializeField] private Animator startVFXAnimator;
    [SerializeField, Range(0f, 1f)] private float coreLineStartNormalizedTime = 0.75f;

    private bool coreLineRequested;
    private Vector3 pendingStartPosition;
    private Vector3 pendingEndPosition;

    public bool IsCoreLineVisible => coreLine == null || coreLine.enabled;

    public void PlayStart()
    {
        ResolveReferences();
        coreLineRequested = false;
        SetCoreLineEnabled(false);

        if (startVFXAnimator == null)
        {
            return;
        }

        startVFXAnimator.enabled = true;
        startVFXAnimator.Play(0, 0, 0f);
    }

    public void ShowCore(
        Vector3 startPosition,
        Vector3 endPosition)
    {
        ResolveReferences();
        coreLineRequested = true;
        pendingStartPosition = startPosition;
        pendingEndPosition = endPosition;
        ConfigureLine(startPosition, endPosition);

        if (CanShowCoreLine())
        {
            SetCoreLineEnabled(true);
        }
    }
    
    public void Hide()
    {
        ResolveReferences();
        coreLineRequested = false;
        SetCoreLineEnabled(false);
        if (startVFXAnimator != null)
        {
            startVFXAnimator.enabled = false;
        }
    }

    private void Update()
    {
        if (!coreLineRequested)
        {
            return;
        }

        ConfigureLine(pendingStartPosition, pendingEndPosition);
        if (CanShowCoreLine())
        {
            SetCoreLineEnabled(true);
        }
    }

    private void ConfigureLine(
        Vector3 startPosition,
        Vector3 endPosition)
    {
        if (coreLine == null)
        {
            return;
        }

        coreLine.useWorldSpace = false;
        coreLine.positionCount = 2;
        coreLine.SetPosition(0, startPosition);
        coreLine.SetPosition(1, endPosition);
    }

    private bool CanShowCoreLine()
    {
        if (startVFXAnimator == null || !startVFXAnimator.enabled)
        {
            return true;
        }

        AnimatorStateInfo stateInfo = startVFXAnimator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.normalizedTime >= coreLineStartNormalizedTime;
    }

    private void SetCoreLineEnabled(bool enabled)
    {
        if (coreLine != null)
        {
            coreLine.enabled = enabled;
        }
    }

    private void Awake()
    {
        ResolveReferences();
        Hide();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (coreLine == null)
        {
            Transform coreTransform = transform.Find("Core");
            if (coreTransform != null)
            {
                coreTransform.TryGetComponent(out coreLine);
            }
        }

        if (startVFXAnimator == null)
        {
            Transform startTransform = transform.Find("Start");
            if (startTransform != null)
            {
                startTransform.TryGetComponent(out startVFXAnimator);
            }
        }
    }
}
