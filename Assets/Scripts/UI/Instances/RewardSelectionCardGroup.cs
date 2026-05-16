using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

[Serializable]
public sealed class RewardCardPrefabEntry
{
    [SerializeField] private RewardCardStyle style;
    [SerializeField] private RewardSelectionCardViewBase prefab;

    public RewardCardPrefabEntry()
    {
    }

    public RewardCardPrefabEntry(RewardCardStyle style, RewardSelectionCardViewBase prefab)
    {
        this.style = style;
        this.prefab = prefab;
    }

    public RewardCardStyle Style => style;
    public RewardSelectionCardViewBase Prefab => prefab;
}

public class RewardSelectionCardGroup : ViewPartBase
{
    private const float REFRESH_OUT_STAGGER_SECONDS = 0.04f;
    private const float SUBMIT_REJECTED_STAGGER_SECONDS = 0.045f;

    [SerializeField] private Transform root;
    [SerializeField] private RewardCardPrefabEntry[] cardPrefabs = Array.Empty<RewardCardPrefabEntry>();

    private readonly List<RewardSelectionCardViewBase> activeContainers = new();
    private CancellationTokenSource submitCancellation;
    private Action<int, string> optionSelected;
    private bool isSelectionLocked;

    private void Awake()
    {
        EnsureRoot();
    }

    public void Configure(IRewardCardPresentation[] options, Action<int, string> optionSelected)
    {
        CancelSubmitAnimation();
        this.optionSelected = optionSelected;
        isSelectionLocked = false;
        RebuildContainers(options);

        for (int i = 0; i < activeContainers.Count; i++)
        {
            RewardSelectionCardViewBase container = activeContainers[i];
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

            container.Configure(new RewardSelectionCardBinding(
                options[i],
                i,
                null,
                OnCardSubmitRequested));
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
        CancelSubmitAnimation();
        optionSelected = null;
        isSelectionLocked = false;
        ClearGeneratedContainers();
    }

    public async UniTask PlayRefreshOutAsync(CancellationToken cancellationToken)
    {
        isSelectionLocked = true;

        List<UniTask> runningTasks = new();
        for (int i = 0; i < activeContainers.Count; i++)
        {
            RewardSelectionCardViewBase container = activeContainers[i];
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
            RewardSelectionCardViewBase container = activeContainers[i];
            if (container == null)
            {
                continue;
            }

            container.SetInteractionLocked(true);
        }

        return true;
    }

    private void OnCardSubmitRequested(int selectedIndex, string selectedOptionId)
    {
        if (selectedIndex < 0 || selectedIndex >= activeContainers.Count)
        {
            Debug.LogWarning(
                $"{nameof(RewardSelectionCardGroup)} '{name}' received an invalid selected index '{selectedIndex}'.",
                this);
            return;
        }

        CancelSubmitAnimation();
        submitCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        PlaySubmitSelectionAsync(selectedIndex, selectedOptionId, submitCancellation).Forget();
    }

    private async UniTaskVoid PlaySubmitSelectionAsync(
        int selectedIndex,
        string selectedOptionId,
        CancellationTokenSource cancellationSource)
    {
        CancellationToken cancellationToken = cancellationSource.Token;
        try
        {
            List<UniTask> runningTasks = new();
            int rejectedOrder = 0;
            for (int i = 0; i < activeContainers.Count; i++)
            {
                RewardSelectionCardViewBase container = activeContainers[i];
                if (container == null || !container.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (i == selectedIndex)
                {
                    runningTasks.Add(container.PlaySelectedSubmitAsync(cancellationToken));
                    continue;
                }

                runningTasks.Add(container.PlayRejectedSubmitAsync(
                    rejectedOrder * SUBMIT_REJECTED_STAGGER_SECONDS,
                    cancellationToken));
                rejectedOrder++;
            }

            if (runningTasks.Count > 0)
            {
                await UniTask.WhenAll(runningTasks);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (ReferenceEquals(submitCancellation, cancellationSource))
            {
                optionSelected?.Invoke(selectedIndex, selectedOptionId);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(submitCancellation, cancellationSource))
            {
                submitCancellation = null;
            }

            cancellationSource.Dispose();
        }
    }

    private static async UniTask PlayContainerRefreshOutAsync(
        RewardSelectionCardViewBase container,
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

    private void RebuildContainers(IRewardCardPresentation[] options)
    {
        ClearGeneratedContainers();
        int requiredCount = options != null ? options.Length : 0;
        if (requiredCount <= 0)
        {
            return;
        }

        EnsureRoot();
        Transform parent = root != null ? root : transform;
        for (int i = 0; i < requiredCount; i++)
        {
            IRewardCardPresentation option = options[i];
            if (option == null)
            {
                throw new ArgumentException(
                    $"{nameof(RewardSelectionCardGroup)} '{name}' received a null reward option at index {i}.",
                    nameof(options));
            }

            RewardSelectionCardViewBase prefab = ResolvePrefab(option.Style);
            RewardSelectionCardViewBase container = Instantiate(prefab, parent, false);
            container.name = $"{prefab.name} ({i + 1})";
            container.gameObject.SetActive(true);
            container.BindSubmitGate(TryBeginSelection);
            activeContainers.Add(container);
        }
    }

    private void ClearGeneratedContainers()
    {
        for (int i = 0; i < activeContainers.Count; i++)
        {
            RewardSelectionCardViewBase container = activeContainers[i];
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

    private void CancelSubmitAnimation()
    {
        if (submitCancellation == null)
        {
            return;
        }

        CancellationTokenSource cancellationSource = submitCancellation;
        submitCancellation = null;
        cancellationSource.Cancel();
        cancellationSource.Dispose();
    }

    private void OnDisable()
    {
        CancelSubmitAnimation();
    }

    private void OnDestroy()
    {
        Clear();
    }

    private RewardSelectionCardViewBase ResolvePrefab(RewardCardStyle style)
    {
        if (cardPrefabs == null || cardPrefabs.Length == 0)
        {
            throw new MissingReferenceException(
                $"{nameof(RewardSelectionCardGroup)} '{name}' has no reward card prefab mappings.");
        }

        for (int i = 0; i < cardPrefabs.Length; i++)
        {
            RewardCardPrefabEntry entry = cardPrefabs[i];
            if (entry == null || entry.Style != style)
            {
                continue;
            }

            if (entry.Prefab == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(RewardSelectionCardGroup)} '{name}' has a null prefab for reward card style '{style}'.");
            }

            return entry.Prefab;
        }

        throw new MissingReferenceException(
            $"{nameof(RewardSelectionCardGroup)} '{name}' is missing reward card prefab mapping for style '{style}'.");
    }

    private static void DestroyGeneratedContainer(RewardSelectionCardViewBase container)
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
