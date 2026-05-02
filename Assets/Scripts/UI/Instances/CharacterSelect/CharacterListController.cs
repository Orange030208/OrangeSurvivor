using AXR.Framework.UI;
using System.Collections.Generic;
using UnityEngine;

public class CharacterListController : UIScrollListBase<CharacterButton, CharacterDataSO>
{
    private int selectedIndex = -1;
    private System.Action<int> characterSelected;

    protected override void Awake()
    {
        base.Awake();
        ClearItemsImmediate();
    }

    protected override bool ShouldPlayRevealOnRefresh()
    {
        return false;
    }

    public void Render(CharacterDataSO[] characters, int currentSelectedIndex, System.Action<int> onCharacterSelected)
    {
        selectedIndex = currentSelectedIndex;
        characterSelected = onCharacterSelected;
        Render((IReadOnlyList<CharacterDataSO>)characters);
    }

    public void SetSelectedIndex(int newSelectedIndex)
    {
        if (selectedIndex == newSelectedIndex)
        {
            return;
        }

        selectedIndex = newSelectedIndex;

        IReadOnlyList<CharacterButton> buttons = ActiveItems;
        for (int i = 0; i < buttons.Count; i++)
        {
            CharacterButton button = buttons[i];
            if (button == null || !button.gameObject.activeSelf)
            {
                continue;
            }

            button.SetSelected(i == selectedIndex);
        }
    }

    public new void Clear()
    {
        selectedIndex = -1;
        characterSelected = null;
        base.Clear();
    }

    protected override void BindItem(CharacterButton item, CharacterDataSO data, int index)
    {
        item.Configure(data.CharacterIcon, () =>
        {
            characterSelected?.Invoke(index);
        });

        item.SetSelected(index == selectedIndex);
    }
}
