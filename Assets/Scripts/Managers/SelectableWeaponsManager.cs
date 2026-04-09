using UnityEngine;
using Random = UnityEngine.Random;

public class SelectableWeaponsManager : MonoSingletonBase<SelectableWeaponsManager>
{
    [SerializeField] private WeaponsHolder weaponsHolder;

    public WeaponInfo[] SelectableWeapons { get; private set; }

    private int selectIndex = -1;

    private void OnEnable()
    {
        GameEventBus.Subscribe<UISelectableWeaponsSnapshotEvent>(PublishSnapshot);
        GameEventBus.Subscribe<SelectWeaponEvent>(OnWeaponSelected);
        GameEventBus.Subscribe<SelectedWeaponConfirmEvent>(OnSelectedWeaponConfirm);
        GameEventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<UISelectableWeaponsSnapshotEvent>(PublishSnapshot);
        GameEventBus.Unsubscribe<SelectWeaponEvent>(OnWeaponSelected);
        GameEventBus.Unsubscribe<SelectedWeaponConfirmEvent>(OnSelectedWeaponConfirm);
        GameEventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        switch (eventData.NewState)
        {
            case GameState.Game:
                weaponsHolder.AddWeapon(SelectableWeapons[selectIndex].weaponData, SelectableWeapons[selectIndex].level);
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
        SelectableWeapons = new WeaponInfo[selectionCount];
        for (int i = 0; i < selectionCount; i++)
        {
            WeaponDataSO weaponData = ResourcesManager.GetRandomWeapon();

            SelectableWeapons[i].weaponData = weaponData;
            SelectableWeapons[i].level = WeaponLevelHelper.GetRandomLevelInclusiveMax();
        }

        PublishSnapshot();
    }

    private void OnWeaponSelected(SelectWeaponEvent e)
    {
        if (SelectableWeapons == null) return;
        if (e.Index < 0 || e.Index >= SelectableWeapons.Length) return;

        selectIndex = e.Index;
    }

    private void OnSelectedWeaponConfirm(SelectedWeaponConfirmEvent e)
    {
        if (selectIndex >= 0 && selectIndex < SelectableWeapons.Length)
        {
            print($"选择了武器{SelectableWeapons[selectIndex].weaponData.ItemName}");
            GameEventBus.Publish(new GameStateChangeRequestEvent(GameState.Game));
        }
        else
        {
            Debug.LogError($"非法的武器下标{selectIndex}");
        }
    }

    private void PublishSnapshot()
    {
        if (SelectableWeapons == null) return;
        GameEventBus.Publish(new SelectableWeaponsSnapshotEvent(SelectableWeapons));
    }
}

public struct WeaponInfo
{
    public WeaponDataSO weaponData;
    public int level;

    public WeaponInfo(WeaponDataSO weaponData, int level)
    {
        this.weaponData = weaponData;
        this.level = level;
    }
}
