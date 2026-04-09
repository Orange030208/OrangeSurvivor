using UnityEngine;
using Random = UnityEngine.Random;

public class SelectableWeaponsManager : MonoSingletonBase<SelectableWeaponsManager>, IGameStateListener
{
    [SerializeField] private WeaponsHolder weaponsHolder;

    public WeaponInfo[] SelectableWeapons { get; private set; }

    private int selectIndex = -1;
    
    private void OnEnable()
    {
        GameEventBus.Subscribe<UISelectableWeaponsSnapshotEvent>(PublishSnapshot);
        GameEventBus.Subscribe<SelectWeaponEvent>(OnWeaponSelected);
        GameEventBus.Subscribe<SelectedWeaponConfirmEvent>(OnSelectedWeaponConfirm);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<UISelectableWeaponsSnapshotEvent>(PublishSnapshot);
        GameEventBus.Unsubscribe<SelectWeaponEvent>(OnWeaponSelected);
        GameEventBus.Unsubscribe<SelectedWeaponConfirmEvent>(OnSelectedWeaponConfirm);
    }

    public void BeforeGameStateChanged(GameState oldState, GameState newState)
    {
    }

    public void AfterGameStateChanged(GameState oldState, GameState newState)
    {
        switch (newState)
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

        selectIndex  = e.Index;
    }

    private void OnSelectedWeaponConfirm(SelectedWeaponConfirmEvent e)
    {
        if (selectIndex >= 0 && selectIndex < SelectableWeapons.Length)
        {
            print($"选择了武器{SelectableWeapons[selectIndex].weaponData.ItemName}");
            GameManager.Instance.StartGame();
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
