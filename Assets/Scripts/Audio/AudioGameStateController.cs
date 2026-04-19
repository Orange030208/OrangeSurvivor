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
            throw new MissingReferenceException($"{nameof(AudioGameStateController)} '{name}' is missing {nameof(AudioRuntimeSettingsSO)}.");
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

        if (entry.BgmKey == AudioBgmKey.None)
        {
            AudioPlaybackBridge.RequestStop(AudioBusType.Music);
            return;
        }

        if (!entry.RestartIfAlreadyPlaying && AudioManager.Instance.IsPlayingMusicCue(entry.BgmKey))
        {
            return;
        }

        AudioPlaybackBridge.RequestPlay(entry.BgmKey, entry.RestartIfAlreadyPlaying);
    }
}
