using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoCard : MonoBehaviour
{
    [SerializeField] private DescriptionListDisplayer descriptionListDisplayer;
    [SerializeField] private Image characterIconImage;
    [SerializeField] private TextMeshProUGUI characterNameText;

    public void DisplayInfo(CharacterDataSO characterData)
    {
        characterIconImage.sprite = characterData.CharacterIcon;
        characterNameText.text = characterData.CharacterName;
        descriptionListDisplayer.DisplaySource(characterData);
    }

    public void ClearInfo()
    {
        characterIconImage.sprite = null;
        characterNameText.text = string.Empty;
        descriptionListDisplayer.DisplayDescriptions(null);
    }
}
