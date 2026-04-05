using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropContainer : MonoBehaviour
{
    [SerializeField] private Image propImage;
    [SerializeField] private TextMeshProUGUI propText;
    [SerializeField] private TextMeshProUGUI propValueText;

    public void Configure(Sprite icon,string propName,string propValue)
    {
        propImage.sprite = icon;
        propText.text = propName;
        propValueText.text = propValue;
    }
}