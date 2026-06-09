using System;
using System.Collections.Generic;
using NUnit.Framework;
using Orange.Extraction;

public sealed class WeightedExtractionPoolTests
{
    [Test]
    public void TryDrawOne_UsesDeterministicWeightedSelection()
    {
        TestItem first = new TestItem("first");
        TestItem second = new TestItem("second");
        WeightedExtractionPool<TestItem, TestContext> pool = new WeightedExtractionPool<TestItem, TestContext>(
            new[]
            {
                new WeightedExtractionEntry<TestItem, TestContext>("first", first, 1f),
                new WeightedExtractionEntry<TestItem, TestContext>("second", second, 3f)
            },
            new SequenceExtractionRandom(0.5f));

        bool wasDrawn = pool.TryDrawOne(TestContext.Default, out ExtractionResult<TestItem> result);

        Assert.That(wasDrawn, Is.True);
        Assert.That(result.Item, Is.SameAs(second));
        Assert.That(result.EntryId, Is.EqualTo("second"));
        Assert.That(result.BaseWeight, Is.EqualTo(3f));
        Assert.That(result.FinalWeight, Is.EqualTo(3f));
        Assert.That(result.TotalWeight, Is.EqualTo(4d));
        Assert.That(result.RollValue, Is.EqualTo(2d));
    }

    [Test]
    public void Evaluate_AppliesWeightModifiersAndMarksZeroWeightCandidates()
    {
        TestItem suppressed = new TestItem("suppressed");
        TestItem boosted = new TestItem("boosted");
        WeightedExtractionPool<TestItem, TestContext> pool = new WeightedExtractionPool<TestItem, TestContext>();
        pool.AddEntry("suppressed", suppressed, 2f, weightModifier: new FixedWeightModifier(0f));
        pool.AddEntry("boosted", boosted, 2f, weightModifier: new ContextMultiplierModifier());

        ExtractionEvaluation<TestItem> evaluation = pool.Evaluate(new TestContext(3f, true));

        Assert.That(evaluation.TotalWeight, Is.EqualTo(6d));
        Assert.That(evaluation.Candidates[0].Status, Is.EqualTo(ExtractionCandidateStatus.ZeroWeight));
        Assert.That(evaluation.Candidates[0].FinalWeight, Is.EqualTo(0f));
        Assert.That(evaluation.Candidates[1].Status, Is.EqualTo(ExtractionCandidateStatus.Drawable));
        Assert.That(evaluation.Candidates[1].FinalWeight, Is.EqualTo(6f));
    }

    [Test]
    public void Evaluate_SeparatesIneligibleCandidatesFromZeroWeightCandidates()
    {
        WeightedExtractionPool<TestItem, TestContext> pool = new WeightedExtractionPool<TestItem, TestContext>();
        pool.AddEntry(
            "blocked",
            new TestItem("blocked"),
            1f,
            (entry, context) => context.AllowBlocked);
        pool.AddEntry("zero", new TestItem("zero"), 0f);
        pool.AddEntry("drawable", new TestItem("drawable"), 1f);

        ExtractionEvaluation<TestItem> evaluation = pool.Evaluate(new TestContext(1f, false));

        Assert.That(evaluation.Candidates[0].Status, Is.EqualTo(ExtractionCandidateStatus.Ineligible));
        Assert.That(evaluation.Candidates[1].Status, Is.EqualTo(ExtractionCandidateStatus.ZeroWeight));
        Assert.That(evaluation.Candidates[2].Status, Is.EqualTo(ExtractionCandidateStatus.Drawable));
        Assert.That(evaluation.TotalWeight, Is.EqualTo(1d));
    }

    [Test]
    public void DrawManyUnique_DoesNotReturnDuplicateEntries()
    {
        WeightedExtractionPool<TestItem, TestContext> pool = new WeightedExtractionPool<TestItem, TestContext>(
            new[]
            {
                new WeightedExtractionEntry<TestItem, TestContext>("first", new TestItem("first"), 1f),
                new WeightedExtractionEntry<TestItem, TestContext>("second", new TestItem("second"), 1f),
                new WeightedExtractionEntry<TestItem, TestContext>("third", new TestItem("third"), 1f)
            },
            new SequenceExtractionRandom(0f, 0f, 0f));

        IReadOnlyList<ExtractionResult<TestItem>> results = pool.DrawManyUnique(TestContext.Default, 3);

        Assert.That(results.Count, Is.EqualTo(3));
        Assert.That(results[0].EntryId, Is.EqualTo("first"));
        Assert.That(results[1].EntryId, Is.EqualTo("second"));
        Assert.That(results[2].EntryId, Is.EqualTo("third"));
    }

