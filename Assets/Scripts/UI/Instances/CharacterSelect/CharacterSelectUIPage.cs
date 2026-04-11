using UnityEngine;

public class CharacterSelectUIPage : UIPageBase
{
    [SerializeField] private CharacterInfoCard characterInfoCard;
    [SerializeField] private CharacterListController characterListController;

    private int selectedCharacterIndex = -1;

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        GameEventBus.Subscribe<CharacterSelectionSnapshotEvent>(OnCharacterSelectionSnapshot);
        GameEventBus.Subscribe<CharacterSelectionChangedEvent>(OnCharacterSelectionChanged);
        GameEventBus.Publish<RequestCharacterSelectionSnapshotEvent>();
    }

    protected override void OnPageClosed()
    {
        GameEventBus.Unsubscribe<CharacterSelectionSnapshotEvent>(OnCharacterSelectionSnapshot);
        GameEventBus.Unsubscribe<CharacterSelectionChangedEvent>(OnCharacterSelectionChanged);
        characterListController?.Clear();
        selectedCharacterIndex = -1;
    }

    private void OnCharacterSelectionSnapshot(CharacterSelectionSnapshotEvent eventData)
    {
        selectedCharacterIndex = eventData.SelectedIndex;
        characterListController?.Render(eventData.Characters, selectedCharacterIndex);

        if (selectedCharacterIndex < 0 || eventData.Characters == null || selectedCharacterIndex >= eventData.Characters.Length)
        {
            return;
        }

        characterInfoCard?.DisplayInfo(eventData.Characters[selectedCharacterIndex]);
    }

    private void OnCharacterSelectionChanged(CharacterSelectionChangedEvent eventData)
    {
        selectedCharacterIndex = eventData.CharacterIndex;
        characterListController?.SetSelectedIndex(selectedCharacterIndex);
        characterInfoCard?.DisplayInfo(eventData.CharacterData);
    }
}
