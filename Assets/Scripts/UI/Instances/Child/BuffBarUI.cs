using System.Collections.Generic;
using UnityEngine;

public class BuffBarUI : MonoBehaviour
{
    [SerializeField] private BuffIconItem buffIconItemPrefab;
    [SerializeField] private Transform itemParent;

    private readonly List<BuffIconItem> spawnedItems = new();
    private Player player;
    private BuffController buffController;
    private UITooltipPresenter tooltipPresenter;

    private void Awake()
    {
        if (buffIconItemPrefab == null)
        {
            throw new MissingReferenceException($"{nameof(BuffBarUI)} '{name}' is missing {nameof(BuffIconItem)} prefab.");
        }

        if (itemParent == null)
        {
            throw new MissingReferenceException($"{nameof(BuffBarUI)} '{name}' is missing item parent.");
        }
    }

    private void OnDisable()
    {
        UnbindPlayer();
        SetVisibleItemCount(0);
    }

    public void BindPlayer(Player targetPlayer)
    {
        UnbindPlayer();
        player = targetPlayer;

        if (player == null)
        {
            SetVisibleItemCount(0);
            return;
        }

        buffController = player.GetComponent<BuffController>();
        if (buffController == null)
        {
            SetVisibleItemCount(0);
            return;
        }

        buffController.OnActiveBuffSnapshotChanged += RenderBuffSnapshots;
        RenderBuffSnapshots(buffController.BuildSnapshots());
    }

    public void SetTooltipPresenter(UITooltipPresenter presenter)
    {
        tooltipPresenter = presenter;
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            ConfigureTooltipPresenter(spawnedItems[i]);
        }
    }

    public void UnbindPlayer()
    {
        if (buffController != null)
        {
            buffController.OnActiveBuffSnapshotChanged -= RenderBuffSnapshots;
            buffController = null;
        }

        player = null;
    }

    private void RenderBuffSnapshots(ActiveBuffSnapshot[] snapshots)
    {
        if (player == null)
        {
            return;
        }

        int snapshotCount = snapshots != null ? snapshots.Length : 0;
        EnsureItemPoolSize(snapshotCount);

        for (int i = 0; i < snapshotCount; i++)
        {
            BuffIconItem item = spawnedItems[i];
            item.gameObject.SetActive(true);
            item.Configure(snapshots[i]);
            item.transform.SetSiblingIndex(i);
        }

        SetVisibleItemCount(snapshotCount);
    }

    private void EnsureItemPoolSize(int requiredCount)
    {
        for (int i = spawnedItems.Count; i < requiredCount; i++)
        {
            BuffIconItem item = Instantiate(buffIconItemPrefab, itemParent);
            ConfigureTooltipPresenter(item);
            item.gameObject.SetActive(false);
            spawnedItems.Add(item);
        }
    }

    private void ConfigureTooltipPresenter(BuffIconItem item)
    {
        if (item == null)
        {
            return;
        }

        TooltipHoverTarget hoverTarget = item.GetComponent<TooltipHoverTarget>();
        hoverTarget?.SetTooltipPresenter(tooltipPresenter);
    }

    private void SetVisibleItemCount(int visibleCount)
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            bool isVisible = i < visibleCount;
            spawnedItems[i].gameObject.SetActive(isVisible);
        }
    }
}
