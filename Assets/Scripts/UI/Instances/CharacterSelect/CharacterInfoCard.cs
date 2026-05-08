using System.Collections.Generic;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoCard : ViewPartBase
{
    [SerializeField] private ExtraInfoDescriber extraInfoDescriber;
    [SerializeField] private Image characterIconImage;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Sprite defaultCharacterIcon;
    [SerializeField] private string defaultCharacterName = "请选择角色";
    [SerializeField] private string defaultExtraInfo = "选择一个角色查看详情";

    public void DisplayInfo(CharacterDataSO characterData)
    {
        if (characterData == null)
        {
            ClearInfo();
            return;
        }

        characterIconImage.sprite = characterData.CharacterIcon;
        characterNameText.text = characterData.CharacterName;
        extraInfoDescriber.Display(characterData);
    }

    public void ClearInfo()
    {
        characterIconImage.sprite = defaultCharacterIcon;
        characterNameText.text = defaultCharacterName;
        extraInfoDescriber.Display(new DefaultCharacterInfo(defaultCharacterName, defaultCharacterIcon, defaultExtraInfo));
    }

    private sealed class DefaultCharacterInfo : IDescribable
    {
        private readonly string extraInfo;

        public DefaultCharacterInfo(string title, Sprite icon, string extraInfo)
        {
            Title = title;
            Icon = icon;
            Description = extraInfo;
            this.extraInfo = extraInfo;
        }

        public string Title { get; }
        public Sprite Icon { get; }
        public string Description { get; }

        public IEnumerable<DescriptorInfo> GetExtraInfos()
        {
            if (string.IsNullOrWhiteSpace(extraInfo))
            {
                yield break;
            }

            yield return new DescriptorInfo(string.Empty, extraInfo);
        }
    }
}
