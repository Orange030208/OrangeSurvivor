using System;
using System.Collections.Generic;
using Orange.Extraction;
using UnityEngine;

public sealed class EquipmentRewardSelectionHandler : IRewardSelectionHandler
{
    private const int OPTION_COUNT = 3;
    private const int DEFAULT_WEAPON_LEVEL = WeaponLevelHelper.MinLevel;

    private readonly Definition definition;

    private EquipmentRewardSelectionHandler(Definition definition)
    {
        this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public RewardSelectionReason Reason => definition.Reason;

    public static EquipmentRewardSelectionHandler CreateWeapon()
    {
        return new EquipmentRewardSelectionHandler(Definition.CreateWeapon());
    }

    public static EquipmentRewardSelectionHandler CreateAccessory()
    {
        return new EquipmentRewardSelectionHandler(Definition.CreateAccessory());
    }

    public bool ShouldCreateSelection(RewardSelectionHandlerContext context, bool hasProcessedSelection)
    {
        return !hasProcessedSelection && definition.CanCreateSelection(context);
    }

    public RewardSelectionRound CreateSelection(RewardSelectionHandlerContext context)
    {
        if (context == null)
        {
            return EmptyRound();
        }

        RewardSelectionOption[] options = definition.CreateOptions(context);
        if (options.Length == 0)
        {
            Debug.LogWarning(definition.EmptyRollWarning, context.LogContext);
        }

        return new RewardSelectionRound(definition.Title, definition.Description, options);
    }

    public bool ApplySelection(RewardSelectionOption option, RewardSelectionHandlerContext context)
    {
        return definition.ApplySelection(option, context);
    }

    private RewardSelectionRound EmptyRound()
    {
        return new RewardSelectionRound(definition.Title, definition.Description, Array.Empty<RewardSelectionOption>());
    }

    private sealed class Definition
    {
        private readonly Func<RewardSelectionHandlerContext, bool> canCreateSelection;
        private readonly Func<RewardSelectionHandlerContext, RewardSelectionOption[]> createOptions;
        private readonly Func<RewardSelectionOption, RewardSelectionHandlerContext, bool> applySelection;

        private Definition(
            RewardSelectionReason reason,
            string title,
            string description,
            string emptyRollWarning,
            Func<RewardSelectionHandlerContext, bool> canCreateSelection,
            Func<RewardSelectionHandlerContext, RewardSelectionOption[]> createOptions,
            Func<RewardSelectionOption, RewardSelectionHandlerContext, bool> applySelection)
        {
            Reason = reason;
            Title = title;
            Description = description;
            EmptyRollWarning = emptyRollWarning;
            this.canCreateSelection = canCreateSelection;
            this.createOptions = createOptions;
            this.applySelection = applySelection;
        }

        public RewardSelectionReason Reason { get; }
        public string Title { get; }
        public string Description { get; }
        public string EmptyRollWarning { get; }

        public static Definition CreateWeapon()
        {
            return new Definition(
                RewardSelectionReason.Weapon,
                "选择武器奖励",
                "选择 1 把武器立即装备。",
                "[EquipmentRewardSelectionHandler] No weapons could be rolled for weapon reward.",
                context => context != null && context.WeaponsHolder != null,
                CreateWeaponOptions,
                (option, context) =>
                {
                    if (option is not WeaponRewardSelectionOption selectedOption || selectedOption.WeaponData == null)
                    {
                        return false;
                    }

                    if (context.WeaponsHolder != null &&
                        context.WeaponsHolder.AddWeapon(selectedOption.WeaponData, selectedOption.Level))
                    {
                        return true;
                    }

                    Debug.LogWarning($"[EquipmentRewardSelectionHandler] Failed to add weapon {selectedOption.WeaponData?.name}.", context.LogContext);
                    return false;
                });
        }

        public static Definition CreateAccessory()
        {
            return new Definition(
                RewardSelectionReason.Chest,
                "选择宝箱奖励",
                "选择 1 个饰品立即装备。",
                "[EquipmentRewardSelectionHandler] No accessories could be rolled for chest reward.",
                context => context != null,
                CreateAccessoryOptions,
                (option, context) =>
                {
                    if (option is not AccessoryRewardSelectionOption selectedOption || selectedOption.AccessoryData == null)
                    {
                        return false;
                    }

                    if (context.AccessoryManager != null &&
                        context.AccessoryManager.EquipAccessory(selectedOption.AccessoryData))
                    {
                        return true;
                    }

                    Debug.LogWarning($"[EquipmentRewardSelectionHandler] Failed to add accessory {selectedOption.AccessoryData?.name}.", context.LogContext);
                    return false;
                });
        }

        public bool CanCreateSelection(RewardSelectionHandlerContext context)
        {
            return canCreateSelection.Invoke(context);
        }

        public RewardSelectionOption[] CreateOptions(RewardSelectionHandlerContext context)
        {
            return createOptions.Invoke(context);
        }

        public bool ApplySelection(RewardSelectionOption option, RewardSelectionHandlerContext context)
        {
            return context != null && applySelection.Invoke(option, context);
        }

        private static RewardSelectionOption[] CreateWeaponOptions(RewardSelectionHandlerContext context)
        {
            if (context?.Weapons == null || context.Weapons.Count == 0)
            {
                Debug.LogError("[EquipmentRewardSelectionHandler] Missing weapon reward candidates.", context?.LogContext);
                return Array.Empty<RewardSelectionOption>();
            }

            ContentTier tier = ContentTierResolver.FromWeaponLevel(DEFAULT_WEAPON_LEVEL);
            WeightedExtractionPool<WeaponDataSO, RewardSelectionHandlerContext> pool = new();
            for (int i = 0; i < context.Weapons.Count; i++)
            {
                WeaponDataSO weapon = context.Weapons[i];
                if (weapon == null || string.IsNullOrWhiteSpace(weapon.WeaponId))
                {
                    continue;
                }

                float baseWeight = context.TierWeightProfile != null
                    ? context.TierWeightProfile.GetWeight(tier)
                    : 1f;
                pool.AddEntry(weapon.WeaponId, weapon, baseWeight);
            }

            IReadOnlyList<ExtractionResult<WeaponDataSO>> results = pool.DrawManyUnique(context, OPTION_COUNT);
            RewardSelectionOption[] options = new RewardSelectionOption[results.Count];
            for (int i = 0; i < results.Count; i++)
            {
                WeaponDataSO weaponData = results[i].Item;
                RewardCardViewConfig viewConfig = RewardCardViewConfigFactory.CreateWeapon(
                    weaponData,
                    DEFAULT_WEAPON_LEVEL,
                    tier);
                options[i] = new WeaponRewardSelectionOption(weaponData, DEFAULT_WEAPON_LEVEL, viewConfig);
            }

            return options;
        }

        private static RewardSelectionOption[] CreateAccessoryOptions(RewardSelectionHandlerContext context)
        {
            if (context?.Accessories == null || context.Accessories.Count == 0)
            {
                Debug.LogError("[EquipmentRewardSelectionHandler] Missing chest reward candidates.", context?.LogContext);
                return Array.Empty<RewardSelectionOption>();
            }

            if (context.TierWeightProfile == null)
            {
                Debug.LogError($"[EquipmentRewardSelectionHandler] Missing {nameof(ContentTierWeightProfileSO)}.", context.LogContext);
                return Array.Empty<RewardSelectionOption>();
            }

            WeightedExtractionPool<AccessoryDataSO, RewardSelectionHandlerContext> pool = new();
            for (int i = 0; i < context.Accessories.Count; i++)
            {
                AccessoryDataSO accessory = context.Accessories[i];
                if (accessory == null || string.IsNullOrWhiteSpace(accessory.AccessoryId))
                {
                    continue;
                }

                pool.AddEntry(
                    accessory.AccessoryId,
                    accessory,
                    context.TierWeightProfile.GetWeight(accessory.Tier),
                    IsAccessoryEligible);
            }

            IReadOnlyList<ExtractionResult<AccessoryDataSO>> results = pool.DrawManyUnique(context, OPTION_COUNT);
            RewardSelectionOption[] options = new RewardSelectionOption[results.Count];
            for (int i = 0; i < results.Count; i++)
            {
                AccessoryDataSO accessory = results[i].Item;
                ContentTier tier = accessory != null ? accessory.Tier : ContentTier.Common;
                RewardCardViewConfig viewConfig = RewardCardViewConfigFactory.CreateAccessory(accessory, tier);
                options[i] = new AccessoryRewardSelectionOption(accessory, viewConfig);
            }

            return options;
        }

        private static bool IsAccessoryEligible(
            WeightedExtractionEntry<AccessoryDataSO, RewardSelectionHandlerContext> entry,
            RewardSelectionHandlerContext context)
        {
            if (entry?.Item == null)
            {
                return false;
            }

            return context?.AccessoryManager == null || context.AccessoryManager.CanEquipAccessory(entry.Item);
        }
    }
}
