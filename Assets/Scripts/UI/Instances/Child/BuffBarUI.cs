using System.Collections.Generic;
using UnityEngine;

public class BuffBarUI : MonoBehaviour
{
    [SerializeField] private BuffIconItem buffIconItemPrefab;
    [SerializeField] private Transform itemParent;

    private readonly List<BuffIconItem> spawnedItems = new();
    private Player player;
    private string subscribedPlayerEventBusId = string.Empty;

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

    private void OnEnable()
    {
        GameEventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        BindPlayer(FindFirstObjectByType<Player>());
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        UnsubscribePlayerEvents();
        SetVisibleItemCount(0);
        player = null;
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        BindPlayer(eventData.Player);
    }

    private void BindPlayer(Player newPlayer)
    {
        UnsubscribePlayerEvents();
        player = newPlayer;

        if (player == null)
        {
            SetVisibleItemCount(0);
            return;
        }

        subscribedPlayerEventBusId = player.RuntimeId;
        GameEventBus.Subscribe<ActiveBuffSnapshotChangedEvent, string>(subscribedPlayerEventBusId, OnActiveBuffSnapshotChanged);
        GameEventBus.Publish<RequestActiveBuffSnapshotEvent, string>(subscribedPlayerEventBusId);
    }

    private void UnsubscribePlayerEvents()
    {
        if (string.IsNullOrEmpty(subscribedPlayerEventBusId))
        {
            return;
        }

        GameEventBus.Unsubscribe<ActiveBuffSnapshotChangedEvent, string>(subscribedPlayerEventBusId, OnActiveBuffSnapshotChanged);
        subscribedPlayerEventBusId = string.Empty;
    }

    private void OnActiveBuffSnapshotChanged(ActiveBuffSnapshotChangedEvent eventData)
    {
        if (player == null)
        {
            return;
        }

        EnsureItemPoolSize(eventData.Buffs.Length);

        for (int i = 0; i < eventData.Buffs.Length; i++)
        {
            BuffIconItem item = spawnedItems[i];
            item.gameObject.SetActive(true);
            item.Configure(eventData.Buffs[i]);
            item.transform.SetSiblingIndex(i);
        }

        SetVisibleItemCount(eventData.Buffs.Length);
    }

    private void EnsureItemPoolSize(int requiredCount)
    {
        for (int i = spawnedItems.Count; i < requiredCount; i++)
        {
            BuffIconItem item = Instantiate(buffIconItemPrefab, itemParent);
            item.gameObject.SetActive(false);
            spawnedItems.Add(item);
        }
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
