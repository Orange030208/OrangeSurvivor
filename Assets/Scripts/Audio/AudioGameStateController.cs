using UnityEngine;

/// <summary>
/// GameState 到 BGM 的桥接控制器：
/// - 监听 GameStateChangedEvent；
/// - 根据 AudioRuntimeSettingsSO 查询目标 BGM；
/// - 在状态变化时请求播放/停止 Music 总线。
/// 它只消费状态事件，不参与 GameState 决策。
/// </summary>
[DisallowMultipleComponent]
public class AudioGameStateController : MonoBehaviour
{
    [Tooltip("运行时音频设置。提供 GameState 到 BGM 的映射与淡变参数。")]
    [SerializeField] private AudioRuntimeSettingsSO runtimeSettings;

    private void Awake()
    {
        if (runtimeSettings == null)
        {
            Debug.LogError($"{nameof(AudioGameStateController)} requires an {nameof(AudioRuntimeSettingsSO)} reference.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void Start()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            return;
        }

        HandleStateChanged(gameManager.CurrentGameState);
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        HandleStateChanged(eventData.NewState);
    }

    private void HandleStateChanged(GameState gameState)
    {
        if (!enabled)
        {
            return;
        }

        if (!runtimeSettings.TryGetGameStateBgmEntry(gameState, out AudioGameStateBgmEntry entry))
        {
            AudioPlaybackBridge.RequestStop(AudioBusType.Music);
            return;
        }

        if (string.IsNullOrWhiteSpace(entry.CueId))
        {
            AudioPlaybackBridge.RequestStop(AudioBusType.Music);
            return;
        }

        if (!entry.RestartIfAlreadyPlaying && AudioManager.Instance != null && AudioManager.Instance.IsPlayingMusicCue(entry.CueId))
        {
            return;
        }

        AudioPlaybackBridge.RequestPlay(entry.CueId, entry.RestartIfAlreadyPlaying);
    }
}
