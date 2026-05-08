public readonly struct CharacterSelectionViewData
{
    public CharacterDataSO[] Characters { get; }
    public int SelectedIndex { get; }

    public CharacterSelectionViewData(CharacterDataSO[] characters, int selectedIndex)
    {
        Characters = characters;
        SelectedIndex = selectedIndex;
    }
}
