using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public class WaveTransitionUpgradeCardGroup : ViewPartBase
{
    private const float REFRESH_OUT_STAGGER_SECONDS = 0.04f;

    [SerializeField] private Transform root;
    [SerializeField] private UIUpgradeContainer[] upgradeContainers;

    private bool isSelectionLocked;

    private void Awake()
    {
        EnsureRoot();
        BindSubmitGates();
    }

    public void Configure(UpgradeCardOptionSnapshot[] options)
    {
        isSelectionLocked = false;
        BindSubmitGates();
        if (upgradeContainers == null)
        {
            return;
        }

        for (int i = 0; i < upgradeContainers.Length; i++)
        {
            UIUpgradeContainer container = upgradeContainers[i];
            if (container == null)
            {
                continue;
            }

            bool hasOption = options != null && i < options.Length;
            container.gameObject.SetActive(hasOption);
            container.SetInteractionLocked(false);

            if (!hasOption)
            {
                continue;
            }

            container.Configure(new InfoAddIndex<UpgradeCardOptionSnapshot>(options[i], i));
        }
    }

    public void SetVisible(bool visible)
    {
        EnsureRoot();
        if (root != null)
        {
            root.gameObject.SetActive(visible);
        }

        if (!visible)
        {
            Clear();
        }
    }

    public void Clear()
    {
        isSelectionLocked = false;
        if (upgradeContainers == null)
        {
            return;
        }

        for (int i = 0; i < upgradeContainers.Length; i++)
        {
            UIUpgradeContainer container = upgradeContainers[i];
            if (container == null)
            {
                continue;
            }

            container.SetInteractionLocked(false);
            container.BindSubmitGate(null);
            container.Dispose();
        }
    }

    public IEnumerator PlayRefreshOutAndWait()
    {
        isSelectionLocked = true;
        if (upgradeContainers == null)
        {
            yield break;
        }

        int runningCount = 0;
        for (int i = 0; i < upgradeContainers.Length; i++)
        {
            UIUpgradeContainer container = upgradeContainers[i];
            if (container == null || !container.gameObject.activeInHierarchy)
            {
                continue;
            }

            runningCount++;
            StartCoroutine(PlayContainerRefreshOut(container, i * REFRESH_OUT_STAGGER_SECONDS, () => runningCount--));
        }

        while (runningCount > 0)
        {
            yield return null;
        }
    }

    public UniTask PlayRefreshOutAsync(CancellationToken cancellationToken)
    {
        return PlayRefreshOutAndWait().ToUniTask(cancellationToken: cancellationToken);
    }

    private bool TryBeginSelection(int selectedIndex)
    {
        if (isSelectionLocked)
        {
            return false;
        }

        isSelectionLocked = true;
        if (upgradeContainers == null)
        {
            return true;
        }

        for (int i = 0; i < upgradeContainers.Length; i++)
        {
            UIUpgradeContainer container = upgradeContainers[i];
            if (container == null)
            {
                continue;
            }

            container.SetInteractionLocked(true);
        }

        return true;
    }

    private void BindSubmitGates()
    {
        if (upgradeContainers == null)
        {
            return;
        }

        for (int i = 0; i < upgradeContainers.Length; i++)
        {
            if (upgradeContainers[i] == null)
            {
                continue;
            }

            upgradeContainers[i].BindSubmitGate(TryBeginSelection);
        }
    }

    private static IEnumerator PlayContainerRefreshOut(
        UIUpgradeContainer container,
        float startDelay,
        System.Action onComplete)
    {
        container.SetInteractionLocked(true);
        if (startDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(startDelay);
        }

        yield return container.PlayRefreshOutAndWait();
        onComplete?.Invoke();
    }

    private void EnsureRoot()
    {
        if (root == null)
        {
            root = transform;
        }
    }
}
