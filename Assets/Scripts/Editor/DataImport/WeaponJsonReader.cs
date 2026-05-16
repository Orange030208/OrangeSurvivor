#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class WeaponJsonReader
{
    public const string DefaultJsonPath = "Assets/GameContent/Weapons/Data/weapons.json";

    public static IReadOnlyList<WeaponJsonWeapon> ReadDefault()
    {
        return Read(DefaultJsonPath);
    }

    public static IReadOnlyList<WeaponJsonWeapon> Read(string jsonPath)
    {
        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            throw new ArgumentException("Weapon JSON path must not be empty.", nameof(jsonPath));
        }

        string absolutePath = Path.GetFullPath(jsonPath);
        if (!File.Exists(absolutePath))
        {
            throw new DataImportException($"Weapon JSON does not exist: {absolutePath}");
        }

        try
        {
            string json = File.ReadAllText(absolutePath, Encoding.UTF8);
            WeaponJsonFile file = JsonUtility.FromJson<WeaponJsonFile>(WrapRootArray(json));
            if (file?.weapons == null)
            {
                throw new DataImportException($"Weapon JSON has no weapons array: {absolutePath}");
            }

            Validate(file.weapons, absolutePath);
            return file.weapons;
        }
        catch (DataImportException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new DataImportException($"Failed to read weapon JSON: {absolutePath}", exception);
        }
    }

    private static string WrapRootArray(string json)
    {
        string normalized = (json ?? string.Empty).Trim();
        if (normalized.StartsWith("[", StringComparison.Ordinal))
        {
            return "{\"weapons\":" + normalized + "}";
        }

        return normalized;
    }

    private static void Validate(IReadOnlyList<WeaponJsonWeapon> weapons, string context)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);
        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponJsonWeapon weapon = weapons[i];
            string weaponContext = $"{context} weapon[{i}]";
            if (weapon == null)
            {
                throw new DataImportException($"{weaponContext} is null.");
            }

            Require(weapon.weaponId, weaponContext, nameof(weapon.weaponId));
            Require(weapon.itemName, weaponContext, nameof(weapon.itemName));
            if (!ids.Add(weapon.weaponId))
            {
                throw new DataImportException($"{weaponContext} has duplicated weaponId '{weapon.weaponId}'.");
            }

            if (weapon.tags == null)
            {
                throw new DataImportException($"{weapon.weaponId} has null tags.");
            }

            if (weapon.spawnPoints == null)
            {
                throw new DataImportException($"{weapon.weaponId} has null spawnPoints.");
            }

            if (weapon.levelStats == null)
            {
                throw new DataImportException($"{weapon.weaponId} has null levelStats.");
            }

            int expectedLevelCount = WeaponLevelHelper.MaxLevel - WeaponLevelHelper.MinLevel + 1;
            if (weapon.levelStats.Count != expectedLevelCount)
            {
                throw new DataImportException(
                    $"{weapon.weaponId} must contain exactly {expectedLevelCount} levelStats rows.");
            }

            for (int statIndex = 0; statIndex < weapon.levelStats.Count; statIndex++)
            {
                WeaponJsonLevelStat stat = weapon.levelStats[statIndex];
                if (stat == null)
                {
                    throw new DataImportException($"{weapon.weaponId} levelStats[{statIndex}] is null.");
                }

                int expectedLevel = WeaponLevelHelper.MinLevel + statIndex;
                if (stat.level != expectedLevel)
                {
                    throw new DataImportException(
                        $"{weapon.weaponId} levelStats[{statIndex}] level must be {expectedLevel}, got {stat.level}.");
                }

                if (stat.statBenefits == null)
                {
                    throw new DataImportException($"{weapon.weaponId} levelStats[{statIndex}] is missing statBenefits.");
                }

                if (stat.holderModifiers == null)
                {
                    throw new DataImportException($"{weapon.weaponId} levelStats[{statIndex}] has null holderModifiers.");
                }
            }
        }
    }

    private static void Require(string value, string context, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DataImportException($"{context} is missing required field '{fieldName}'.");
        }
    }
}
#endif
