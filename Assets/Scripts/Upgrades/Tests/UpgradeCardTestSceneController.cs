using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public class UpgradeCardTestSceneController : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private Player playerPrefab;
    [SerializeField] private CharacterDataSO testCharacterData;
    [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;
    [SerializeField] private int initialUpgradePoints = 3;
    [SerializeField] private int testWaveNumber = 3;
    [SerializeField] private int initialGold = 60;

    private Player player;

    private System.Collections.IEnumerator Start()
    {
        ValidateConfiguration();
        EnsurePlayer();
        yield return null;
        PublishPlayerReady();
        GrantUpgradePoints();
        OpenUpgradePage();
    }

    [NaughtyAttributes.Button]
    public void RestartUpgradeTest()
    {
        GrantUpgradePoints();
        OpenUpgradePage();
    }

    private void EnsurePlayer()
    {
        Player prefab = playerPrefab != null ? playerPrefab : ResourcesManager.GetDefaultPlayerPrefab();
        player = Instantiate(prefab, playerSpawnPosition, Quaternion.identity);
        ConfigurePlayerForUpgradeTest(player);
    }

    private void ConfigurePlayerForUpgradeTest(Player targetPlayer)
    {
        System.Type type = typeof(Player);
        System.Reflection.FieldInfo field = type.GetField(
            "characterData",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.SetValue(targetPlayer, testCharacterData);
    }

    private void ValidateConfiguration()
    {
        if (uiManager == null)
        {
            throw new MissingReferenceException($"{nameof(UpgradeCardTestSceneController)} requires an explicit {nameof(UIManager)} reference.");
        }
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

    private void OpenUpgradePage()
    {
        GameEventBus.Publish(new WaveCompletedEvent(
            Mathf.Max(1, testWaveNumber),
            testWaveNumber + 1,
            0f,
            true));
        GameEventBus.Publish(new GameStateChangedEvent(GameState.Game, GameState.WaveTransition));
        ResetToUpgradePageAsync().Forget();
    }

    private async UniTask ResetToUpgradePageAsync()
    {
        if (uiManager == null)
        {
            Debug.LogError($"{nameof(UpgradeCardTestSceneController)} requires a {nameof(UIManager)} before opening the upgrade page.", this);
            return;
        }

        try
        {
            await uiManager.ResetToPageAsync<WaveTransitionUIPage>(cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        catch (System.OperationCanceledException)
        {
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }
}
