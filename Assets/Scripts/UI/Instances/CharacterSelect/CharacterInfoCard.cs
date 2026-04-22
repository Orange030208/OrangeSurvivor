using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoCard : MonoBehaviour
{
    [SerializeField] private ExtraInfoDescriber extraInfoDescriber;
    [SerializeField] private Image characterIconImage;
    [SerializeField] private TextMeshProUGUI characterNameText;

    public void DisplayInfo(CharacterDataSO characterData)
    {
        characterIconImage.sprite = characterData.CharacterIcon;
        characterNameText.text = characterData.CharacterName;
        extraInfoDescriber.Display(characterData);
    }

    public void ClearInfo()
    {
        characterIconImage.sprite = null;
        characterNameText.text = string.Empty;
        extraInfoDescriber.Display((IDescribable)null);
    }
}
