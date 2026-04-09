using UnityEngine;

public class GameStateUIController : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private bool openMenuOnStart = true;

    private void Awake()
    {
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Subscribe<PauseStateChangedEvent>(OnPauseStateChanged);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Unsubscribe<PauseStateChangedEvent>(OnPauseStateChanged);
    }

    private void Start()
    {
        if (!openMenuOnStart)
        {
            return;
        }

        ShowMenuStateUI();
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        if (uiManager == null)
        {
            return;
        }

        switch (eventData.NewState)
        {
            case GameState.Menu:
                ShowMenuStateUI();
                break;
            case GameState.WeaponSelection:
                ShowWeaponSelectionUI();
                break;
            case GameState.Game:
                ShowGameStateUI();
                break;
            case GameState.WaveTransition:
                ShowWaveTransitionStateUI();
                break;
            case GameState.Shop:
                ShowShopStateUI();
                break;
        }
    }

    private void OnPauseStateChanged(PauseStateChangedEvent eventData)
    {
        if (uiManager == null)
        {
            return;
        }

        if (eventData.IsPaused)
        {
            if (!uiManager.IsPageOpen<GamePauseMenu>())
            {
                uiManager.OpenPage<GamePauseMenu>();
            }

            return;
        }

        uiManager.ClosePage<GamePauseMenu>();
    }

    private void ShowMenuStateUI()
    {
        uiManager.CloseAllPages();
        uiManager.OpenPage<MenuUIPage>();
    }

    private void ShowWeaponSelectionUI()
    {
        uiManager.CloseTopPage();
        uiManager.OpenPage<WeaponSelectionUIPage>();
    }

    private void ShowGameStateUI()
    {
        uiManager.CloseAllPages();
        uiManager.OpenPage<GamingUIPage>();
    }

    private void ShowWaveTransitionStateUI()
    {
        uiManager.CloseAllPages();
        uiManager.OpenPage<WaveTransitionUIPage>();
    }

    private void ShowShopStateUI()
    {
        uiManager.CloseAllPages();
        uiManager.OpenPage<ShopUIPage>();
    }
}
