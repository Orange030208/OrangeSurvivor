using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExtraInfoDescriber :Describer
{
    [SerializeField] private TextMeshProUGUI infoText;

    public override void Display(IDescribable describable)
    {
        if (describable == null)
        {
            Display((IEnumerable<DescriptorInfo>)null);
            return;
        }

        Display(describable.GetExtraInfos());
    }

    private void Display(IEnumerable<DescriptorInfo> descriptorInfos)
    {
        if (descriptorInfos == null)
        {
            infoText.text = "";
            return;
        }

        StringBuilder sb = new();
        foreach (DescriptorInfo descriptorInfo in descriptorInfos)
        {
            sb.AppendLine(descriptorInfo.value);
        }

        infoText.text = sb.ToString();
    }
}
