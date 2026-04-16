using UnityEngine;

public struct RequestCharacterSelectionSnapshotEvent : IGameEvent
{
}

public struct CharacterSelectionSnapshotEvent : IGameEvent
{
    public CharacterDataSO[] Characters;
    public int SelectedIndex;

    public CharacterSelectionSnapshotEvent(CharacterDataSO[] characters, int selectedIndex)
    {
        Characters = characters;
        SelectedIndex = selectedIndex;
    }
}

public struct CharacterItemClickedEvent : IGameEvent
{
    public int CharacterIndex;

    public CharacterItemClickedEvent(int characterIndex)
    {
        CharacterIndex = characterIndex;
    }
}

public struct CharacterSelectionChangedEvent : IGameEvent
{
    public int CharacterIndex;
    public CharacterDataSO CharacterData;

    public CharacterSelectionChangedEvent(int characterIndex, CharacterDataSO characterData)
    {
        CharacterIndex = characterIndex;
        CharacterData = characterData;
    }
}

public struct CharacterSelectionCompletedEvent : IGameEvent
{
}

public struct CharacterSelectionBackClickedEvent : IGameEvent
{
}
