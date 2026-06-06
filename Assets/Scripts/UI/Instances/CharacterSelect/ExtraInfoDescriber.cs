using TMPro;
using UnityEngine;

public class ExtraInfoDescriber :Describer
{
    [SerializeField] private TextMeshProUGUI infoText;

    public override void Display(InfoDocument document)
    {
        if (infoText == null)
        {
            return;
        }

        infoText.text = document != null
            ? InfoDocumentTextFormatter.ToRichText(document)
            : string.Empty;
    }

    public void DisplayText(string text)
    {
        if (infoText == null)
        {
            return;
        }

        infoText.text = text ?? string.Empty;
    }
}
