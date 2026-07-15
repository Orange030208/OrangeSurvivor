using NUnit.Framework;

public sealed class ShopBoardTests
{
    [Test]
    public void Flow_OnlyAllowsCommandsFromReadyStage()
    {
        ShopBoard board = new();

        Assert.IsFalse(board.TryBeginReroll(false, 10));
        Assert.IsTrue(board.TryBeginVisit(1));
        Assert.IsFalse(board.TryBeginReroll(false, 10));
        Assert.IsTrue(board.CompleteVisitOpening());
        Assert.IsTrue(board.TryBeginReroll(false, 10));
        Assert.IsFalse(board.TryBeginClosing());
        Assert.IsTrue(board.CompleteOperation());
        Assert.IsTrue(board.TryBeginClosing());
        Assert.IsTrue(board.CompleteClosing());
        Assert.AreEqual(ShopFlowStage.Closed, board.Stage);
    }

    [Test]
    public void Board_ClearsGlobalPriceModifiersWhenVisitEnds()
    {
        ShopBoard board = new();
        board.TryBeginVisit(1);

        board.SetPriceModifier("coupon", 0.5f);
        board.SetPriceModifier("coupon", 0.8f);
        board.SetPriceModifier("sale", 0.5f);

        Assert.IsTrue(board.TryGetPriceModifier("coupon", out float couponMultiplier));
        Assert.AreEqual(0.8f, couponMultiplier);
        Assert.AreEqual(0.4f, board.GetPriceModifierMultiplier());
        Assert.IsTrue(board.CompleteVisitOpening());
        Assert.IsTrue(board.TryBeginClosing());
        Assert.IsTrue(board.CompleteClosing());
        Assert.IsFalse(board.TryGetPriceModifier("coupon", out _));
    }

    [Test]
    public void PriceCalculation_AggregatesGlobalAndOfferModifiers()
    {
        int price = ShopPricingService.ApplyPriceMultiplier(
            100,
            1f,
            1f,
            0.5f,
            0.8f);

        Assert.AreEqual(40, price);
    }

    [Test]
    public void Board_RecordsFreeRerollForCurrentStage()
    {
        ShopBoard board = new();
        board.TryBeginVisit(1);
        board.CompleteVisitOpening();
        board.GrantVisitFreeRerolls(1);

        Assert.IsTrue(board.TryBeginReroll(true, 20));

        Assert.IsTrue(board.IsCurrentRerollFree);
        Assert.IsTrue(board.TryConsumeVisitFreeReroll());
        Assert.IsTrue(board.CompleteOperation());
        Assert.IsFalse(board.IsCurrentRerollFree);
        Assert.AreEqual(0, board.CurrentRerollCost);
    }

    [Test]
    public void Board_TracksPaidRerollsPerVisit()
    {
        ShopBoard board = new();
        board.TryBeginVisit(1);
        board.CompleteVisitOpening();
        board.TryBeginReroll(false, 10);

        board.RecordPaidReroll();

        Assert.AreEqual(1, board.PaidRerollCountThisVisit);
        Assert.IsTrue(board.CompleteOperation());
        Assert.IsTrue(board.TryBeginClosing());
        Assert.IsTrue(board.CompleteClosing());
        Assert.IsTrue(board.TryBeginVisit(2));
        Assert.AreEqual(0, board.PaidRerollCountThisVisit);
    }
}
