using System;

public interface ICharacterSelectionService
{
    event Action<CharacterSelectionChangedArgs> SelectionChanged;

    CharacterSelectionSnapshot CreateSnapshot();
    bool SelectCharacter(int characterIndex);
}
