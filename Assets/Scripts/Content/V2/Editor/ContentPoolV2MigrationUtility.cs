#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ContentPoolV2MigrationUtility
{
    private const string MenuRoot = "Tools/Survivors/Content Pools/";

    [MenuItem(MenuRoot + "Create V2 Profiles From Selected Legacy Pools")]
    public static void CreateV2ProfilesFromSelection()
    {
        Object[] selections = Selection.objects;
        List<ContentPoolSO> pools = new();
        for (int i = 0; i < selections.Length; i++)
        {
            if (selections[i] is ContentPoolSO pool)
            {
                pools.Add(pool);
            }
        }

        if (pools.Count == 0)
        {
            Debug.LogWarning("[ContentPoolV2MigrationUtility] Select one or more ContentPoolSO assets first.");
            return;
        }

        string report = CreateProfiles(pools);
        Debug.Log(report);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static string CreateProfiles(IReadOnlyList<ContentPoolSO> pools)
    {
        List<string> lines = new()
        {
            "# Content Pool V2 Migration Report"
        };

        if (pools == null || pools.Count == 0)
        {
            lines.Add("- No pools supplied.");
            return string.Join("\n", lines);
        }

        for (int i = 0; i < pools.Count; i++)
        {
            ContentPoolSO pool = pools[i];
            if (pool == null)
            {
                continue;
            }

            string legacyPath = AssetDatabase.GetAssetPath(pool);
            if (string.IsNullOrWhiteSpace(legacyPath))
            {
                lines.Add($"- Skipped in-memory pool '{pool.name}'.");
                continue;
            }

            ContentPoolProfileSO profile = CreateOrUpdateProfile(pool, legacyPath, out string profilePath);
            lines.Add($"- {legacyPath} -> {profilePath} ({profile.Entries.Count} entries)");
        }

        return string.Join("\n", lines);
    }

    private static ContentPoolProfileSO CreateOrUpdateProfile(
        ContentPoolSO legacyPool,
        string legacyPath,
        out string profilePath)
    {
        profilePath = ResolveProfilePath(legacyPath);
        EnsureFolder(Path.GetDirectoryName(profilePath)?.Replace('\\', '/'));

        ContentPoolProfileSO profile = AssetDatabase.LoadAssetAtPath<ContentPoolProfileSO>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<ContentPoolProfileSO>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }

        List<ContentPoolEntryDefinition> entries = new();
        if (legacyPool.Entries != null)
        {
            for (int i = 0; i < legacyPool.Entries.Count; i++)
            {
                ContentPoolEntry entry = legacyPool.Entries[i];
                ContentPoolEntryDefinition definition = ContentPoolEntryDefinition.FromLegacy(
                    entry,
                    entry != null ? entry.BaseWeight : 0f);
                if (definition != null)
                {
                    entries.Add(definition);
                }
            }
        }

        profile.Initialize(
            legacyPool.name,
            GuessKind(legacyPool),
            entries,
            legacyPool.DefaultRollCount,
            legacyPool.AllowDuplicateResults);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static string ResolveProfilePath(string legacyPath)
    {
        string directory = Path.GetDirectoryName(legacyPath)?.Replace('\\', '/') ?? "Assets";
        string fileName = Path.GetFileNameWithoutExtension(legacyPath);
        return $"{directory}/PoolsV2/{fileName} V2.asset";
    }

    private static ContentPoolKind GuessKind(ContentPoolSO pool)
    {
        string poolName = pool != null ? pool.name.ToLowerInvariant() : string.Empty;
        if (poolName.Contains("upgrade"))
        {
            return ContentPoolKind.UpgradeCard;
        }

        if (poolName.Contains("chest"))
        {
            return ContentPoolKind.ChestReward;
        }

        if (poolName.Contains("shop"))
        {
            return ContentPoolKind.Shop;
        }

        if (poolName.Contains("drop") || poolName.Contains("rewardpool"))
        {
            return ContentPoolKind.Drop;
        }

        if (poolName.Contains("wave"))
        {
            return ContentPoolKind.WaveSpawn;
        }

        if (poolName.Contains("weapon"))
        {
            return ContentPoolKind.WeaponReward;
        }

        return ContentPoolKind.Generic;
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        string name = Path.GetFileName(folder);
        EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
