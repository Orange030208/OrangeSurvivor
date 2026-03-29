using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoSingletonBase<GameManager>
{
    [SerializeField] private Player _player;
    [SerializeField] private GameState initialGameState = GameState.Menu;

    private GameState _gameState;
    private GameState GameState
    {
        get => _gameState;
        set
        {
            if (value == _gameState) return;
            IEnumerable<IGameStateListener> stateListeners = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IGameStateListener>();
            GameState oldState = _gameState;
            foreach (IGameStateListener listener in stateListeners)
            {
                listener.BeforeGameStateChanged(oldState, value);
            }
            _gameState = value;
            foreach (IGameStateListener listener in stateListeners)
            {
                listener.AfterGameStateChanged(oldState, value);
            }

            if (value == GameState.GameOver)
            {
                ManageGameOver();
            }
        }
    }

    public void WeaponSelection() => GameState = GameState.WeaponSelection;
    public void StartGame() => GameState = GameState.Game;
    public void GameOver() => GameState = GameState.GameOver;
    public void EnterMenu() => GameState = GameState.Menu;
    public void EnterShop() => GameState = GameState.Shop;
    public void EnterWaveTransition() => GameState = GameState.WaveTransition;
    
    private void OnEnable()
    {
        WaveManager.OnWaveComplete += OnWaveCompleted;
    }

    private void OnDisable()
    {
        WaveManager.OnWaveComplete -= OnWaveCompleted;
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        GameState = initialGameState;
    }

    private void OnWaveCompleted(int waveNumber)
    {
        if (_player.IsLevelUpInCurrentWave)
        {
            GameState = GameState.WaveTransition;
        }
        else
        {
            GameState = GameState.Shop;
        }
        print(_player.LevelUpValue);
    }

    public void ManageGameOver()
    {
        DOVirtual.DelayedCall(2, () =>
        {
            SceneManager.LoadScene(0);
        });
    }
}

public enum GameState
{
    None,
    Menu,
    WeaponSelection,
    Game,
    GameOver,
    StageComplete,
    WaveTransition,
    Shop
}

public interface IGameStateListener
{
    void BeforeGameStateChanged(GameState oldState, GameState newState);
    void AfterGameStateChanged(GameState oldState, GameState newState);
}
