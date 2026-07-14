using NUnit.Framework;

public sealed class ShopBoardTests
{
    [Test]
    public void Flow_OnlyAllowsCommandsFromReadyStage()
    {
        ShopBoard board = new();

        Assert.IsFalse(board.TryBeginReroll(0, 10));
        Assert.IsTrue(board.TryBeginVisit(1));
        Assert.IsFalse(board.TryBeginReroll(0, 10));
        Assert.IsTrue(board.CompleteVisitOpening());
        Assert.IsTrue(board.TryBeginReroll(0, 10));
        Assert.IsFalse(board.TryBeginClosing());
        Assert.IsTrue(board.CompleteOperation());
        Assert.IsTrue(board.TryBeginClosing());
        Assert.IsTrue(board.CompleteClosing());
        Assert.AreEqual(ShopFlowStage.Closed, board.Stage);
    }

    [Test]
    public void Board_AggregatesVisitPriceModifiers()
    {
        ShopBoard board = new();
        board.TryBeginVisit(1);

        board.SetVisitPriceMultiplier("coupon", 0.5f);
        board.SetVisitPriceMultiplier("sale", 0.8f);

        Assert.AreEqual(40, board.ApplyVisitPriceModifiers(100));
        board.RemoveVisitPriceMultiplier("coupon");
        Assert.AreEqual(80, board.ApplyVisitPriceModifiers(100));
    }

    [Test]
    public void Board_PrefersVisitFreeRerollBeforeAttributeFreeReroll()
    {
        ShopBoard board = new();
        board.TryBeginVisit(1);
        board.CompleteVisitOpening();
        board.GrantVisitFreeRerolls(1);

        Assert.IsTrue(board.TryBeginReroll(2, 20));

        Assert.AreEqual(ShopFreeRerollSource.Visit, board.CurrentOperation.FreeRerollSource);
        Assert.IsTrue(board.TryConsumeVisitFreeReroll());
    }

    [Test]
    public void Board_TracksPaidRerollsPerVisit()
    {
        ShopBoard board = new();
        board.TryBeginVisit(1);
        board.CompleteVisitOpening();
        board.TryBeginReroll(0, 10);

        board.RecordPaidReroll();

        Assert.AreEqual(1, board.PaidRerollCountThisVisit);
        Assert.IsTrue(board.CompleteOperation());
        Assert.IsTrue(board.TryBeginClosing());
        Assert.IsTrue(board.CompleteClosing());
        Assert.IsTrue(board.TryBeginVisit(2));
        Assert.AreEqual(0, board.PaidRerollCountThisVisit);
    }
}
