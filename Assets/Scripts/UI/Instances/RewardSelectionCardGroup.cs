using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public class RewardSelectionCardGroup : ViewPartBase
{
    private const float REFRESH_OUT_STAGGER_SECONDS = 0.04f;

    [SerializeField] private Transform root;
    [SerializeField] private RewardSelectionCardContainer cardContainerPrefab;

    private readonly List<RewardSelectionCardContainer> activeContainers = new();
    private bool isSelectionLocked;

    private void Awake()
    {
        EnsureRoot();
    }

    public void Configure(RewardSelectionCardViewModel[] options, Action<int, string> optionSelected)
    {
        isSelectionLocked = false;
        int optionCount = options != null ? options.Length : 0;
        RebuildContainers(optionCount);

        for (int i = 0; i < activeContainers.Count; i++)
        {
            RewardSelectionCardContainer container = activeContainers[i];
            if (container == null)
            {
                continue;
            }

            bool hasOption = options != null && i < options.Length;
            container.SetInteractionLocked(false);

            if (!hasOption)
            {
                continue;
            }

            container.Configure(new RewardSelectionCardBinding(options[i], i, optionSelected));
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
        ClearGeneratedContainers();
    }

    public async UniTask PlayRefreshOutAsync(CancellationToken cancellationToken)
    {
        isSelectionLocked = true;

        List<UniTask> runningTasks = new();
        for (int i = 0; i < activeContainers.Count; i++)
        {
            RewardSelectionCardContainer container = activeContainers[i];
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
        for (int i = 0; i < activeContainers.Count; i++)
        {
            RewardSelectionCardContainer container = activeContainers[i];
            if (container == null)
            {
                continue;
            }

            container.SetInteractionLocked(true);
        }

        return true;
    }

    private static async UniTask PlayContainerRefreshOutAsync(
        RewardSelectionCardContainer container,
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

    private void RebuildContainers(int requiredCount)
    {
        ClearGeneratedContainers();
        if (requiredCount <= 0)
        {
            return;
        }

        EnsureRoot();
        if (cardContainerPrefab == null)
        {
            Debug.LogError($"{nameof(RewardSelectionCardGroup)} '{name}' is missing card container prefab.", this);
            return;
        }

        Transform parent = root != null ? root : transform;
        for (int i = 0; i < requiredCount; i++)
        {
            RewardSelectionCardContainer container = Instantiate(cardContainerPrefab, parent, false);
            container.name = $"{cardContainerPrefab.name} ({i + 1})";
            container.gameObject.SetActive(true);
            container.BindSubmitGate(TryBeginSelection);
            activeContainers.Add(container);
        }
    }

    private void ClearGeneratedContainers()
    {
        for (int i = 0; i < activeContainers.Count; i++)
        {
            RewardSelectionCardContainer container = activeContainers[i];
            if (container == null)
            {
                continue;
            }

            container.SetInteractionLocked(false);
            container.BindSubmitGate(null);
            container.Dispose();
            container.gameObject.SetActive(false);
            DestroyGeneratedContainer(container);
        }

        activeContainers.Clear();
    }

    private static void DestroyGeneratedContainer(RewardSelectionCardContainer container)
    {
        if (container == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(container.gameObject);
        }
        else
        {
            DestroyImmediate(container.gameObject);
        }
    }
}
