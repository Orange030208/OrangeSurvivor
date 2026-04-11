using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoCard : MonoBehaviour
{
    [SerializeField] private CharacterExtraInfoDisplayer characterExtraInfoDisplayer;
    [SerializeField] private Image characterIconImage;
    [SerializeField] private TextMeshProUGUI characterNameText;

    public void DisplayInfo(CharacterDataSO characterData)
    {
        characterIconImage.sprite = characterData.CharacterIcon;
        characterNameText.text = characterData.CharacterName;
        characterExtraInfoDisplayer.DisplayInfo(characterData);
    }
}