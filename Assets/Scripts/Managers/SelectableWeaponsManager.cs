using UnityEngine;
using Random = UnityEngine.Random;

public class SelectableWeaponsManager : MonoSingletonBase<SelectableWeaponsManager>
{
    [SerializeField] private WeaponsHolder weaponsHolder;

    public WeaponLevelEntry[] SelectableWeapons { get; private set; }

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
        if (eventData.NewState == GameState.WeaponSelection)
        {
            ConfigureSelectionWeapons();
        }
    }

    [NaughtyAttributes.Button]
    private void ConfigureSelectionWeapons()
    {
        int selectionCount = 3;
        SelectableWeapons = new WeaponLevelEntry[selectionCount];
        selectIndex = -1;

        for (int i = 0; i < selectionCount; i++)
        {
            WeaponDataSO weaponData = ResourcesManager.GetRandomWeapon();
            SelectableWeapons[i] = new WeaponLevelEntry(weaponData, WeaponLevelHelper.GetRandomLevelInclusiveMax());
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
        if (SelectableWeapons == null || selectIndex < 0 || selectIndex >= SelectableWeapons.Length)
        {
            Debug.LogError($"非法的武器下标{selectIndex}");
            return;
        }

        WeaponLevelEntry selectedWeapon = SelectableWeapons[selectIndex];
        if (selectedWeapon.weaponData == null)
        {
            Debug.LogError("选择的武器数据为空");
            return;
        }

        if (weaponsHolder == null)
        {
            Debug.LogError("WeaponsHolder 未绑定");
            return;
        }

        if (!weaponsHolder.AddWeapon(selectedWeapon.weaponData, selectedWeapon.level))
        {
            Debug.LogError($"添加武器失败: {selectedWeapon.weaponData.ItemName}");
            return;
        }

        print($"选择了武器{selectedWeapon.weaponData.ItemName}");
        GameEventBus.Publish<WeaponSelectionCompletedEvent>();
    }

    private void PublishSnapshot()
    {
        if (SelectableWeapons == null) return;
        GameEventBus.Publish(new SelectableWeaponsSnapshotEvent(SelectableWeapons));
    }
}
