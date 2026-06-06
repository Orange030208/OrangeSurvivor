using System.Collections.Generic;
using Orange.UIFramework;
using UnityEngine;

public class BuffBarUI : ViewPartBase
{
    [SerializeField] private BuffIconItem buffIconItemPrefab;
    [SerializeField] private Transform itemParent;

    private readonly List<BuffIconItem> spawnedItems = new();
    private Player player;
    private BuffController buffController;

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
        EndSession();
    }

    public void BeginSession(Player targetPlayer)
    {
        ConfigureTooltipTargets();
        BindPlayer(targetPlayer);
    }

    public void EndSession()
    {
        UnbindPlayer();
        SetVisibleItemCount(0);
    }

    private void BindPlayer(Player targetPlayer)
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

        buffController.OnActiveBuffViewDataChanged += RenderBuffViewData;
        RenderBuffViewData(buffController.BuildActiveBuffViewData());
    }

    private void UnbindPlayer()
    {
        if (buffController != null)
        {
            buffController.OnActiveBuffViewDataChanged -= RenderBuffViewData;
            buffController = null;
        }

        player = null;
    }

    private void RenderBuffViewData(ActiveBuffViewData[] viewData)
    {
        if (player == null)
        {
            return;
        }

        int viewDataCount = viewData != null ? viewData.Length : 0;
        EnsureItemPoolSize(viewDataCount);

        for (int i = 0; i < viewDataCount; i++)
        {
            BuffIconItem item = spawnedItems[i];
            item.gameObject.SetActive(true);
            item.Configure(viewData[i]);
            item.transform.SetSiblingIndex(i);
        }

        SetVisibleItemCount(viewDataCount);
    }

    private void EnsureItemPoolSize(int requiredCount)
    {
        for (int i = spawnedItems.Count; i < requiredCount; i++)
        {
            BuffIconItem item = Instantiate(buffIconItemPrefab, itemParent);
            item.gameObject.SetActive(false);
            ConfigureTooltipTarget(item);
            spawnedItems.Add(item);
        }
    }

    private void ConfigureTooltipTargets()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            ConfigureTooltipTarget(spawnedItems[i]);
        }
    }

    private void ConfigureTooltipTarget(BuffIconItem item)
    {
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
