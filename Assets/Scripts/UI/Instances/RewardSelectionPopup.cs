using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;

public class RewardSelectionPopup : PopupBase
{
    [Header("内容")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private RewardSelectionCardGroup cardGroup;

    protected override void Awake()
    {
        base.Awake();
        ResolveViewParts();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        RewardSelectionPopupModel model = context.GetPayload<RewardSelectionPopupModel>();
        if (model == null)
        {
            throw new System.ArgumentException($"{nameof(RewardSelectionPopup)} requires {nameof(RewardSelectionPopupModel)} payload.");
        }

        Configure(model);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        cardGroup?.Clear();
    }

    public void Configure(RewardSelectionPopupModel model)
    {
        ResolveViewParts();
        ValidateConfiguration();

        if (titleText != null)
        {
            titleText.text = model.Title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = model.Description;
            descriptionText.gameObject.SetActive(!string.IsNullOrWhiteSpace(model.Description));
        }

        cardGroup.Configure(model.RequestId, model.Options);
        cardGroup.SetVisible(true);
    }

    public async UniTask RefreshAsync(RewardSelectionPopupModel model, CancellationToken cancellationToken)
    {
        ResolveViewParts();
        ValidateConfiguration();
        await cardGroup.PlayRefreshOutAsync(cancellationToken);
        Configure(model);
    }

    private void ResolveViewParts()
    {
        if (cardGroup == null)
        {
            cardGroup = GetComponentInChildren<RewardSelectionCardGroup>(true);
        }

        if (titleText == null)
        {
            ResolveTitleText();
        }
    }

    private void ResolveTitleText()
    {
        TextMeshProUGUI[] textComponents = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int textIndex = 0; textIndex < textComponents.Length; textIndex++)
        {
            TextMeshProUGUI textComponent = textComponents[textIndex];
            if (textComponent == null)
            {
                continue;
            }

            if (cardGroup != null && textComponent.transform.IsChildOf(cardGroup.transform))
            {
                continue;
            }

            titleText = textComponent;
            return;
        }
    }

    private void ValidateConfiguration()
    {
        if (cardGroup == null)
        {
            throw new MissingReferenceException($"{nameof(RewardSelectionPopup)} '{name}' is missing card group.");
        }
    }
}
