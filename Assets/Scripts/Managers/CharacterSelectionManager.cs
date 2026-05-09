using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSelectionManager : MonoSingletonBase<CharacterSelectionManager>
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
        IReadOnlyList<CharacterDataSO> configuredCharacters = GameContentRuntime.Provider.Characters;
        characters = ToArray(configuredCharacters);
        selectedIndex = -1;
    }

    private static CharacterDataSO[] ToArray(IReadOnlyList<CharacterDataSO> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<CharacterDataSO>();
        }

        CharacterDataSO[] result = new CharacterDataSO[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            result[i] = source[i];
        }

        return result;
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

    public CharacterSelectionViewData CreateViewData()
    {
        if (characters == null || characters.Length == 0)
        {
            RefreshCharacters();
        }

        return new CharacterSelectionViewData(characters, selectedIndex);
    }
}
