using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WeaponSelectionManager : MonoSingletonBase<WeaponSelectionManager>, IGameStateListener
{
    [SerializeField] private WeaponDataSO[] weaponDataPool;
    [SerializeField] private Player player;
    [SerializeField] private PlayerWeapons playerWeapons;

    public SelectionWeapon[] SelectionWeapons { get; private set; }

    private WeaponDataSO _currentWeaponData;
    private int _currentWeaponLevel;

    private void OnEnable()
    {
        GameEventBus.Subscribe<RequestWeaponSelectionSnapshotEvent>(PublishSnapshot);
        GameEventBus.Subscribe<WeaponSelectionOptionClickedEvent>(OnWeaponOptionClicked);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<RequestWeaponSelectionSnapshotEvent>(PublishSnapshot);
        GameEventBus.Unsubscribe<WeaponSelectionOptionClickedEvent>(OnWeaponOptionClicked);
    }

    public void BeforeGameStateChanged(GameState oldState, GameState newState)
    {
    }

    public void AfterGameStateChanged(GameState oldState, GameState newState)
    {
        switch (newState)
        {
            case GameState.Game:
                playerWeapons.AddWeapon(_currentWeaponData, _currentWeaponLevel);
                break;
            case GameState.WeaponSelection:
                ConfigureSelectionWeapons();
                break;
        }
    }

    [NaughtyAttributes.Button]
    private void ConfigureSelectionWeapons()
    {
        int selectionCount = 3;
        SelectionWeapons = new SelectionWeapon[selectionCount];
        for (int i = 0; i < selectionCount; i++)
        {
            int randomIndex = Random.Range(0, weaponDataPool.Length);
            WeaponDataSO weaponData = weaponDataPool[randomIndex];

            SelectionWeapons[i].weaponData = weaponData;
            SelectionWeapons[i].level = Random.Range(1, 7);
        }

        PublishSnapshot();
    }

    private void OnWeaponOptionClicked(WeaponSelectionOptionClickedEvent e)
    {
        if (SelectionWeapons == null) return;
        if (e.Index < 0 || e.Index >= SelectionWeapons.Length) return;

        SelectionWeapon selectedWeapon = SelectionWeapons[e.Index];
        SelectWeapon(selectedWeapon.weaponData, selectedWeapon.level);
    }

    private void SelectWeapon(WeaponDataSO weaponData, int level)
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
        }

        if (player == null || weaponData == null)
        {
            return;
        }

        print($"武器名称:{weaponData.Name}");

        _currentWeaponData = weaponData;
        _currentWeaponLevel = level;
    }

    private void PublishSnapshot()
    {
        if (SelectionWeapons == null) return;
        GameEventBus.Publish(new WeaponSelectionChangedEvent(SelectionWeapons));
    }
}

public struct SelectionWeapon
{
    public WeaponDataSO weaponData;
    public int level;
}
