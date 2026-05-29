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
}
