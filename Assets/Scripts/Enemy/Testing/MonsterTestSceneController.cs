using System;
using UnityEngine;

public sealed class MonsterTestSceneController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private MonsterTestTarget target;

    [Header("Enemy Data")]
    [SerializeField] private EnemySO[] testEnemies;
    [SerializeField] private int defaultEnemyIndex;

    [Header("Spawn")]
    [SerializeField] private Transform spawnRoot;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool keepSimulationRunning = true;

    private EnemyFactory enemyFactory;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (keepSimulationRunning)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            GameSimulation.SetManualOverride(true);
        }

        enemyFactory = new EnemyFactory();
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnDefaultEnemy();
        }
    }

    private void OnDestroy()
    {
        if (keepSimulationRunning)
        {
            GameSimulation.ClearManualOverride();
            Time.timeScale = previousTimeScale;
        }
    }

    [ContextMenu("Spawn Default Enemy")]
    public void SpawnDefaultEnemy()
    {
        EnemySO definition = ResolveDefaultEnemy();
        Transform spawnPoint = ResolveSpawnPoint(0);
        SpawnEnemy(definition, spawnPoint.position);
    }

    [ContextMenu("Spawn All Test Enemies")]
    public void SpawnAllTestEnemies()
    {
        if (testEnemies == null || testEnemies.Length == 0)
        {
            Debug.LogWarning($"{nameof(MonsterTestSceneController)} has no test enemies configured.");
            return;
        }

        for (int i = 0; i < testEnemies.Length; i++)
        {
            EnemySO definition = testEnemies[i];
            if (definition == null)
            {
                continue;
            }

            Transform spawnPoint = ResolveSpawnPoint(i);
            SpawnEnemy(definition, spawnPoint.position);
        }
    }

    [ContextMenu("Clear Spawned Enemies")]
    public void ClearSpawnedEnemies()
    {
        if (spawnRoot == null)
        {
            return;
        }

        for (int i = spawnRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = spawnRoot.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void SpawnEnemy(EnemySO definition, Vector3 spawnPosition)
    {
        if (definition == null)
        {
            throw new MissingReferenceException($"{nameof(MonsterTestSceneController)} requires a non-null {nameof(EnemySO)}.");
        }

        if (target == null)
        {
            throw new MissingReferenceException($"{nameof(MonsterTestSceneController)} requires a {nameof(MonsterTestTarget)}.");
        }

        enemyFactory.Spawn(definition, target, spawnPosition, spawnRoot);
    }

    private EnemySO ResolveDefaultEnemy()
    {
        if (testEnemies == null || testEnemies.Length == 0)
        {
            throw new MissingReferenceException($"{nameof(MonsterTestSceneController)} requires at least one configured {nameof(EnemySO)}.");
        }

        int clampedIndex = Mathf.Clamp(defaultEnemyIndex, 0, testEnemies.Length - 1);
        EnemySO definition = testEnemies[clampedIndex];
        if (definition == null)
        {
            throw new MissingReferenceException($"{nameof(MonsterTestSceneController)} has an empty default enemy slot at index {clampedIndex}.");
        }

        return definition;
    }

    private Transform ResolveSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            throw new MissingReferenceException($"{nameof(MonsterTestSceneController)} requires at least one spawn point.");
        }

        int resolvedIndex = Math.Abs(index) % spawnPoints.Length;
        Transform spawnPoint = spawnPoints[resolvedIndex];
        if (spawnPoint == null)
        {
            throw new MissingReferenceException($"{nameof(MonsterTestSceneController)} spawn point {resolvedIndex} is missing.");
        }

        return spawnPoint;
    }
}
