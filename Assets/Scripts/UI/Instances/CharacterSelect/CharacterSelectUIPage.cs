using AXR.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUIPage : UIPageBase
{
    [SerializeField] private CharacterInfoCard characterInfoCard;
    [SerializeField] private CharacterListController characterListController;
    [SerializeField] private UIClickTarget confirm;
    [SerializeField] private UIClickTarget back;
    [SerializeField] private CharacterSelectionManager characterSelectionManager;

    private ICharacterSelectionService selectionService;
    private int selectedCharacterIndex = -1;

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        selectionService = ResolveSelectionService();
        if (selectionService != null)
        {
            selectionService.SelectionChanged += OnCharacterSelectionChanged;
        }

        confirm.OnClicked += OnConfirmOnClicked;
        back.OnClicked += OnBackOnClicked;

        SetConfirmButtonInteractable(false);
        characterInfoCard.ClearInfo();
        if (selectionService != null)
        {
            ApplyCharacterSelectionSnapshot(selectionService.CreateSnapshot());
        }
    }

    protected override void OnPageClosed()
    {
        if (selectionService != null)
        {
            selectionService.SelectionChanged -= OnCharacterSelectionChanged;
            selectionService = null;
        }

        confirm.OnClicked -= OnConfirmOnClicked;
        back.OnClicked -= OnBackOnClicked;

        SetConfirmButtonInteractable(false);
        characterInfoCard.ClearInfo();
        characterListController.Clear();
        selectedCharacterIndex = -1;
    }

    private void ApplyCharacterSelectionSnapshot(CharacterSelectionSnapshot snapshot)
    {
        selectedCharacterIndex = snapshot.SelectedIndex;
        characterListController.Render(snapshot.Characters, selectedCharacterIndex, OnCharacterSelected);
        SetConfirmButtonInteractable(selectedCharacterIndex >= 0);

        if (selectedCharacterIndex < 0 || snapshot.Characters == null || selectedCharacterIndex >= snapshot.Characters.Length)
        {
            characterInfoCard.ClearInfo();
            return;
        }

        characterInfoCard.DisplayInfo(snapshot.Characters[selectedCharacterIndex]);
    }

    private void OnCharacterSelectionChanged(CharacterSelectionChangedArgs args)
    {
        selectedCharacterIndex = args.CharacterIndex;
        characterListController.SetSelectedIndex(selectedCharacterIndex);
        characterInfoCard.DisplayInfo(args.CharacterData);
        SetConfirmButtonInteractable(true);
    }

    private void OnCharacterSelected(int characterIndex)
    {
        selectionService?.SelectCharacter(characterIndex);
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

    private ICharacterSelectionService ResolveSelectionService()
    {
        if (characterSelectionManager != null)
        {
            return characterSelectionManager;
        }

        characterSelectionManager = CharacterSelectionManager.Instance;
        return characterSelectionManager;
    }
}
