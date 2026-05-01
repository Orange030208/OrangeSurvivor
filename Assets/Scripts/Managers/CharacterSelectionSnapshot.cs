public readonly struct CharacterSelectionSnapshot
{
    public CharacterDataSO[] Characters { get; }
    public int SelectedIndex { get; }

    public CharacterSelectionSnapshot(CharacterDataSO[] characters, int selectedIndex)
    {
        Characters = characters;
        SelectedIndex = selectedIndex;
    }
}
