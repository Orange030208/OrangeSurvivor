using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class WaveTransitionManager : MonoSingletonBase<WaveTransitionManager>,IGameStateListener
{
    public PropEnum[] PropEnums { private set; get; }
    
    public event Action<PropEnum[]> OnUpdatePropsChanged;
    
    public void BeforeGameStateChanged(GameState oldState, GameState newState)
    {
    }

    public void AfterGameStateChanged(GameState oldState, GameState newState)
    {
        switch (newState)
        {
            case GameState.WaveTransition:
                ConfigureUpgradeProps();
                break;
        }
    }

    private void ConfigureUpgradeProps()
    {
        PropEnums = new PropEnum[3];
        PropEnums[0] = PropEnum.Attack;
        PropEnums[1] = PropEnum.Armor;
        PropEnums[2] = PropEnum.AttackSpeed;
        OnUpdatePropsChanged?.Invoke(PropEnums);
    }
}