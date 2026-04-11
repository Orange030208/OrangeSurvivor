using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectionUIPage : UIPageBase
{
    [SerializeField] private UIWeaponSelectionContainer[] weaponContainers;
    [SerializeField] private Button startButton;

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        GameEventBus.Subscribe<SelectableWeaponsSnapshotEvent>(OnSelectableWeaponsChanged);

        startButton.onClick.AddListener(() =>
        {
            GameEventBus.Publish(new SelectedWeaponConfirmEvent());
        });

        GameEventBus.Publish<UISelectableWeaponsSnapshotEvent>();

        InitWeaponContainersClickAction();
    }

    protected override void OnPageClosed()
    {
        GameEventBus.Unsubscribe<SelectableWeaponsSnapshotEvent>(OnSelectableWeaponsChanged);
        startButton.onClick.RemoveAllListeners();
    }

    private void OnSelectableWeaponsChanged(SelectableWeaponsSnapshotEvent e)
    {
        int count = Mathf.Min(weaponContainers.Length, e.SelectableWeapons.Length);
        for (int i = 0; i < count; i++)
        {
            weaponContainers[i].Configure(
                new InfoAddIndex<WeaponLevelEntry>(e.SelectableWeapons[i], i)
            );
        }
    }

    private void InitWeaponContainersClickAction()
    {
        for (int i = 0; i < weaponContainers.Length; i++)
        {
            int temp = i;
            weaponContainers[i].OnClicked += (_) =>
            {
                GameEventBus.Publish(new SelectWeaponEvent(temp));
                weaponContainers[temp].Select();
                for (int j = 0; j < weaponContainers.Length; ++j)
                {
                    if (j == temp) continue;
                    weaponContainers[j].Unselect();
                }
            };
        }
    }
}