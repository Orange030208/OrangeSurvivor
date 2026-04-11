using System.Collections.Generic;
using UnityEngine;

public class CharacterListController : MonoBehaviour
{
    [SerializeField] private CharacterButton characterButtonPrefab;
    [SerializeField] private Transform characterButtonParent;

    private readonly List<CharacterButton> spawnedButtons = new();
    private int selectedIndex = -1;

    public void Render(CharacterDataSO[] characters, int currentSelectedIndex)
    {
        int characterCount = characters == null ? 0 : characters.Length;
        RebuildButtons(characterCount);
        selectedIndex = currentSelectedIndex;

        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            CharacterButton button = spawnedButtons[i];
            CharacterDataSO characterData = characters[i];
            int index = i;

            button.Configure(characterData.CharacterIcon, index == selectedIndex);
            button.OnClick += () => GameEventBus.Publish(new CharacterItemClickedEvent(index));
        }
    }

    public void SetSelectedIndex(int newSelectedIndex)
    {
        selectedIndex = newSelectedIndex;

        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            spawnedButtons[i].SetSelected(i == selectedIndex);
        }
    }

    public void Clear()
    {
        characterButtonParent.Clear();
        spawnedButtons.Clear();
        selectedIndex = -1;
    }

    private void RebuildButtons(int characterCount)
    {
        Clear();

        if (characterCount <= 0 || characterButtonPrefab == null || characterButtonParent == null)
        {
            return;
        }

        for (int i = 0; i < characterCount; i++)
        {
            CharacterButton button = Instantiate(characterButtonPrefab, characterButtonParent);
            spawnedButtons.Add(button);
        }
    }
}