    [Test]
    public void DrawManyUnique_ReturnsAvailableResultsWhenCandidatesRunOut()
    {
        WeightedExtractionPool<TestItem, TestContext> pool = new WeightedExtractionPool<TestItem, TestContext>();
        pool.AddEntry("available", new TestItem("available"), 1f);
        pool.AddEntry(
            "blocked",
            new TestItem("blocked"),
            10f,
            (entry, context) => false);

        IReadOnlyList<ExtractionResult<TestItem>> results = pool.DrawManyUnique(TestContext.Default, 3);

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].EntryId, Is.EqualTo("available"));
    }

    [Test]
    public void EmptyZeroWeightAndIneligiblePoolsFailWithoutThrowing()
    {
        WeightedExtractionPool<TestItem, TestContext> emptyPool = new WeightedExtractionPool<TestItem, TestContext>();
        WeightedExtractionPool<TestItem, TestContext> zeroPool = new WeightedExtractionPool<TestItem, TestContext>(
            new[] { new WeightedExtractionEntry<TestItem, TestContext>("zero", new TestItem("zero"), 0f) });
        WeightedExtractionPool<TestItem, TestContext> blockedPool = new WeightedExtractionPool<TestItem, TestContext>();
        blockedPool.AddEntry("blocked", new TestItem("blocked"), 1f, (entry, context) => false);

        Assert.That(emptyPool.TryDrawOne(TestContext.Default, out ExtractionResult<TestItem> emptyResult), Is.False);
        Assert.That(emptyResult, Is.Null);
        Assert.That(zeroPool.TryDrawOne(TestContext.Default, out ExtractionResult<TestItem> zeroResult), Is.False);
        Assert.That(zeroResult, Is.Null);
        Assert.That(blockedPool.TryDrawOne(TestContext.Default, out ExtractionResult<TestItem> blockedResult), Is.False);
        Assert.That(blockedResult, Is.Null);
    }

    [Test]
    public void InvalidInputsThrowDiagnosableExceptions()
    {
        TestItem item = new TestItem("item");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WeightedExtractionEntry<TestItem, TestContext>("negative", item, -1f));
        Assert.Throws<ArgumentException>(() =>
            new WeightedExtractionEntry<TestItem, TestContext>("nan", item, float.NaN));
        Assert.Throws<ArgumentNullException>(() =>
            new WeightedExtractionEntry<TestItem, TestContext>("null-item", null, 1f));
        Assert.Throws<ArgumentNullException>(() =>
            new WeightedExtractionPool<TestItem, TestContext>((IEnumerable<TestItem>)null));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WeightedExtractionPool<TestItem, TestContext>(Array.Empty<TestItem>(), -1f));
    }

    [Test]
    public void NonFiniteModifierOutputThrows()
    {
        WeightedExtractionPool<TestItem, TestContext> pool = new WeightedExtractionPool<TestItem, TestContext>();
        pool.AddEntry("bad", new TestItem("bad"), 1f, weightModifier: new FixedWeightModifier(float.PositiveInfinity));

        Assert.Throws<InvalidOperationException>(() => pool.Evaluate(TestContext.Default));
    }

    [Test]
    public void NegativeModifierOutputIsClampedToZero()
    {
        WeightedExtractionPool<TestItem, TestContext> pool = new WeightedExtractionPool<TestItem, TestContext>();
        pool.AddEntry("negative", new TestItem("negative"), 1f, weightModifier: new FixedWeightModifier(-10f));

        ExtractionEvaluation<TestItem> evaluation = pool.Evaluate(TestContext.Default);

        Assert.That(evaluation.Candidates[0].Status, Is.EqualTo(ExtractionCandidateStatus.ZeroWeight));
        Assert.That(evaluation.Candidates[0].FinalWeight, Is.EqualTo(0f));
    }

    [Test]
    public void SingleGenericPoolSupportsContextFreeDraws()
    {
        TestItem first = new TestItem("first");
        TestItem second = new TestItem("second");
        WeightedExtractionPool<TestItem> pool = new WeightedExtractionPool<TestItem>(
            new[] { first, second },
            1f,
            new SequenceExtractionRandom(0.75f));

        bool wasDrawn = pool.TryDrawOne(out ExtractionResult<TestItem> result);

        Assert.That(wasDrawn, Is.True);
        Assert.That(result.Item, Is.SameAs(second));
    }

    [Test]
    public void BusinessPoolCanInheritAndInitializeFromItems()
    {
        TestItem first = new TestItem("first");
        TestItem second = new TestItem("second");
        TestItemPool pool = new TestItemPool(new[] { first, second }, new SequenceExtractionRandom(0f));

        ExtractionEvaluation<TestItem> evaluation = pool.Evaluate(TestContext.Default);

        Assert.That(pool.Count, Is.EqualTo(2));
        Assert.That(evaluation.Candidates[0].EntryId, Is.EqualTo("first"));
        Assert.That(evaluation.Candidates[0].BaseWeight, Is.EqualTo(TestItemPool.DefaultWeight));
    }

    private readonly struct TestContext
    {
        public static readonly TestContext Default = new TestContext(1f, true);

        public TestContext(float multiplier, bool allowBlocked)
        {
            Multiplier = multiplier;
            AllowBlocked = allowBlocked;
        }

        public float Multiplier { get; }
        public bool AllowBlocked { get; }
    }

    private sealed class TestItem
    {
        public TestItem(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public override string ToString()
        {
            return Id;
        }
    }

    private sealed class TestItemPool : WeightedExtractionPool<TestItem, TestContext>
    {
        public const float DefaultWeight = 2f;

        public TestItemPool(IEnumerable<TestItem> items, IExtractionRandom random)
            : base(items, DefaultWeight, random)
        {
        }
    }

    private sealed class ContextMultiplierModifier : IExtractionWeightModifier<TestItem, TestContext>
    {
        public float ModifyWeight(WeightedExtractionEntry<TestItem, TestContext> entry, float baseWeight, TestContext context)
        {
            return baseWeight * context.Multiplier;
        }
    }

    private sealed class FixedWeightModifier : IExtractionWeightModifier<TestItem, TestContext>
    {
        private readonly float value;

        public FixedWeightModifier(float value)
        {
            this.value = value;
        }

        public float ModifyWeight(WeightedExtractionEntry<TestItem, TestContext> entry, float baseWeight, TestContext context)
        {
            return value;
        }
    }

    private sealed class SequenceExtractionRandom : IExtractionRandom
    {
        private readonly float[] values;
        private int index;

        public SequenceExtractionRandom(params float[] values)
        {
            this.values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public float NextNormalizedValue()
        {
            if (values.Length == 0)
            {
                return 0f;
            }

            int currentIndex = Math.Min(index, values.Length - 1);
            index++;
            return values[currentIndex];
        }
    }
}
