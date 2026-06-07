using System.Collections.Generic;
using UnityEngine;

public class RewardCardApplyService
{
    public bool Apply(RewardCardSO card, Player player)
    {
        if (card == null || player == null)
        {
            return false;
        }

        bool appliedAnyEffect = false;
        string sourceId = $"RewardCard_{card.Id}_{System.Guid.NewGuid():N}";
        FeatureHost featureHost = player.GetComponent<FeatureHost>();

        IReadOnlyList<FeatureBase> grantedAbilities = card.GrantedAbilities;
        if (grantedAbilities != null && grantedAbilities.Count > 0)
        {
            if (featureHost != null && featureHost.InstallFeature(sourceId, grantedAbilities))
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
