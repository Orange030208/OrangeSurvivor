using UniversalUI.Core.Runtime;
using UnityEngine;
using UniversalUI.Instances;

namespace UniversalUI.Integration.Game
{
    public sealed class GameStateUIController : MonoBehaviour, IGameStateListener
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

        private void Start()
        {
            if (!openMenuOnStart)
            {
                return;
            }

            ShowMenuStateUI();
        }

        public void BeforeGameStateChanged(GameState oldState, GameState newState)
        {
        }

        public void AfterGameStateChanged(GameState oldState, GameState newState)
        {
            if (uiManager == null)
            {
                return;
            }

            switch (newState)
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

        private void ShowMenuStateUI()
        {
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
            uiManager.OpenPage<ShopUIPage>();
        }
    }
}
