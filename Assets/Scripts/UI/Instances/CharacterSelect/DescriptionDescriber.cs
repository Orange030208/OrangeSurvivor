using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class DescriptionDescriber : Describer
{
    [SerializeField] private TextMeshProUGUI infoText;

    public override void Display(IDescribable describable)
    {
        if (describable == null)
        {
            Display(string.Empty);
            return;
        }

        Display(describable.Description);
    }

    private void Display(string description)
    {
        if (description == null)
        {
            infoText.text = "";
            return;
        }

        infoText.text = description;
    }
}