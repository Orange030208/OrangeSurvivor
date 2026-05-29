using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class DescriptionDescriber : Describer
{
    [SerializeField] private TextMeshProUGUI infoText;

    public override void Display(InfoDocument document)
    {
        if (infoText == null)
        {
            return;
        }

        infoText.text = document != null
            ? InfoDocumentTextFormatter.ToPlainText(document, includeHeader: false)
            : string.Empty;
    }
}
