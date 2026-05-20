using System.Collections;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class UpgradeCardTestSceneController : MonoBehaviour
{
    private const int GAME_STATE_WAIT_FRAME_LIMIT = 30;

    [SerializeField] private GameManager gameManager;
    [SerializeField] private Player playerPrefab;
    [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;
    [SerializeField] private int initialUpgradePoints = 3;
    [SerializeField] private int testWaveNumber = 3;
    [SerializeField] private int initialGold = 60;

    private Player player;

    private void Awake()
    {
        EnsureManagers();
        EnsurePlayer();
        ConfigureGameManagerForUpgradeTest();
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return WaitForGameState(GameState.Game);

        if (!CanRequestUpgradeReward())
        {
            yield break;
        }

        PublishPlayerReady();
        PublishWaveContext();
        GrantUpgradePoints();
    }

    [NaughtyAttributes.Button]
    public void RestartUpgradeTest()
    {
        if (!CanRequestUpgradeReward())
        {
            return;
        }

        PublishWaveContext();
        GrantUpgradePoints();
    }

    private void EnsurePlayer()
    {
        Player prefab = playerPrefab != null ? playerPrefab : GameContentRuntime.Provider.DefaultPlayerPrefab;
        if (prefab == null)
        {
            Debug.LogError($"[{nameof(UpgradeCardTestSceneController)}] Missing player prefab. Assign one in the scene or {nameof(GameContentCatalogSO)}.", this);
            return;
        }

        player = Instantiate(prefab, playerSpawnPosition, Quaternion.identity);
        ConfigurePlayerForUpgradeTest(player);
    }

    private void ConfigurePlayerForUpgradeTest(Player targetPlayer)
    {
        CharacterDataSO characterData = GameContentRuntime.Provider.DefaultCharacter;
        if (characterData == null)
        {
            Debug.LogError(
                $"[{nameof(UpgradeCardTestSceneController)}] Missing default character in {nameof(GameContentCatalogSO)}.",
                this);
            return;
        }

        System.Type type = typeof(Player);
        FieldInfo field = type.GetField(
            "characterData",
            BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(targetPlayer, characterData);
    }

    private void EnsureManagers()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
    }

    private void ConfigureGameManagerForUpgradeTest()
    {
        if (gameManager == null)
        {
            Debug.LogError("[UpgradeCardTestSceneController] Upgrade reward test requires a GameManager in the scene.", this);
            return;
        }

        SetPrivateField(gameManager, "player", player);
        SetPrivateField(gameManager, "initialGameState", GameState.Game);
    }

    private void PublishPlayerReady()
    {
        GameEventBus.Publish(new PlayerSpawnedEvent(player));
        CurrencyWallet wallet = player.GetComponent<CurrencyWallet>();
        wallet.SetAmount(initialGold);
    }

    private void GrantUpgradePoints()
    {
        PlayerLevel playerLevel = player.GetComponent<PlayerLevel>();
        int targetPoints = Mathf.Max(1, initialUpgradePoints);
        int safety = 0;
        while (playerLevel.UnspentUpgradePoints < targetPoints && safety < 20)
        {
            playerLevel.AddXP(playerLevel.RequiredXP);
            safety++;
        }
    }

    private void PublishWaveContext()
    {
        int currentWave = Mathf.Max(1, testWaveNumber);
        GameEventBus.Publish(new WaveStartedEvent(currentWave, currentWave));
        GameEventBus.Publish(new WaveRuntimeChangedEvent(currentWave, currentWave, true, true, true));
    }

    private IEnumerator WaitForGameState(GameState targetState)
    {
        int waitedFrames = 0;
        while (gameManager != null
               && gameManager.CurrentGameState != targetState
               && waitedFrames < GAME_STATE_WAIT_FRAME_LIMIT)
        {
            waitedFrames++;
            yield return null;
        }
    }

    private bool CanRequestUpgradeReward()
    {
        if (gameManager == null)
        {
            Debug.LogError("[UpgradeCardTestSceneController] Cannot request upgrade reward without GameManager.", this);
            return false;
        }

        if (gameManager.CurrentGameState != GameState.Game)
        {
            Debug.LogWarning(
                $"[UpgradeCardTestSceneController] Upgrade reward requests must go through GameManager while state is Game. Current state: {gameManager.CurrentGameState}.",
                this);
            return false;
        }

        return true;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            Debug.LogError($"[UpgradeCardTestSceneController] Missing private field '{fieldName}' on {target.GetType().Name}.");
            return;
        }

        field.SetValue(target, value);
    }
}
