using UnityEngine;

public static class FeatureInstaller
{
    public const string CharacterSourceId = "CHARACTER_BASE";

    public static bool InstallCharacter(FeatureHost featureHost, CharacterDataSO characterData)
    {
        if (featureHost == null || characterData == null)
        {
            return false;
        }

        return featureHost.InstallSource(CharacterSourceId, characterData);
    }

    public static bool InstallSource(FeatureHost featureHost, string sourceId, IFeatureSource source)
    {
        if (featureHost == null || source == null)
        {
            return false;
        }

        return featureHost.InstallSource(sourceId, source);
    }

    public static bool RemoveSource(FeatureHost featureHost, string sourceId)
    {
        if (featureHost == null)
        {
            return false;
        }

        return featureHost.RemoveSource(sourceId);
    }
}
