using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class DescriptionListDisplayer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI infoRichText;

    public void Display(IDescribable describable)
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
            infoRichText.text = "";
            return;
        }
        StringBuilder sb = new();
        foreach (DescriptorInfo descriptorInfo in descriptorInfos)
        {
            sb.AppendLine(descriptorInfo.value);
        }
        infoRichText.text = sb.ToString();
    }
}
