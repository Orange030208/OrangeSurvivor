using UnityEngine;

public class UIUpgradeContainer : UIContainerBase<InfoAddIndex<UpgradeCardOptionSnapshot>,Describer>
{
    [Header("稀有度表现")]
    [SerializeField] private UpgradeCardRarityPresenter rarityPresenter;
    [SerializeField] private bool playRevealSfx = true;

    public override void Configure(InfoAddIndex<UpgradeCardOptionSnapshot> resource)
    {
        UpgradeCardOptionSnapshot option = resource.info;
        iconImage.sprite = option.Icon;
        nameText.text = option.Title;
        DefaultDescribe describable = new DefaultDescribe
        {
            Description = BuildDescription(option)
        };
        bottom.Display(describable);
        UpgradeCardRarityPresentationProfile presentationProfile = ResolveRarityPresentationProfile(option.Rarity);
        ApplyRarityPresentation(presentationProfile);
        PlayRevealSfx(presentationProfile);
        CleanClickEvent();
        OnClicked += _ =>
        {
            PlaySelectSfx(presentationProfile);
            GameEventBus.Publish<UpgradeContainerClickedEvent>(new UpgradeContainerClickedEvent(resource.index));
        };
    }

    private void ApplyRarityPresentation(UpgradeCardRarityPresentationProfile profile)
    {
        if (rarityPresenter == null)
        {
            rarityPresenter = GetComponent<UpgradeCardRarityPresenter>();
        }

        rarityPresenter?.Apply(profile);
    }

    private static UpgradeCardRarityPresentationProfile ResolveRarityPresentationProfile(UpgradeCardRarity rarity)
    {
        UpgradeCardRarityPresentationCatalogSO catalog = ResourcesManager.GetUpgradeCardRarityPresentationCatalog();
        return catalog != null && catalog.TryGetProfile(rarity, out UpgradeCardRarityPresentationProfile configuredProfile)
            ? configuredProfile
            : UpgradeCardRarityPresentationCatalogSO.GetDefaultProfile(rarity);
    }

    private void PlayRevealSfx(UpgradeCardRarityPresentationProfile profile)
    {
        if (!playRevealSfx)
        {
            return;
        }

        AudioSfxBridge.RequestPlay(profile.RevealSfxKey);
    }

    private static void PlaySelectSfx(UpgradeCardRarityPresentationProfile profile)
    {
        AudioSfxKey selectSfxKey = profile.SelectSfxKey != AudioSfxKey.None
            ? profile.SelectSfxKey
            : AudioSfxKey.WoodenButtonClicked;
        AudioSfxBridge.RequestPlay(selectSfxKey);
    }

    private static string BuildDescription(UpgradeCardOptionSnapshot option)
    {
        string description = option.Description;
        string rarityText = GetRarityText(option.Rarity);
        string pickText = option.MaxPickCount > 1
            ? $"\n已选择 {option.PickCount}/{option.MaxPickCount}"
            : string.Empty;
        string tagText = BuildTagText(option.Tags);
        return $"{rarityText}{tagText}\n{description}{pickText}";
    }

    private static string GetRarityText(UpgradeCardRarity rarity)
    {
        return rarity switch
        {
            UpgradeCardRarity.Common => "普通",
            UpgradeCardRarity.Rare => "稀有",
            UpgradeCardRarity.Epic => "史诗",
            UpgradeCardRarity.Legendary => "传说",
            _ => rarity.ToString()
        };
    }

    private static string BuildTagText(UpgradeCardTag[] tags)
    {
        if (tags == null || tags.Length == 0)
        {
            return string.Empty;
        }

        int count = Mathf.Min(2, tags.Length);
        string result = " · ";
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                result += "/";
            }

            result += GetTagText(tags[i]);
        }

        return result;
    }

    private static string GetTagText(UpgradeCardTag tag)
    {
        return tag switch
        {
            UpgradeCardTag.Attack => "攻击",
            UpgradeCardTag.Defense => "防御",
            UpgradeCardTag.Critical => "暴击",
            UpgradeCardTag.AttackSpeed => "攻速",
            UpgradeCardTag.MoveSpeed => "移动",
            UpgradeCardTag.Pickup => "拾取",
            UpgradeCardTag.Economy => "经济",
            UpgradeCardTag.Weapon => "武器",
            UpgradeCardTag.Melee => "近战",
            UpgradeCardTag.Ranged => "远程",
            UpgradeCardTag.Projectile => "投射物",
            UpgradeCardTag.Recovery => "回复",
            UpgradeCardTag.LowHealth => "低血",
            UpgradeCardTag.AreaDamage => "范围",
            _ => tag.ToString()
        };
    }
}

public struct InfoAddIndex<T>
{
    public T info;
    public int index;

    public InfoAddIndex(T info, int index)
    {
        this.info = info;
        this.index = index;
    }
}
