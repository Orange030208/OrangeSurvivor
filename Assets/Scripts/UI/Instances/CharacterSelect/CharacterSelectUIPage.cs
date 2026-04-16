using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUIPage : UIPageBase
{
    [SerializeField] private CharacterInfoCard characterInfoCard;
    [SerializeField] private CharacterListController characterListController;
    [SerializeField] private UIClickTarget confirm;
    [SerializeField] private UIClickTarget back;

    private int selectedCharacterIndex = -1;

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        GameEventBus.Subscribe<CharacterSelectionSnapshotEvent>(OnCharacterSelectionSnapshot);
        GameEventBus.Subscribe<CharacterSelectionChangedEvent>(OnCharacterSelectionChanged);
        confirm.OnClicked += OnConfirmOnClicked;
        back.OnClicked += OnBackOnClicked;

        SetConfirmButtonInteractable(false);
        characterInfoCard.ClearInfo();
        GameEventBus.Publish<RequestCharacterSelectionSnapshotEvent>();
    }

    protected override void OnPageClosed()
    {
        GameEventBus.Unsubscribe<CharacterSelectionSnapshotEvent>(OnCharacterSelectionSnapshot);
        GameEventBus.Unsubscribe<CharacterSelectionChangedEvent>(OnCharacterSelectionChanged);
        confirm.OnClicked -= OnConfirmOnClicked;
        back.OnClicked -= OnBackOnClicked;

        SetConfirmButtonInteractable(false);
        characterInfoCard.ClearInfo();
        characterListController.Clear();
        selectedCharacterIndex = -1;
    }

    private void OnCharacterSelectionSnapshot(CharacterSelectionSnapshotEvent eventData)
    {
        selectedCharacterIndex = eventData.SelectedIndex;
        characterListController.Render(eventData.Characters, selectedCharacterIndex);
        SetConfirmButtonInteractable(selectedCharacterIndex >= 0);

        if (selectedCharacterIndex < 0 || eventData.Characters == null || selectedCharacterIndex >= eventData.Characters.Length)
        {
            characterInfoCard.ClearInfo();
            return;
        }

        characterInfoCard.DisplayInfo(eventData.Characters[selectedCharacterIndex]);
    }

    private void OnCharacterSelectionChanged(CharacterSelectionChangedEvent eventData)
    {
        selectedCharacterIndex = eventData.CharacterIndex;
        characterListController.SetSelectedIndex(selectedCharacterIndex);
        characterInfoCard.DisplayInfo(eventData.CharacterData);
        SetConfirmButtonInteractable(true);
    }

    private void OnConfirmOnClicked()
    {
        if (selectedCharacterIndex < 0)
        {
            return;
        }

        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish<CharacterSelectionCompletedEvent>();
    }

    private void OnBackOnClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish<CharacterSelectionBackClickedEvent>();

    }

    private void SetConfirmButtonInteractable(bool interactable)
    {
        confirm.Interactable = interactable;
    }
}
