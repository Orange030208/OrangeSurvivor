using System;
using UnityEngine;

public class CharacterSelectionManager : MonoSingletonBase<CharacterSelectionManager>, ICharacterSelectionService
{
    private CharacterDataSO[] characters = Array.Empty<CharacterDataSO>();
    private int selectedIndex = -1;

    public event Action<CharacterSelectionChangedArgs> SelectionChanged;

    public int SelectedIndex => selectedIndex;
    public CharacterDataSO SelectedCharacter =>
        selectedIndex >= 0 && selectedIndex < characters.Length ? characters[selectedIndex] : null;

    private void OnEnable()
    {
        RefreshCharacters();
    }

    private void RefreshCharacters()
    {
        characters = ResourcesManager.GetAllCharacters();
        selectedIndex = -1;
    }

    public bool SelectCharacter(int characterIndex)
    {
        if (characters.Length == 0)
        {
            return false;
        }

        if (characterIndex < 0 || characterIndex >= characters.Length)
        {
            return false;
        }

        if (selectedIndex == characterIndex)
        {
            return true;
        }

        selectedIndex = characterIndex;
        SelectionChanged?.Invoke(new CharacterSelectionChangedArgs(selectedIndex, characters[selectedIndex]));
        return true;
    }

    public CharacterSelectionSnapshot CreateSnapshot()
    {
        if (characters == null || characters.Length == 0)
        {
            RefreshCharacters();
        }

        return new CharacterSelectionSnapshot(characters, selectedIndex);
    }
}
