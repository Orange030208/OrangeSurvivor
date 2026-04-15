using System.Collections.Generic;
using UnityEngine;

public class CharacterListController : UIScrollListBase<CharacterButton, CharacterDataSO>
{
    private int selectedIndex = -1;

    protected override void Awake()
    {
        base.Awake();
        ClearItemsImmediate();
    }

    public void Render(CharacterDataSO[] characters, int currentSelectedIndex)
    {
        selectedIndex = currentSelectedIndex;
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
        base.Clear();
    }

    protected override void BindItem(CharacterButton item, CharacterDataSO data, int index)
    {
        item.Configure(data.CharacterIcon, index == selectedIndex, () =>
        {
            GameEventBus.Publish(new CharacterItemClickedEvent(index));
        });
    }
}
