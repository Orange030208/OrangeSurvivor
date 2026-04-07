using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectionUIPage : UIPageBase
{
    [SerializeField] private WeaponSelectionContainer[] weaponContainers;
    private SelectionWeapon[] currentSelectionWeapons;
    [SerializeField] private Button startButton;

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        GameEventBus.Subscribe<WeaponSelectionChangedEvent>(OnSelectionWeaponsChanged);
        GameEventBus.Subscribe<WeaponSelectionContainerClickedEvent>(OnWeaponContainerClicked);

        startButton.onClick.AddListener(() =>
        {
            if (HasWeaponSelected) GameManager.Instance.StartGame();
        });

        GameEventBus.Publish<RequestWeaponSelectionSnapshotEvent>();
    }

    protected override void OnPageClosed()
    {
        GameEventBus.Unsubscribe<WeaponSelectionChangedEvent>(OnSelectionWeaponsChanged);
        GameEventBus.Unsubscribe<WeaponSelectionContainerClickedEvent>(OnWeaponContainerClicked);

        foreach (var container in weaponContainers)
        {
            container.Cleanup();
        }

        startButton.onClick.RemoveAllListeners();
    }

    private void OnSelectionWeaponsChanged(WeaponSelectionChangedEvent e)
    {
        currentSelectionWeapons = e.SelectionWeapons;
        if (currentSelectionWeapons == null) return;
        ApplySelectionWeapons(currentSelectionWeapons);
    }

    private void ApplySelectionWeapons(SelectionWeapon[] selectionWeapons)
    {
        int count = Mathf.Min(weaponContainers.Length, selectionWeapons.Length);
        for (int i = 0; i < count; i++)
        {
            weaponContainers[i].Configure(
                i,
                selectionWeapons[i].weaponData,
                selectionWeapons[i].level
            );
        }
    }

    private void OnWeaponContainerClicked(WeaponSelectionContainerClickedEvent e)
    {
        GameEventBus.Publish(new WeaponSelectionOptionClickedEvent(e.ContainerIndex));

        for (int i = 0; i < weaponContainers.Length; i++)
        {
            if (i == e.ContainerIndex)
                weaponContainers[i].Select();
            else
                weaponContainers[i].Deselect();
        }
    }

    private bool HasWeaponSelected
    {
        get
        {
            foreach (var container in weaponContainers)
            {
                if (container.isSelected)
                    return true;
            }

            return false;
        }
    }
}