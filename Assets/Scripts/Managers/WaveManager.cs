using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 敌波管理器：负责按配置的时间、频率生成敌人
/// </summary>
public class WaveManager : MonoSingletonBase<WaveManager>, IGameStateListener
{
    // 单个波次的总持续时间（单位：秒）
    [SerializeField] private float waveDuration;

    // 配置的所有波次数据数组
    [OnValueChanged("AutoSetWaveNames")] [SerializeField]
    private Wave[] waves;
#if UNITY_EDITOR
    private void AutoSetWaveNames()
    {
        if (waves == null) return;
        for (int i = 0; i < waves.Length; i++)
        {
            waves[i].name = $"Wave {i + 1}";
        }
    }
#endif

    // 每个波次分段的生成计数器列表：记录每个分段已生成的敌人数量，用于控制生成频率
    private List<int> counterList = new List<int>();
    private int currentWaveIndex = 0;

    public int CurrentWave => currentWaveIndex + 1;

    public int TotalWaves => waves.Length;

    // 波次运行计时器
    private float timer;
    private bool isTimerOn;

    [SerializeField] private Entity spawnAroundEntity;

    public static event Action<int, int> OnWaveStarted; // 传递当前波次和总波次
    public static event Action<int> OnWaveComplete;
    public static event Action OnAllWavesCompleted;
    public static event Action<float, float> OnWaveProgress; // 传递剩余时间, 总时间

    private void Update()
    {
        if (!isTimerOn) return;

        // 计时器未超过波次总时长时，持续执行当前波次的敌人生成逻辑
        if (timer < waveDuration)
        {
            ProcessCurrentWaveSpawns();
            timer += Time.deltaTime;
            OnWaveProgress?.Invoke(Mathf.Max(0, waveDuration - timer), waveDuration);
        }
        else
        {
            CompleteCurrentWave();
        }
    }

    /// <summary>
    /// 管理当前波次：遍历分段、计算时间、生成敌人
    /// </summary>
    private void ProcessCurrentWaveSpawns()
    {
        // 安全检查
        if (waves == null || currentWaveIndex >= waves.Length) return;

        // 获取当前激活的波次
        Wave currentWave = waves[currentWaveIndex];

        // 遍历当前波次的所有分段
        for (int i = 0; i < currentWave.segments.Count; i++)
        {
            WaveSegment segment = currentWave.segments[i];

            // 将百分比时间转换为实际秒数：分段的开始/结束时间
            float tStart = segment.timeStartEnd.x / 100f * waveDuration;
            float tEnd = segment.timeStartEnd.y / 100f * waveDuration;

            // 不在当前分段的时间范围内，跳过该分段
            if (timer < tStart || timer > tEnd)
                continue;

            // 当前时间距离分段开始的已过时间
            float timeSinceSegmentStart = timer - tStart;

            // 单个敌人的生成间隔（频率的倒数，单位：秒/个）
            float spawnDelay = 1f / segment.spawnFrequency;

            // 满足生成间隔条件：生成敌人，并更新计数器
            if (timeSinceSegmentStart / spawnDelay >= counterList[i])
            {
                // 在目标实体周围生成敌人，父物体为当前管理器
                Instantiate(segment.enemy.gameObject, GetSpawnPosition(spawnAroundEntity), Quaternion.identity,
                    transform);
                counterList[i]++;
            }
        }
    }

    /// <summary>
    /// 开始指定索引的波次
    /// </summary>
    private void StartWave(int waveIndex)
    {
        if (waves == null || waves.Length == 0)
        {
            Debug.LogWarning("WaveManager: 没有配置任何波次数据！");
            return;
        }

        if (waveIndex >= waves.Length)
        {
            OnAllWavesCompleted?.Invoke();
            GameManager.Instance.EnterWaveTransition();
            return;
        }

        currentWaveIndex = waveIndex;
        timer = 0f;
        counterList.Clear();

        for (int i = 0; i < waves[waveIndex].segments.Count; i++)
        {
            counterList.Add(0); // 从0开始计数，确保分段开始时能立刻生成第一个敌人
        }

        isTimerOn = true;
        OnWaveStarted?.Invoke(currentWaveIndex + 1, waves.Length);
    }

    /// <summary>
    /// 完成当前波次并准备进入下一波
    /// </summary>
    private void CompleteCurrentWave()
    {
        isTimerOn = false;
        OnWaveComplete?.Invoke(currentWaveIndex + 1);

        // 尝试开启下一波
        int nextWaveIndex = currentWaveIndex + 1;
        if (nextWaveIndex < waves.Length)
        {
            StartWave(nextWaveIndex);
        }
        else
        {
            OnAllWavesCompleted?.Invoke();
        }
    }

    private Vector3 GetSpawnPosition(IEntity entity)
    {
        if (entity == null) return Vector3.zero;

        Vector2 direction = Random.onUnitSphere;
        Vector2 offset = direction.normalized * Random.Range(6f, 10f);
        Vector2 targetPos = (Vector2)entity.Center + offset;
        targetPos.x = Mathf.Clamp(targetPos.x, -10f, 10f);
        targetPos.y = Mathf.Clamp(targetPos.y, -10f, 10f);

        return targetPos;
    }

    public void BeforeGameStateChanged(GameState oldState, GameState newState)
    {
    }

    public void AfterGameStateChanged(GameState oldState, GameState newState)
    {
        switch (newState)
        {
            case GameState.Game:
                // 每次进入Game状态时，从第0波重新开始
                StartWave(0);
                break;
            case GameState.WaveTransition:
            case GameState.Shop:
                DefeatAllEnemies();
                break;
            case GameState.GameOver:
                isTimerOn = false;
                DefeatAllEnemies();
                break;
        }
    }

    private void DefeatAllEnemies()
    {
        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            enemy.PassAwayAfterWave();
        }
    }
}

/// <summary>
/// 波次数据结构：存储一个完整波次的配置
/// </summary>
[Serializable]
public struct Wave
{
    // 波次名称（用于编辑器区分）
    public string name;

    // 波次包含的所有分段列表
    public List<WaveSegment> segments;
}

/// <summary>
/// 波次分段数据：单个分段的敌人生成配置
/// </summary>
[Serializable]
public struct WaveSegment
{
    // 当前分段要生成的敌人预制体
    public Enemy enemy;

    // 敌人生成频率（单位：个/秒）
    public float spawnFrequency;

    // 分段生效的时间区间（0-100百分比，对应波次总时长）
    [MinMaxSlider(0, 100)] public Vector2 timeStartEnd;
}