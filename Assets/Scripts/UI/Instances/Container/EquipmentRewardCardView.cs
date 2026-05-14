using UnityEngine;
using UnityEngine.UI;

public class EquipmentRewardCardView : RewardSelectionCardViewBase
{
    [SerializeField] private GameObject iconRoot;
    [SerializeField] private Image iconImage;

    protected override RewardOptionKind ExpectedKind => RewardOptionKind.Weapon;
    protected override string ExpectedKindDescription => $"{RewardOptionKind.Weapon} or {RewardOptionKind.Accessory}";

    protected override bool SupportsKind(RewardOptionKind kind)
    {
        return kind == RewardOptionKind.Weapon || kind == RewardOptionKind.Accessory;
    }

    protected override void RenderPresentation(IRewardCardPresentation option)
    {
        base.RenderPresentation(option);
        ResolveIconReferences();

        bool hasIcon = option.Icon != null;
        if (iconRoot != null)
        {
            iconRoot.SetActive(hasIcon);
        }

        if (iconImage != null)
        {
            iconImage.sprite = option.Icon;
            iconImage.enabled = hasIcon;
        }
    }

    private void ResolveIconReferences()
    {
        if (iconImage == null)
        {
            Transform iconTransform = FindChildByName(transform, "Icon");
            iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        }

        if (iconRoot == null)
        {
            Transform iconFrameTransform = FindChildByName(transform, "IconFrame");
            if (iconFrameTransform != null)
            {
                iconRoot = iconFrameTransform.gameObject;
                return;
            }
        }

        if (iconRoot == null && iconImage != null)
        {
            iconRoot = iconImage.transform.parent != null
                ? iconImage.transform.parent.gameObject
                : iconImage.gameObject;
        }
    }

    private static Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
