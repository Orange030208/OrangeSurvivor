using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class ContentPoolV2Tests
{
    [Test]
    public void Roll_UsesDeterministicWeightedSelection()
    {
        TestContentAsset first = CreateContent("first");
        TestContentAsset second = CreateContent("second");
        ContentRollRequest request = new(
            new[]
            {
                new ContentPoolEntryDefinition(first, 1f, "first"),
                new ContentPoolEntryDefinition(second, 3f, "second")
            },
            new ContentRollScope(ContentPoolScopeIds.Generic, "test"),
            1,
            false,
            random: new SequenceContentRandom(0.5f));

        ContentRollOutcome outcome = new ContentPoolServiceV2(modifierSource: NullContentModifierSource.Instance)
            .Roll(request);

        Assert.That(outcome.HasAny, Is.True);
        Assert.That(outcome.Selections[0].Content, Is.SameAs(second));
    }

    [Test]
    public void Roll_RespectsDuplicateAndMutualExclusionRules()
    {
        TestContentAsset first = CreateContent("first");
        TestContentAsset second = CreateContent("second");
        TestContentAsset third = CreateContent("third");
        ContentRollRequest request = new(
            new[]
            {
                new ContentPoolEntryDefinition(
                    first,
                    1f,
                    "first",
                    mutuallyExclusiveEntryIds: new[] { "second" }),
                new ContentPoolEntryDefinition(second, 1f, "second"),
                new ContentPoolEntryDefinition(third, 1f, "third")
            },
            new ContentRollScope(ContentPoolScopeIds.Generic, "test"),
            3,
            false,
            random: new SequenceContentRandom(0f, 0f, 0f));

        ContentRollOutcome outcome = new ContentPoolServiceV2(modifierSource: NullContentModifierSource.Instance)
            .Roll(request);

        Assert.That(outcome.Selections.Count, Is.EqualTo(2));
        Assert.That(outcome.Selections[0].EntryId, Is.EqualTo("first"));
        Assert.That(outcome.Selections[1].EntryId, Is.EqualTo("third"));
    }

    [Test]
    public void RunContentHistory_IsSharedByRunButIsolatedByScope()
    {
        TestContentAsset content = CreateContent("shared");
        ContentPoolEntryDefinition entry = new(
            content,
            1f,
            "shared",
            maxPickCount: 1);
        RunContentHistory history = new();
        ContentRollScope shopScope = new(ContentPoolScopeIds.Shop, "pool", "player");
        ContentRollScope rewardScope = new(ContentPoolScopeIds.ChestReward, "pool", "player");
        ContentPoolServiceV2 service = new(modifierSource: NullContentModifierSource.Instance);

        ContentRollOutcome firstShopRoll = service.Roll(new ContentRollRequest(
            new[] { entry },
            shopScope,
            1,
            false,
            history,
            random: new SequenceContentRandom(0f)));
        history.RecordPick(shopScope, firstShopRoll.Selections[0]);

        ContentRollOutcome secondShopRoll = service.Roll(new ContentRollRequest(
            new[] { entry },
            shopScope,
            1,
            false,
            history,
            random: new SequenceContentRandom(0f)));
        ContentRollOutcome rewardRoll = service.Roll(new ContentRollRequest(
            new[] { entry },
            rewardScope,
            1,
            false,
            history,
            random: new SequenceContentRandom(0f)));

        Assert.That(secondShopRoll.HasAny, Is.False);
        Assert.That(rewardRoll.HasAny, Is.True);
    }

    [Test]
    public void Roll_RespectsMaxRollCountWithinCurrentOutcomeAndHistory()
    {
        TestContentAsset content = CreateContent("limited");
        ContentPoolEntryDefinition entry = new(
            content,
            1f,
            "limited",
            maxRollCount: 1);
        RunContentHistory history = new();
        ContentRollScope scope = new(ContentPoolScopeIds.Generic, "test");
        ContentPoolServiceV2 service = new(modifierSource: NullContentModifierSource.Instance);

        ContentRollOutcome firstRoll = service.Roll(new ContentRollRequest(
            new[] { entry },
            scope,
            3,
            true,
            history,
            random: new SequenceContentRandom(0f, 0f, 0f)));
        ContentRollOutcome secondRoll = service.Roll(new ContentRollRequest(
            new[] { entry },
            scope,
            1,
            true,
            history,
            random: new SequenceContentRandom(0f)));

        Assert.That(firstRoll.Selections.Count, Is.EqualTo(1));
        Assert.That(secondRoll.HasAny, Is.False);
    }

    [Test]
    public void RunContentHistory_TracksPreviousOfferForLastRoll()
    {
        TestContentAsset first = CreateContent("first");
        TestContentAsset second = CreateContent("second");
        RunContentHistory history = new();
        ContentRollScope scope = new(ContentPoolScopeIds.Generic, "test");
        ContentPoolServiceV2 service = new(modifierSource: NullContentModifierSource.Instance);

        service.Roll(new ContentRollRequest(
            new[]
            {
                new ContentPoolEntryDefinition(first, 1f, "first"),
                new ContentPoolEntryDefinition(second, 1f, "second")
            },
            scope,
            2,
            false,
            history,
            random: new SequenceContentRandom(0f, 0f)));

        Assert.That(history.WasPreviouslyOffered(scope, "first"), Is.True);
        Assert.That(history.WasPreviouslyOffered(scope, "second"), Is.True);
    }

    [Test]
    public void LegacyAdapter_MatchesLegacyRollServiceForSimplePool()
    {
        TestContentAsset first = CreateContent("first");
        TestContentAsset second = CreateContent("second");
        ContentPoolEntry[] legacyEntries =
        {
            new(first, 1f, "first"),
            new(second, 3f, "second")
        };
        ContentPoolSO pool = ScriptableObject.CreateInstance<ContentPoolSO>();
        pool.Initialize(legacyEntries, 1, false);
        ContentRollContext legacyContext = new(ContentPoolScopeIds.Generic);
        SequenceContentRandom legacyRandom = new(0.5f);
        SequenceContentRandom v2Random = new(0.5f);

        ContentRollResult legacyResult = new ContentPoolRollService(legacyRandom)
            .Roll(pool, legacyContext, 1);
        ContentRollRequest request = LegacyContentPoolAdapter.CreateRequest(
            pool,
            legacyContext,
            new ContentRollScope(ContentPoolScopeIds.Generic, pool.name),
            1,
            random: v2Random);
        ContentRollOutcome v2Outcome = new ContentPoolServiceV2(v2Random, NullContentModifierSource.Instance)
            .Roll(request);

        Assert.That(v2Outcome.HasAny, Is.EqualTo(legacyResult.HasAny));
        Assert.That(v2Outcome.Selections[0].Content, Is.SameAs(legacyResult.Items[0].Content));
    }

    [Test]
    public void Roll_ReturnsEmptyOutcomeForEmptyOrZeroWeightEntries()
    {
        TestContentAsset content = CreateContent("zero");
        ContentRollRequest request = new(
            new[] { new ContentPoolEntryDefinition(content, 0f, "zero") },
            new ContentRollScope(ContentPoolScopeIds.Generic, "test"),
            1,
            false,
            random: new SequenceContentRandom(0f));

        ContentRollOutcome outcome = new ContentPoolServiceV2(modifierSource: NullContentModifierSource.Instance)
            .Roll(request);

        Assert.That(outcome.HasAny, Is.False);
    }

    private static TestContentAsset CreateContent(string name)
    {
        TestContentAsset asset = ScriptableObject.CreateInstance<TestContentAsset>();
        asset.name = name;
        return asset;
    }

    private sealed class TestContentAsset : ScriptableObject
    {
    }

    private sealed class SequenceContentRandom : IContentRandom
    {
        private readonly Queue<float> values = new();

        public SequenceContentRandom(params float[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                this.values.Enqueue(values[i]);
            }
        }

        public float Value01()
        {
            return values.Count > 0 ? values.Dequeue() : 0f;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return minInclusive;
        }
    }
}
