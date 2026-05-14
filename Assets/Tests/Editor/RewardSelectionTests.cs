using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

public sealed class RewardSelectionTests
{
    private readonly List<UnityEngine.Object> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            UnityEngine.Object createdObject = createdObjects[i];
            if (createdObject != null)
            {
                UnityEngine.Object.DestroyImmediate(createdObject);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void RewardSelectionCardGroupMixedStylesInstantiatesMatchingViewTypes()
    {
        GameObject groupObject = CreateGameObject("Reward Card Group");
        RewardSelectionCardGroup group = groupObject.AddComponent<RewardSelectionCardGroup>();
        UpgradeRewardCardView upgradePrefab = CreateViewPrefab<UpgradeRewardCardView>("Upgrade Prefab");
        EquipmentRewardCardView equipmentPrefab = CreateViewPrefab<EquipmentRewardCardView>("Equipment Prefab");
        SetPrefabMappings(
            group,
            new RewardCardPrefabEntry(RewardCardStyle.UpgradeCard, upgradePrefab),
            new RewardCardPrefabEntry(RewardCardStyle.EquipmentReward, equipmentPrefab));

        group.Configure(
            new IRewardCardPresentation[]
            {
                CreateUpgradePresentation("upgrade"),
                CreateWeaponPresentation("weapon"),
                CreateAccessoryPresentation("accessory")
            },
            null);

        Assert.AreEqual(3, groupObject.transform.childCount);
        Assert.IsNotNull(groupObject.transform.GetChild(0).GetComponent<UpgradeRewardCardView>());
        Assert.IsNotNull(groupObject.transform.GetChild(1).GetComponent<EquipmentRewardCardView>());
        Assert.IsNotNull(groupObject.transform.GetChild(2).GetComponent<EquipmentRewardCardView>());
    }

    [Test]
    public void RewardSelectionCardGroupMissingStyleMappingThrowsClearError()
    {
        GameObject groupObject = CreateGameObject("Reward Card Group");
        RewardSelectionCardGroup group = groupObject.AddComponent<RewardSelectionCardGroup>();
        UpgradeRewardCardView upgradePrefab = CreateViewPrefab<UpgradeRewardCardView>("Upgrade Prefab");
        SetPrefabMappings(group, new RewardCardPrefabEntry(RewardCardStyle.UpgradeCard, upgradePrefab));

        MissingReferenceException exception = Assert.Throws<MissingReferenceException>(() =>
            group.Configure(
                new IRewardCardPresentation[] { CreateWeaponPresentation("weapon") },
                null));

        StringAssert.Contains(nameof(RewardCardStyle.EquipmentReward), exception.Message);
    }

    [Test]
    public async Task RewardSelectionCardGroupDefersSelectionCallbackUntilSubmitAnimationsComplete()
    {
        GameObject groupObject = CreateGameObject("Reward Card Group");
        RewardSelectionCardGroup group = groupObject.AddComponent<RewardSelectionCardGroup>();
        TestRewardSelectionCardView upgradePrefab = CreateViewPrefab<TestRewardSelectionCardView>("Test Upgrade Prefab");
        SetPrefabMappings(group, new RewardCardPrefabEntry(RewardCardStyle.UpgradeCard, upgradePrefab));

        int callbackCount = 0;
        int selectedIndex = -1;
        string selectedOptionId = null;
        group.Configure(
            new IRewardCardPresentation[]
            {
                CreateUpgradePresentation("option-0"),
                CreateUpgradePresentation("option-1"),
                CreateUpgradePresentation("option-2")
            },
            (index, optionId) =>
            {
                callbackCount++;
                selectedIndex = index;
                selectedOptionId = optionId;
            });

        TestRewardSelectionCardView first = groupObject.transform.GetChild(0).GetComponent<TestRewardSelectionCardView>();
        TestRewardSelectionCardView selected = groupObject.transform.GetChild(1).GetComponent<TestRewardSelectionCardView>();
        TestRewardSelectionCardView third = groupObject.transform.GetChild(2).GetComponent<TestRewardSelectionCardView>();

        selected.OnPointerClick(null);
        await UniTask.Yield();

        Assert.AreEqual(1, selected.SelectedSubmitCount);
        Assert.AreEqual(1, first.RejectedSubmitCount);
        Assert.AreEqual(1, third.RejectedSubmitCount);
        Assert.AreEqual(0, callbackCount);

        selected.CompleteSelectedSubmit();
        first.CompleteRejectedSubmit();
        await UniTask.Yield();
        Assert.AreEqual(0, callbackCount);

        third.CompleteRejectedSubmit();
        await UniTask.Yield();

        Assert.AreEqual(1, callbackCount);
        Assert.AreEqual(1, selectedIndex);
        Assert.AreEqual("option-1", selectedOptionId);
    }

    [Test]
    public void RewardCardPresentersCreateExpectedPresentationMetadata()
    {
        UpgradeRewardCardPresentation upgrade = new UpgradeRewardCardPresenter().Create(default);
        EquipmentRewardCardPresenter equipmentPresenter = new();
        EquipmentRewardCardPresentation weapon = equipmentPresenter.CreateWeapon(null, WeaponLevelHelper.MinLevel);
        EquipmentRewardCardPresentation accessory = equipmentPresenter.CreateAccessory(null);

        AssertPresentation(
            upgrade,
            string.Empty,
            RewardOptionKind.UpgradeCard,
            RewardCardStyle.UpgradeCard,
            CardQuality.Common,
            false);
        AssertPresentation(
            weapon,
            string.Empty,
            RewardOptionKind.Weapon,
            RewardCardStyle.EquipmentReward,
            CardQuality.Common,
            false);
        AssertPresentation(
            accessory,
            string.Empty,
            RewardOptionKind.Accessory,
            RewardCardStyle.EquipmentReward,
            CardQuality.Common,
            false);
    }

    [Test]
    public void CardQualityVisualControllerAppliesShadowAndGlowScale()
    {
        GameObject cardObject = CreateGameObject("Quality Visual");
        CardQualityVisualController visual = cardObject.AddComponent<CardQualityVisualController>();
        RectTransform shadow = CreateGameObject("Shadow").AddComponent<RectTransform>();
        RectTransform glow = CreateGameObject("Glow").AddComponent<RectTransform>();
        shadow.SetParent(cardObject.transform, false);
        glow.SetParent(cardObject.transform, false);
        shadow.localScale = new Vector3(2f, 3f, 1f);
        glow.localScale = new Vector3(4f, 5f, 1f);

        visual.Apply(new CardQualityPresentationProfile(
            CardQuality.Epic,
            "test",
            default,
            default,
            Color.white,
            Color.white,
            null,
            null,
            null,
            1f,
            1.5f,
            0.5f));

        Assert.AreEqual(new Vector3(3f, 4.5f, 1f), shadow.localScale);
        Assert.AreEqual(new Vector3(2f, 2.5f, 1f), glow.localScale);
    }

    [Test]
    public void EquipmentRewardSelectionHandlerFactoriesExposeExpectedReasons()
    {
        IRewardSelectionHandler weaponHandler = EquipmentRewardSelectionHandler.CreateWeapon();
        IRewardSelectionHandler accessoryHandler = EquipmentRewardSelectionHandler.CreateAccessory();

        Assert.AreEqual(RewardSelectionReason.Weapon, weaponHandler.Reason);
        Assert.AreEqual(RewardSelectionReason.Chest, accessoryHandler.Reason);
    }

    [Test]
    public void EquipmentRewardSelectionHandlerShouldCreateSelectionMatchesRewardTypeRules()
    {
        IRewardSelectionHandler weaponHandler = EquipmentRewardSelectionHandler.CreateWeapon();
        IRewardSelectionHandler accessoryHandler = EquipmentRewardSelectionHandler.CreateAccessory();
        GameObject playerObject = CreateGameObject("Player");
        WeaponsHolder weaponsHolder = playerObject.AddComponent<WeaponsHolder>();
        RewardSelectionHandlerContext contextWithWeaponHolder = CreateRewardContext(weaponsHolder);
        RewardSelectionHandlerContext contextWithoutWeaponHolder = CreateRewardContext(null);

        Assert.IsTrue(weaponHandler.ShouldCreateSelection(contextWithWeaponHolder, false));
        Assert.IsFalse(weaponHandler.ShouldCreateSelection(contextWithoutWeaponHolder, false));
        Assert.IsFalse(weaponHandler.ShouldCreateSelection(contextWithWeaponHolder, true));
        Assert.IsTrue(accessoryHandler.ShouldCreateSelection(contextWithoutWeaponHolder, false));
        Assert.IsFalse(accessoryHandler.ShouldCreateSelection(contextWithoutWeaponHolder, true));
    }

    [Test]
    public void RewardSelectionManagerRejectsMismatchedSelectionId()
    {
        GameObject managerObject = CreateGameObject("Reward Selection Manager");
        managerObject.SetActive(false);
        RewardSelectionManager manager = managerObject.AddComponent<RewardSelectionManager>();
        RewardSelectionOption option = new UpgradeRewardSelectionOption(
            default,
            CreateUpgradePresentation("expected"));
        SetPrivateField(manager, "currentOptions", new[] { option });

        bool resolved = TryResolveSelectedOptionForTests(
            manager,
            new RewardSelectionResult(0, "wrong"),
            out RewardSelectionOption selectedOption);

        Assert.IsFalse(resolved);
        Assert.IsNull(selectedOption);
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject gameObject = new(name);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private T CreateViewPrefab<T>(string name) where T : RewardSelectionCardViewBase
    {
        GameObject gameObject = CreateGameObject(name);
        return gameObject.AddComponent<T>();
    }

    private static void SetPrefabMappings(RewardSelectionCardGroup group, params RewardCardPrefabEntry[] entries)
    {
        SetPrivateField(group, "cardPrefabs", entries);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Missing field '{fieldName}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static bool TryResolveSelectedOptionForTests(
        RewardSelectionManager manager,
        RewardSelectionResult result,
        out RewardSelectionOption selectedOption)
    {
        MethodInfo method = typeof(RewardSelectionManager).GetMethod(
            "TryResolveSelectedOption",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method, "Missing RewardSelectionManager.TryResolveSelectedOption.");

        object[] arguments = { result, null };
        bool resolved = (bool)method.Invoke(manager, arguments);
        selectedOption = arguments[1] as RewardSelectionOption;
        return resolved;
    }

    private static RewardSelectionHandlerContext CreateRewardContext(WeaponsHolder weaponsHolder)
    {
        return new RewardSelectionHandlerContext(
            null,
            null,
            null,
            weaponsHolder,
            new ContentHistoryState(),
            1,
            null,
            null,
            null,
            null);
    }

    private static UpgradeRewardCardPresentation CreateUpgradePresentation(string optionId)
    {
        return new UpgradeRewardCardPresentation(
            optionId,
            "Upgrade",
            "Upgrade description",
            CardQuality.Common,
            Array.Empty<string>(),
            true);
    }

    private static EquipmentRewardCardPresentation CreateWeaponPresentation(string optionId)
    {
        return new EquipmentRewardCardPresentation(
            RewardOptionKind.Weapon,
            optionId,
            new TestDescribable("Weapon", "Weapon description"),
            CardQuality.Rare,
            true);
    }

    private static EquipmentRewardCardPresentation CreateAccessoryPresentation(string optionId)
    {
        return new EquipmentRewardCardPresentation(
            RewardOptionKind.Accessory,
            optionId,
            new TestDescribable("Accessory", "Accessory description"),
            CardQuality.Epic,
            true);
    }

    private static void AssertPresentation(
        IRewardCardPresentation presentation,
        string optionId,
        RewardOptionKind kind,
        RewardCardStyle style,
        CardQuality quality,
        bool interactable)
    {
        Assert.AreEqual(optionId, presentation.OptionId);
        Assert.AreEqual(kind, presentation.Kind);
        Assert.AreEqual(style, presentation.Style);
        Assert.AreEqual(quality, presentation.Quality);
        Assert.AreEqual(interactable, presentation.Interactable);
    }

    private sealed class TestDescribable : IDescribable
    {
        public TestDescribable(string title, string description)
        {
            Title = title;
            Description = description;
        }

        public string Title { get; }
        public Sprite Icon => null;
        public string Description { get; }

        public IEnumerable<DescriptorInfo> GetExtraInfos()
        {
            return Enumerable.Empty<DescriptorInfo>();
        }
    }

    private sealed class TestRewardSelectionCardView : RewardSelectionCardViewBase
    {
        private readonly UniTaskCompletionSource selectedSubmitCompletion = new();
        private readonly UniTaskCompletionSource rejectedSubmitCompletion = new();

        protected override RewardOptionKind ExpectedKind => RewardOptionKind.UpgradeCard;

        public int SelectedSubmitCount { get; private set; }
        public int RejectedSubmitCount { get; private set; }

        public override async UniTask PlaySelectedSubmitAsync(CancellationToken cancellationToken)
        {
            SelectedSubmitCount++;
            await selectedSubmitCompletion.Task.AttachExternalCancellation(cancellationToken);
        }

        public override async UniTask PlayRejectedSubmitAsync(float startDelay, CancellationToken cancellationToken)
        {
            RejectedSubmitCount++;
            await rejectedSubmitCompletion.Task.AttachExternalCancellation(cancellationToken);
        }

        public void CompleteSelectedSubmit()
        {
            selectedSubmitCompletion.TrySetResult();
        }

        public void CompleteRejectedSubmit()
        {
            rejectedSubmitCompletion.TrySetResult();
        }
    }
}
