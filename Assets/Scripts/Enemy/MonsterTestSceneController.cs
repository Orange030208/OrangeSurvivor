using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Minimal scene bootstrap for Monster Test Scene. It keeps monster testing independent
/// from the full menu/UI wave flow while still using production enemy configuration.
/// </summary>
public sealed class MonsterTestSceneController : MonoBehaviour
{
    private const string DEFAULT_ENEMY_RESOURCE_PATH = "Data/Enemies/Skeleton/SkeletonEnemy";
    private const string DEFAULT_PLAYER_RESOURCE_PATH = "Prefabs/Player/Character";
    private const string DEFAULT_CHARACTER_RESOURCE_PATH = "Data/Characters/Test";

    [Header("Target")]
    [SerializeField] public Player testPlayer;
    [SerializeField] public string playerResourcePath = DEFAULT_PLAYER_RESOURCE_PATH;
    [SerializeField] public CharacterDataSO testCharacterData;
    [SerializeField] public string characterResourcePath = DEFAULT_CHARACTER_RESOURCE_PATH;
    [SerializeField] public Vector3 playerSpawnPosition = Vector3.zero;

    [Header("Enemy")]
    [SerializeField] public EnemySO enemyData;
    [SerializeField] public string enemyResourcePath = DEFAULT_ENEMY_RESOURCE_PATH;
    [SerializeField] public int spawnCount = 3;
    [SerializeField] public float spawnRadius = 3f;
    [SerializeField] public bool spawnOnStart = true;

    [Header("Runtime")]
    [FormerlySerializedAs("enemyRuntimeRegistry")]
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
            Player playerPrefab = Resources.Load<Player>(playerResourcePath);
            if (playerPrefab == null)
            {
                Debug.LogError($"{nameof(MonsterTestSceneController)} cannot load player prefab at Resources/{playerResourcePath}.");
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

        if (string.IsNullOrWhiteSpace(characterResourcePath))
        {
            return null;
        }

        testCharacterData = Resources.Load<CharacterDataSO>(characterResourcePath);
        if (testCharacterData == null)
        {
            Debug.LogWarning($"{nameof(MonsterTestSceneController)} cannot load character data at Resources/{characterResourcePath}.");
        }

        return testCharacterData;
    }

    private void SelectTestCharacter()
    {
        CharacterDataSO characterData = ResolveCharacterData();
        CharacterDataSO[] characters = ResourcesManager.GetAllCharacters();
        if (characters == null || characters.Length == 0)
        {
            Debug.LogWarning($"{nameof(MonsterTestSceneController)} cannot select a test character because no character data was loaded.");
            return;
        }

        int selectedIndex = 0;
        if (characterData != null)
        {
            for (int i = 0; i < characters.Length; i++)
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

        if (string.IsNullOrWhiteSpace(enemyResourcePath))
        {
            Debug.LogError($"{nameof(MonsterTestSceneController)} requires an enemy data asset or Resources path.");
            return null;
        }

        enemyData = Resources.Load<EnemySO>(enemyResourcePath);
        if (enemyData == null)
        {
            Debug.LogError($"{nameof(MonsterTestSceneController)} cannot load enemy data at Resources/{enemyResourcePath}.");
        }

        return enemyData;
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
