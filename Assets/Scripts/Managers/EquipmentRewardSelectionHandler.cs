using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EquipmentRewardSelectionHandler : IRewardSelectionHandler
{
    private const int OPTION_COUNT = 3;
    private const int DEFAULT_WEAPON_LEVEL = WeaponLevelHelper.MinLevel;

    private readonly ContentPoolRollService rollService = new();
    private readonly EquipmentRewardCardPresenter presenter = new();
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
        ContentPoolSO pool = definition.ResolvePool(context);
        if (pool == null)
        {
            return EmptyRound();
        }

        ContentRollContext rollContext = definition.CreateRollContext(context, pool);
        ContentRollResult result = rollService.Roll(
            pool,
            rollContext,
            OPTION_COUNT,
            definition.CanUseEntry);
        List<ContentRollItem> items = new(result.Items);
        int count = Mathf.Min(OPTION_COUNT, items.Count);
        RewardSelectionOption[] options = new RewardSelectionOption[count];

        for (int i = 0; i < count; i++)
        {
            ContentRollItem rollItem = items[i];
            options[i] = definition.CreateOption(rollItem, presenter);
        }

        if (options.Length == 0)
        {
            Debug.LogWarning(definition.EmptyRollWarning, context.LogContext);
        }

        return new RewardSelectionRound(definition.Title, definition.Description, options);
    }

    public bool ApplySelection(RewardSelectionOption option, RewardSelectionHandlerContext context)
    {
        if (!definition.ApplySelection(option, context))
        {
            return false;
        }

        ContentPoolSO pool = definition.ResolvePool(context);
        ContentRollItem rollItem = definition.GetRollItem(option);
        context.ContentHistoryState.RecordPick(
            context.CreateHistoryScope(pool, definition.ScopeId),
            rollItem);
        return true;
    }

    private RewardSelectionRound EmptyRound()
    {
        return new RewardSelectionRound(definition.Title, definition.Description, Array.Empty<RewardSelectionOption>());
    }

    private sealed class Definition
    {
        private readonly Func<RewardSelectionHandlerContext, bool> canCreateSelection;
        private readonly Func<RewardSelectionHandlerContext, ContentPoolSO> resolveContextPool;
        private readonly Func<IGameContentProvider, ContentPoolSO> resolveProviderPool;
        private readonly Func<RewardSelectionHandlerContext, ContentPoolSO, ContentRollContext> createRollContext;
        private readonly Predicate<ContentPoolEntry> canUseEntry;
        private readonly Func<ContentRollItem, EquipmentRewardCardPresenter, RewardSelectionOption> createOption;
        private readonly Func<RewardSelectionOption, RewardSelectionHandlerContext, bool> applySelection;
        private readonly Func<RewardSelectionOption, ContentRollItem> getRollItem;

        private Definition(
            RewardSelectionReason reason,
            string title,
            string description,
            string scopeId,
            string missingPoolError,
            string emptyRollWarning,
            Func<RewardSelectionHandlerContext, bool> canCreateSelection,
            Func<RewardSelectionHandlerContext, ContentPoolSO> resolveContextPool,
            Func<IGameContentProvider, ContentPoolSO> resolveProviderPool,
            Func<RewardSelectionHandlerContext, ContentPoolSO, ContentRollContext> createRollContext,
            Predicate<ContentPoolEntry> canUseEntry,
            Func<ContentRollItem, EquipmentRewardCardPresenter, RewardSelectionOption> createOption,
            Func<RewardSelectionOption, RewardSelectionHandlerContext, bool> applySelection,
            Func<RewardSelectionOption, ContentRollItem> getRollItem)
        {
            Reason = reason;
            Title = title;
            Description = description;
            ScopeId = scopeId;
            MissingPoolError = missingPoolError;
            EmptyRollWarning = emptyRollWarning;
            this.canCreateSelection = canCreateSelection;
            this.resolveContextPool = resolveContextPool;
            this.resolveProviderPool = resolveProviderPool;
            this.createRollContext = createRollContext;
            this.canUseEntry = canUseEntry;
            this.createOption = createOption;
            this.applySelection = applySelection;
            this.getRollItem = getRollItem;
        }

        public RewardSelectionReason Reason { get; }
        public string Title { get; }
        public string Description { get; }
        public string ScopeId { get; }
        public string MissingPoolError { get; }
        public string EmptyRollWarning { get; }

        public static Definition CreateWeapon()
        {
            return new Definition(
                RewardSelectionReason.Weapon,
                "选择武器奖励",
                "选择 1 把武器立即装备。",
                ContentPoolScopeIds.WeaponReward,
                $"[EquipmentRewardSelectionHandler] Missing weapon reward content pool in scene or {nameof(GameContentCatalogSO)}.",
                "[EquipmentRewardSelectionHandler] No weapons could be rolled for weapon reward.",
                context => context != null && context.WeaponsHolder != null,
                context => context.WeaponRewardPool,
                provider => provider.WeaponRewardPool,
                (context, pool) => new ContentRollContext(
                    ContentPoolScopeIds.WeaponReward,
                    context.Player,
                    progressionSnapshot: RunProgressionRuntime.CurrentSnapshot,
                    historyScope: context.CreateHistoryScope(pool, ContentPoolScopeIds.WeaponReward),
                    history: context.ContentHistoryState,
                    weaponsHolder: context.WeaponsHolder),
                entry => entry.Content is WeaponDataSO,
                (rollItem, presenter) =>
                {
                    WeaponDataSO weaponData = rollItem.Content as WeaponDataSO;
                    EquipmentRewardCardPresentation presentation = presenter.CreateWeapon(weaponData, DEFAULT_WEAPON_LEVEL);
                    return new WeaponRewardSelectionOption(
                        presentation.OptionId,
                        weaponData,
                        DEFAULT_WEAPON_LEVEL,
                        rollItem,
                        presentation);
                },
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
                },
                option => option is WeaponRewardSelectionOption selectedOption
                    ? selectedOption.RollItem
                    : default);
        }

        public static Definition CreateAccessory()
        {
            return new Definition(
                RewardSelectionReason.Chest,
                "选择宝箱奖励",
                "选择 1 个饰品立即装备。",
                ContentPoolScopeIds.ChestReward,
                $"[EquipmentRewardSelectionHandler] Missing chest reward content pool in scene or {nameof(GameContentCatalogSO)}.",
                "[EquipmentRewardSelectionHandler] No accessories could be rolled for chest reward.",
                context => context != null,
                context => context.ChestRewardPool,
                provider => provider.ChestRewardPool,
                (context, pool) => new ContentRollContext(
                    ContentPoolScopeIds.ChestReward,
                    context.Player,
                    progressionSnapshot: context.CreateWaveProgressionSnapshot(),
                    historyScope: context.CreateHistoryScope(pool, ContentPoolScopeIds.ChestReward),
                    history: context.ContentHistoryState),
                entry => entry.Content is AccessoryDataSO,
                (rollItem, presenter) =>
                {
                    AccessoryDataSO accessory = rollItem.Content as AccessoryDataSO;
                    EquipmentRewardCardPresentation presentation = presenter.CreateAccessory(accessory);
                    return new AccessoryRewardSelectionOption(presentation.OptionId, accessory, rollItem, presentation);
                },
                (option, context) =>
                {
                    if (option is not AccessoryRewardSelectionOption selectedOption || selectedOption.AccessoryData == null)
                    {
                        return false;
                    }

                    context.AccessoryManager?.EquipAccessory(selectedOption.AccessoryData);
                    return true;
                },
                option => option is AccessoryRewardSelectionOption selectedOption
                    ? selectedOption.RollItem
                    : default);
        }

        public bool CanCreateSelection(RewardSelectionHandlerContext context)
        {
            return canCreateSelection.Invoke(context);
        }

        public ContentPoolSO ResolvePool(RewardSelectionHandlerContext context)
        {
            if (context == null)
            {
                return null;
            }

            ContentPoolSO pool = resolveContextPool.Invoke(context);
            if (pool != null)
            {
                return pool;
            }

            if (GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
            {
                pool = resolveProviderPool.Invoke(provider);
                if (pool != null)
                {
                    return pool;
                }
            }

            Debug.LogError(MissingPoolError, context.LogContext);
            return null;
        }

        public ContentRollContext CreateRollContext(RewardSelectionHandlerContext context, ContentPoolSO pool)
        {
            return createRollContext.Invoke(context, pool);
        }

        public bool CanUseEntry(ContentPoolEntry entry)
        {
            return canUseEntry.Invoke(entry);
        }

        public RewardSelectionOption CreateOption(ContentRollItem rollItem, EquipmentRewardCardPresenter presenter)
        {
            return createOption.Invoke(rollItem, presenter);
        }

        public bool ApplySelection(RewardSelectionOption option, RewardSelectionHandlerContext context)
        {
            return context != null && applySelection.Invoke(option, context);
        }

        public ContentRollItem GetRollItem(RewardSelectionOption option)
        {
            return getRollItem.Invoke(option);
        }
    }
}
