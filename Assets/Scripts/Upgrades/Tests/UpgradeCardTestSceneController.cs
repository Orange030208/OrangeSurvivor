using System;
using UnityEngine;

public class UpgradeCardTestSceneController : MonoBehaviour
{
    private const string SELF_TEST_PASSED_MARKER_NAME = "Upgrade Card Test Result - Passed";
    private const string SELF_TEST_FAILED_MARKER_NAME = "Upgrade Card Test Result - Failed";

    [SerializeField] private UIManager uiManager;
    [SerializeField] private Player playerPrefab;
    [SerializeField] private CharacterDataSO testCharacterData;
    [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;
    [SerializeField] private int initialUpgradePoints = 3;
    [SerializeField] private int testWaveNumber = 3;
    [SerializeField] private int initialGold = 60;
    [SerializeField] private bool runSelfTestOnStart = true;
    [SerializeField] private float selfTestTimeoutSeconds = 3f;

    private Player player;
    private UpgradeCardOptionSnapshot[] latestUpgradeOptions = Array.Empty<UpgradeCardOptionSnapshot>();
    private TransitionPhase latestTransitionPhase = TransitionPhase.None;

    private void OnEnable()
    {
        GameEventBus.Subscribe<UpgradeOptionsChangedEvent>(OnUpgradeOptionsChanged);
        GameEventBus.Subscribe<WaveTransitionPhaseChangedEvent>(OnWaveTransitionPhaseChanged);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<UpgradeOptionsChangedEvent>(OnUpgradeOptionsChanged);
        GameEventBus.Unsubscribe<WaveTransitionPhaseChangedEvent>(OnWaveTransitionPhaseChanged);
    }

    private System.Collections.IEnumerator Start()
    {
        EnsurePlayer();
        yield return null;
        EnsureManagers();
        PublishPlayerReady();
        GrantUpgradePoints();
        OpenUpgradePage();

        if (runSelfTestOnStart)
        {
            yield return RunSelfTest();
        }
    }

    [NaughtyAttributes.Button]
    public void RestartUpgradeTest()
    {
        GrantUpgradePoints();
        OpenUpgradePage();
    }

    private void EnsurePlayer()
    {
        if (player != null)
        {
            return;
        }

        Player prefab = playerPrefab != null ? playerPrefab : ResourcesManager.GetDefaultPlayerPrefab();
        if (prefab == null)
        {
            Debug.LogError("[UpgradeCardTestSceneController] Missing player prefab.");
            return;
        }

        player = Instantiate(prefab, playerSpawnPosition, Quaternion.identity);
        ConfigurePlayerForUpgradeTest(player);
    }

    private void ConfigurePlayerForUpgradeTest(Player targetPlayer)
    {
        if (targetPlayer == null || testCharacterData == null)
        {
            return;
        }

        System.Type type = typeof(Player);
        System.Reflection.FieldInfo field = type.GetField(
            "characterData",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(targetPlayer, testCharacterData);
    }

    private void EnsureManagers()
    {
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }

        if (FindFirstObjectByType<WaveTransitionManager>() == null)
        {
            gameObject.AddComponent<WaveTransitionManager>();
        }

        if (FindFirstObjectByType<ShopManager>() == null)
        {
            gameObject.AddComponent<ShopManager>();
        }
    }

    private void PublishPlayerReady()
    {
        if (player == null)
        {
            return;
        }

        GameEventBus.Publish(new PlayerSpawnedEvent(player));
        CurrencyWallet wallet = player.GetComponent<CurrencyWallet>();
        wallet?.SetAmount(initialGold);
    }

    private void GrantUpgradePoints()
    {
        if (player == null)
        {
            return;
        }

        PlayerLevel playerLevel = player.GetComponent<PlayerLevel>();
        if (playerLevel == null)
        {
            return;
        }

        int targetPoints = Mathf.Max(1, initialUpgradePoints);
        int safety = 0;
        while (playerLevel.UnspentUpgradePoints < targetPoints && safety < 20)
        {
            playerLevel.AddXP(playerLevel.RequiredXP);
            safety++;
        }
    }

    private void OpenUpgradePage()
    {
        if (uiManager == null)
        {
            Debug.LogError("[UpgradeCardTestSceneController] Missing UIManager.");
            return;
        }

        GameEventBus.Publish(new WaveCompletedEvent(
            Mathf.Max(1, testWaveNumber),
            testWaveNumber + 1,
            WaveCompletionReason.DurationElapsed,
            0f,
            true));
        GameEventBus.Publish(new GameStateChangedEvent(GameState.Game, GameState.WaveTransition));
        uiManager.ResetToPage<WaveTransitionUIPage>();
    }

    private void OnUpgradeOptionsChanged(UpgradeOptionsChangedEvent eventData)
    {
        latestUpgradeOptions = eventData.Options ?? Array.Empty<UpgradeCardOptionSnapshot>();
    }

    private void OnWaveTransitionPhaseChanged(WaveTransitionPhaseChangedEvent eventData)
    {
        latestTransitionPhase = eventData.newPhase;
    }

    private System.Collections.IEnumerator RunSelfTest()
    {
        float timeoutAt = Time.unscaledTime + Mathf.Max(0.25f, selfTestTimeoutSeconds);
        while (Time.unscaledTime < timeoutAt && !IsUpgradePageReady())
        {
            yield return null;
        }

        ValidateSelfTestResult();
    }

    private bool IsUpgradePageReady()
    {
        return uiManager != null
               && uiManager.IsPageOpen<WaveTransitionUIPage>()
               && latestTransitionPhase == TransitionPhase.UpgradeSelection
               && latestUpgradeOptions.Length > 0;
    }

    private void ValidateSelfTestResult()
    {
        UpgradeCardPoolSO pool = ResourcesManager.GetUpgradeCardPool();
        int cardCount = pool?.Cards?.Count ?? 0;
        if (pool == null || cardCount < 3)
        {
            FailSelfTest($"upgrade card pool is invalid. CardCount={cardCount}.");
            return;
        }

        PlayerLevel playerLevel = player != null ? player.GetComponent<PlayerLevel>() : null;
        if (player == null || playerLevel == null || playerLevel.UnspentUpgradePoints <= 0)
        {
            FailSelfTest("player or upgrade points are not ready.");
            return;
        }

        if (uiManager == null || !uiManager.IsPageOpen<WaveTransitionUIPage>())
        {
            FailSelfTest("WaveTransitionUIPage did not open.");
            return;
        }

        if (latestTransitionPhase != TransitionPhase.UpgradeSelection)
        {
            FailSelfTest($"expected UpgradeSelection phase, got {latestTransitionPhase}.");
            return;
        }

        int expectedOptionCount = Mathf.Max(3, pool.OptionCount);
        if (latestUpgradeOptions.Length < expectedOptionCount)
        {
            FailSelfTest($"expected at least {expectedOptionCount} upgrade options, got {latestUpgradeOptions.Length}.");
            return;
        }

        if (!ValidateOfferRules(pool))
        {
            return;
        }

        if (!ValidateRarityPresentationProfiles())
        {
            return;
        }

        WaveTransitionUIPage page = FindFirstObjectByType<WaveTransitionUIPage>(FindObjectsInactive.Include);
        UIUpgradeContainer[] containers = GetUpgradeContainers(page);
        if (containers.Length < latestUpgradeOptions.Length)
        {
            FailSelfTest($"upgrade UI containers are not configured. Containers={containers.Length}, Options={latestUpgradeOptions.Length}.");
            return;
        }

        int activeContainerCount = CountActiveContainers(containers);
        if (activeContainerCount < latestUpgradeOptions.Length)
        {
            FailSelfTest($"not all upgrade UI containers are active. Active={activeContainerCount}, Options={latestUpgradeOptions.Length}.");
            return;
        }

        if (!ValidateUpgradeCardMotion(containers))
        {
            return;
        }

        CreateSelfTestMarker(
            SELF_TEST_PASSED_MARKER_NAME,
            SELF_TEST_FAILED_MARKER_NAME);
        Debug.Log($"[UpgradeCardTestSceneController] Self-test passed. Cards={cardCount}, Options={latestUpgradeOptions.Length}, UpgradePoints={playerLevel.UnspentUpgradePoints}.");
    }

    private bool ValidateOfferRules(UpgradeCardPoolSO pool)
    {
        if (!TryFindCard(pool, "new_weapon_cache", out UpgradeCardSO minWaveCard) ||
            !TryFindCard(pool, "heavy_critical", out UpgradeCardSO tagRequirementCard) ||
            !TryFindCard(pool, "weapon_focus", out UpgradeCardSO weaponRequirementCard) ||
            !TryFindCard(pool, "long_barrel", out UpgradeCardSO weaponTagRequirementCard) ||
            !TryFindCard(pool, "tough_body", out UpgradeCardSO defenseCard) ||
            !TryFindCard(pool, "glass_cannon", out UpgradeCardSO glassCannonCard))
        {
            FailSelfTest("required rule test cards are missing from the pool.");
            return false;
        }

        UpgradeRunState emptyState = new();
        WeaponsHolder weaponsHolder = player != null ? player.GetComponent<WeaponsHolder>() : null;
        UpgradeCardOfferContext waveOneContext = new(emptyState, 1, weaponsHolder);
        if (minWaveCard.OfferConditions.AreSatisfied(waveOneContext))
        {
            FailSelfTest("min-wave requirement did not block new_weapon_cache at wave 1.");
            return false;
        }

        UpgradeCardOfferContext waveTwoContext = new(emptyState, 2, weaponsHolder);
        if (!minWaveCard.OfferConditions.AreSatisfied(waveTwoContext))
        {
            FailSelfTest("min-wave requirement did not allow new_weapon_cache at wave 2.");
            return false;
        }

        if (tagRequirementCard.OfferConditions.AreSatisfied(waveTwoContext))
        {
            FailSelfTest("tag requirement did not block heavy_critical before critical picks.");
            return false;
        }

        emptyState.RecordPick(CreateRuntimeTagProbeCard("critical_probe", UpgradeCardTag.Critical));
        if (!tagRequirementCard.OfferConditions.AreSatisfied(waveTwoContext))
        {
            FailSelfTest("tag requirement did not allow heavy_critical after critical picks.");
            return false;
        }

        if (!weaponRequirementCard.OfferConditions.AreSatisfied(waveTwoContext))
        {
            FailSelfTest("owned-weapon requirement did not detect the test character weapon.");
            return false;
        }

        if (weaponTagRequirementCard.OfferConditions.RequiredOwnedWeaponTags.Count > 0 &&
            !weaponTagRequirementCard.OfferConditions.AreSatisfied(waveTwoContext))
        {
            FailSelfTest("owned-weapon-tag requirement did not detect the test character weapon tags.");
            return false;
        }

        UpgradeRunState mutualState = new();
        mutualState.RecordPick(defenseCard);
        UpgradeCardOfferContext mutualContext = new(mutualState, 6, weaponsHolder);
        UpgradeCardRollService rollService = new();
        for (int i = 0; i < 24; i++)
        {
            var options = rollService.RollOptions(pool, mutualContext);
            for (int optionIndex = 0; optionIndex < options.Count; optionIndex++)
            {
                if (options[optionIndex] == glassCannonCard)
                {
                    FailSelfTest("mutual exclusion allowed glass_cannon after tough_body was picked.");
                    return false;
                }
            }
        }

        var oneOfferOptions = rollService.RollOptions(pool, new UpgradeCardOfferContext(new UpgradeRunState(), 6, weaponsHolder));
        if (ContainsBoth(oneOfferOptions, defenseCard, glassCannonCard))
        {
            FailSelfTest("mutual exclusion allowed tough_body and glass_cannon in the same offer.");
            return false;
        }

        return true;
    }

    private bool ValidateUpgradeCardMotion(UIUpgradeContainer[] containers)
    {
        for (int i = 0; i < containers.Length; i++)
        {
            UIUpgradeContainer container = containers[i];
            if (container == null || !container.gameObject.activeSelf)
            {
                continue;
            }

            if (container.GetComponent<UIMotionPlayer>() == null)
            {
                FailSelfTest($"upgrade container {container.name} is missing UIMotionPlayer.");
                return false;
            }

            if (container.GetComponent<UIMotionTrigger>() == null)
            {
                FailSelfTest($"upgrade container {container.name} is missing UIMotionTrigger.");
                return false;
            }
        }

        return true;
    }

    private bool ValidateRarityPresentationProfiles()
    {
        UpgradeCardRarityPresentationCatalogSO catalog = ResourcesManager.GetUpgradeCardRarityPresentationCatalog();
        if (catalog == null)
        {
            FailSelfTest("rarity presentation catalog is missing.");
            return false;
        }

        UpgradeCardRarity[] rarities =
        {
            UpgradeCardRarity.Common,
            UpgradeCardRarity.Rare,
            UpgradeCardRarity.Epic,
            UpgradeCardRarity.Legendary
        };

        for (int i = 0; i < rarities.Length; i++)
        {
            if (!catalog.TryGetProfile(rarities[i], out UpgradeCardRarityPresentationProfile profile))
            {
                FailSelfTest($"rarity presentation profile is not configured for {rarities[i]}.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(profile.PresentationKey))
            {
                FailSelfTest($"rarity presentation key is invalid for {rarities[i]}.");
                return false;
            }
        }

        return true;
    }

    private void FailSelfTest(string reason)
    {
        CreateSelfTestMarker(SELF_TEST_FAILED_MARKER_NAME, SELF_TEST_PASSED_MARKER_NAME);
        Debug.LogError($"[UpgradeCardTestSceneController] Self-test failed: {reason}");
    }

    private static void CreateSelfTestMarker(string markerName, string staleMarkerName)
    {
        GameObject staleMarker = GameObject.Find(staleMarkerName);
        if (staleMarker != null)
        {
            Destroy(staleMarker);
        }

        GameObject marker = GameObject.Find(markerName);
        if (marker == null)
        {
            marker = new GameObject(markerName);
        }

        marker.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        marker.SetActive(true);
        marker.tag = "Untagged";
        marker.name = markerName;
    }

    private static UIUpgradeContainer[] GetUpgradeContainers(WaveTransitionUIPage page)
    {
        if (page == null)
        {
            return Array.Empty<UIUpgradeContainer>();
        }

        System.Reflection.FieldInfo field = typeof(WaveTransitionUIPage).GetField(
            "upgradeContainers",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return field?.GetValue(page) as UIUpgradeContainer[] ?? Array.Empty<UIUpgradeContainer>();
    }

    private static int CountActiveContainers(UIUpgradeContainer[] containers)
    {
        int count = 0;
        for (int i = 0; i < containers.Length; i++)
        {
            if (containers[i] != null && containers[i].gameObject.activeSelf)
            {
                count++;
            }
        }

        return count;
    }

    private static bool TryFindCard(UpgradeCardPoolSO pool, string cardId, out UpgradeCardSO card)
    {
        card = null;
        if (pool?.Cards == null || string.IsNullOrWhiteSpace(cardId))
        {
            return false;
        }

        for (int i = 0; i < pool.Cards.Count; i++)
        {
            UpgradeCardSO candidate = pool.Cards[i];
            if (candidate != null && string.Equals(candidate.CardId, cardId, StringComparison.Ordinal))
            {
                card = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool ContainsBoth(
        System.Collections.Generic.IReadOnlyList<UpgradeCardSO> options,
        UpgradeCardSO first,
        UpgradeCardSO second)
    {
        if (options == null || first == null || second == null)
        {
            return false;
        }

        bool containsFirst = false;
        bool containsSecond = false;
        for (int i = 0; i < options.Count; i++)
        {
            containsFirst |= options[i] == first;
            containsSecond |= options[i] == second;
        }

        return containsFirst && containsSecond;
    }

    private static UpgradeCardSO CreateRuntimeTagProbeCard(string cardId, UpgradeCardTag tag)
    {
        UpgradeCardSO card = ScriptableObject.CreateInstance<UpgradeCardSO>();
        card.InitializeRuntime(
            cardId,
            "Tag Probe",
            UpgradeCardRarity.Common,
            1,
            new[] { tag },
            "Test only.",
            new[] { new PropModifierData(PropType.Attack, PropModifierType.Add, 1f) });
        return card;
    }
}
