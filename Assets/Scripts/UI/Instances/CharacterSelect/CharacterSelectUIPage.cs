using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public class CharacterSelectUIPage : PageBase
{
    [SerializeField] private CharacterInfoCard characterInfoCard;
    [SerializeField] private CharacterListUI characterList;
    [SerializeField] private UIClickTarget confirm;
    [SerializeField] private UIClickTarget back;

    private CharacterSelectionManager characterSelectionManager;
    private int selectedCharacterIndex = -1;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        characterSelectionManager = context.GetPayload<CharacterSelectionManager>()
            ?? throw new System.InvalidOperationException($"{nameof(CharacterSelectUIPage)} requires {nameof(CharacterSelectionManager)} payload.");
        characterSelectionManager.SelectionChanged += OnCharacterSelectionChanged;

        confirm.OnClicked += OnConfirmOnClicked;
        back.OnClicked += OnBackOnClicked;

        SetConfirmButtonInteractable(false);
        characterInfoCard.ClearInfo();
        ApplyCharacterSelectionSnapshot(characterSelectionManager.CreateSnapshot());

        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        if (characterSelectionManager != null)
        {
            characterSelectionManager.SelectionChanged -= OnCharacterSelectionChanged;
            characterSelectionManager = null;
        }

        confirm.OnClicked -= OnConfirmOnClicked;
        back.OnClicked -= OnBackOnClicked;

        SetConfirmButtonInteractable(false);
        characterInfoCard.ClearInfo();
        characterList.Clear();
        selectedCharacterIndex = -1;
    }

    private void ApplyCharacterSelectionSnapshot(CharacterSelectionSnapshot snapshot)
    {
        selectedCharacterIndex = snapshot.SelectedIndex;
        characterList.Render(snapshot.Characters, selectedCharacterIndex, OnCharacterSelected);
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
        characterList.SetSelectedIndex(selectedCharacterIndex);
        characterInfoCard.DisplayInfo(args.CharacterData);
        SetConfirmButtonInteractable(true);
    }

    private void OnCharacterSelected(int characterIndex)
    {
        characterSelectionManager?.SelectCharacter(characterIndex);
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

    private void ValidateConfiguration()
    {
        if (characterInfoCard == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterSelectUIPage)} '{name}' is missing character info card.");
        }

        if (characterList == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterSelectUIPage)} '{name}' is missing character list.");
        }

        if (confirm == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterSelectUIPage)} '{name}' is missing confirm button.");
        }

        if (back == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterSelectUIPage)} '{name}' is missing back button.");
        }
    }
}
