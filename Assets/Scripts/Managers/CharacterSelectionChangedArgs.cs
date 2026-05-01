public readonly struct CharacterSelectionChangedArgs
{
    public int CharacterIndex { get; }
    public CharacterDataSO CharacterData { get; }

    public CharacterSelectionChangedArgs(int characterIndex, CharacterDataSO characterData)
    {
        CharacterIndex = characterIndex;
        CharacterData = characterData;
    }
}
