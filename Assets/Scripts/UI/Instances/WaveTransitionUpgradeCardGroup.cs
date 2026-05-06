using System;
using System.Collections.Generic;
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

    public async UniTask PlayRefreshOutAsync(CancellationToken cancellationToken)
    {
        isSelectionLocked = true;
        if (upgradeContainers == null)
        {
            return;
        }

        List<UniTask> runningTasks = new();
        for (int i = 0; i < upgradeContainers.Length; i++)
        {
            UIUpgradeContainer container = upgradeContainers[i];
            if (container == null || !container.gameObject.activeInHierarchy)
            {
                continue;
            }

            runningTasks.Add(PlayContainerRefreshOutAsync(
                container,
                i * REFRESH_OUT_STAGGER_SECONDS,
                cancellationToken));
        }

        if (runningTasks.Count == 0)
        {
            return;
        }

        await UniTask.WhenAll(runningTasks);
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

    private static async UniTask PlayContainerRefreshOutAsync(
        UIUpgradeContainer container,
        float startDelay,
        CancellationToken cancellationToken)
    {
        container.SetInteractionLocked(true);
        if (startDelay > 0f)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(startDelay),
                DelayType.UnscaledDeltaTime,
                PlayerLoopTiming.Update,
                cancellationToken);
        }

        await container.PlayRefreshOutAsync(cancellationToken);
    }

    private void EnsureRoot()
    {
        if (root == null)
        {
            root = transform;
        }
    }
}
