using System;
using UnityEngine;

public class CharacterSelectionManager : MonoSingletonBase<CharacterSelectionManager>
{
    [SerializeField] private int defaultSelectedIndex;

    private CharacterDataSO[] characters = Array.Empty<CharacterDataSO>();
    private int selectedIndex = -1;

    private void OnEnable()
    {
        GameEventBus.Subscribe<RequestCharacterSelectionSnapshotEvent>(PublishSnapshot);
        GameEventBus.Subscribe<CharacterItemClickedEvent>(OnCharacterItemClicked);
        RefreshCharacters();
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<RequestCharacterSelectionSnapshotEvent>(PublishSnapshot);
        GameEventBus.Unsubscribe<CharacterItemClickedEvent>(OnCharacterItemClicked);
    }

    private void RefreshCharacters()
    {
        characters = ResourcesManager.GetAllCharacters();

        if (characters.Length == 0)
        {
            selectedIndex = -1;
            return;
        }

        selectedIndex = Mathf.Clamp(defaultSelectedIndex, 0, characters.Length - 1);
    }

    private void OnCharacterItemClicked(CharacterItemClickedEvent eventData)
    {
        if (characters.Length == 0)
        {
            return;
        }

        if (eventData.CharacterIndex < 0 || eventData.CharacterIndex >= characters.Length)
        {
            return;
        }

        if (selectedIndex == eventData.CharacterIndex)
        {
            return;
        }

        selectedIndex = eventData.CharacterIndex;
        GameEventBus.Publish(new CharacterSelectionChangedEvent(selectedIndex, characters[selectedIndex]));
    }

    private void PublishSnapshot()
    {
        if (characters == null || characters.Length == 0)
        {
            RefreshCharacters();
        }

        GameEventBus.Publish(new CharacterSelectionSnapshotEvent(characters, selectedIndex));
    }
}
