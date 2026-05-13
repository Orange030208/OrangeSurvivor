using UnityEngine;

public class UpgradeCardApplyService
{
    public bool Apply(UpgradeCardSO card, Player player)
    {
        if (card == null || player == null)
        {
            return false;
        }

        bool appliedAnyEffect = false;
        string sourceId = $"UpgradeCard_{card.CardId}_{System.Guid.NewGuid():N}";
        FeatureHost featureHost = player.GetComponent<FeatureHost>();

        var specialFeatures = card.SpecialFeatures;
        if (specialFeatures != null && specialFeatures.Count > 0)
        {
            if (featureHost != null && featureHost.InstallFeature(sourceId, specialFeatures))
            {
                appliedAnyEffect = true;
            }
            else
            {
                Debug.LogWarning($"[UpgradeCardApplyService] Player is missing {nameof(FeatureHost)} or feature install failed.");
            }
        }

        return appliedAnyEffect;
    }
}
