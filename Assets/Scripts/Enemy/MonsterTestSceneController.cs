using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 怪物测试场景的最小启动入口。
/// 让怪物测试脱离完整菜单、UI 和波次流程，同时继续使用正式敌人配置。
/// </summary>
public sealed class MonsterTestSceneController : MonoBehaviour
{
    [Header("目标")]
    [SerializeField] public Player testPlayer;
    [SerializeField] public CharacterDataSO testCharacterData;
    [SerializeField] public Vector3 playerSpawnPosition = Vector3.zero;

    [Header("敌人")]
    [SerializeField] public EnemySO enemyData;
    [SerializeField] public int spawnCount = 3;
    [SerializeField] public float spawnRadius = 3f;
    [SerializeField] public bool spawnOnStart = true;

    [Header("运行时")]
    [SerializeField] public EnemyRegistry enemyRegistry;
    [SerializeField] public Transform enemyParent;

    private EnemyFactory enemyFactory;

    private void Awake()
    {
        EnsureRuntimeRegistry();
        EnsureEnemyParent();
        enemyFactory = new EnemyFactory();
    }

    private void Start()
    {
        EnsureTestPlayer();
        SelectTestCharacter();
        PublishTestGameState();

        if (spawnOnStart)
        {
            SpawnConfiguredEnemies();
        }
    }

    [ContextMenu("Spawn Configured Enemies")]
    public void SpawnConfiguredEnemies()
    {
        EnsureTestPlayer();
        EnemySO resolvedEnemyData = ResolveEnemyData();
        if (resolvedEnemyData == null || testPlayer == null)
        {
            return;
        }

        int safeSpawnCount = Mathf.Max(1, spawnCount);
        float safeSpawnRadius = Mathf.Max(0.5f, spawnRadius);
        for (int i = 0; i < safeSpawnCount; i++)
        {
            float angle = Mathf.PI * 2f * i / safeSpawnCount;
            Vector3 spawnPosition = testPlayer.transform.position + new Vector3(
                Mathf.Cos(angle) * safeSpawnRadius,
                Mathf.Sin(angle) * safeSpawnRadius,
                0f);

            enemyFactory.Spawn(resolvedEnemyData, testPlayer, spawnPosition, enemyParent);
        }
    }

    [ContextMenu("Clear Test Enemies")]
    public void ClearTestEnemies()
    {
        EnsureRuntimeRegistry();
        enemyRegistry.DefeatAllTrackedEnemies();
    }

    private void EnsureRuntimeRegistry()
    {
        if (enemyRegistry != null)
        {
            return;
        }

        enemyRegistry = FindFirstObjectByType<EnemyRegistry>();
        if (enemyRegistry == null)
        {
            enemyRegistry = new GameObject(nameof(EnemyRegistry)).AddComponent<EnemyRegistry>();
        }
    }

    private void EnsureEnemyParent()
    {
        if (enemyParent != null)
        {
            return;
        }

        GameObject enemyParentObject = GameObject.Find("Test Enemies");
        if (enemyParentObject == null)
        {
            enemyParentObject = new GameObject("Test Enemies");
        }

        enemyParent = enemyParentObject.transform;
    }

    private void EnsureTestPlayer()
    {
        if (testPlayer == null)
        {
            testPlayer = FindFirstObjectByType<Player>();
        }

        if (testPlayer == null)
        {
            Player playerPrefab = GameContentRuntime.TryGetProvider(out IGameContentProvider provider)
                ? provider.DefaultPlayerPrefab
                : null;
            if (playerPrefab == null)
            {
                Debug.LogError($"{nameof(MonsterTestSceneController)} requires a test player or default player prefab in {nameof(GameContentCatalogSO)}.");
                return;
            }

            testPlayer = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
            testPlayer.name = "Monster Test Player";
        }

        GameEventBus.Publish(new PlayerSpawnedEvent(testPlayer));
    }

    private CharacterDataSO ResolveCharacterData()
    {
        if (testCharacterData != null)
        {
            return testCharacterData;
        }

        return null;
    }

    private void SelectTestCharacter()
    {
        CharacterDataSO characterData = ResolveCharacterData();
        IReadOnlyList<CharacterDataSO> characters = GameContentRuntime.Provider.Characters;
        if (characters == null || characters.Count == 0)
        {
            Debug.LogWarning($"{nameof(MonsterTestSceneController)} cannot select a test character because no character data was loaded.");
            return;
        }

        int selectedIndex = 0;
        if (characterData != null)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i] == characterData)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

        CharacterSelectionManager.Instance?.SelectCharacter(selectedIndex);
    }


    private EnemySO ResolveEnemyData()
    {
        if (enemyData != null)
        {
            return enemyData;
        }

        Debug.LogError($"{nameof(MonsterTestSceneController)} requires an explicit enemy data asset.");
        return null;
    }

    private static void PublishTestGameState()
    {
        GameEventBus.Publish(new GameStateChangedEvent(GameState.None, GameState.Game));
    }

    private void OnValidate()
    {
        spawnCount = Math.Max(1, spawnCount);
        spawnRadius = Mathf.Max(0.5f, spawnRadius);
    }
}
