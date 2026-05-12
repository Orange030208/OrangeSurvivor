using NUnit.Framework;

public sealed class WaveCompletionRuleTests
{
    [Test]
    public void TimerOnlyCompletesWhenTimerElapsedAndKeepsCountdownPresentation()
    {
        IWaveCompletionRule rule = new TimerOnlyWaveCompletionRule();

        WaveCompletionDecision decision = rule.OnTimerElapsed(CreateContext());

        Assert.IsTrue(decision.CompleteWave);
        Assert.IsFalse(decision.StopTimer);
        Assert.IsFalse(decision.HasDiagnosticError);
        Assert.IsTrue(rule.ShowsCountdownTimer);
        Assert.IsTrue(rule.PlaysCountdownWarning);
    }

    [Test]
    public void BossDefeatedCompletesAfterRegisteredBossDies()
    {
        IWaveCompletionRule rule = new BossDefeatedWaveCompletionRule();
        WaveCompletionContext context = CreateContext();

        WaveCompletionDecision registeredDecision = rule.OnEnemyRegistered(EnemyRole.Boss, context);
        WaveCompletionDecision deathDecision = rule.OnEnemyDied(EnemyRole.Boss, context);

        Assert.IsFalse(registeredDecision.CompleteWave);
        Assert.IsFalse(registeredDecision.StopTimer);
        Assert.IsTrue(deathDecision.CompleteWave);
        Assert.IsFalse(deathDecision.HasDiagnosticError);
        Assert.IsFalse(rule.ShowsCountdownTimer);
        Assert.IsFalse(rule.PlaysCountdownWarning);
    }

    [Test]
    public void BossDefeatedStopsTimerButWaitsForBossWhenTimerElapsedAfterBossSpawned()
    {
        IWaveCompletionRule rule = new BossDefeatedWaveCompletionRule();
        WaveCompletionContext context = CreateContext();

        rule.OnEnemyRegistered(EnemyRole.Boss, context);
        WaveCompletionDecision decision = rule.OnTimerElapsed(context);

        Assert.IsFalse(decision.CompleteWave);
        Assert.IsTrue(decision.StopTimer);
        Assert.IsFalse(decision.HasDiagnosticError);
    }

    [Test]
    public void BossDefeatedCompletesWithDiagnosticWhenTimerElapsedBeforeBossSpawned()
    {
        IWaveCompletionRule rule = new BossDefeatedWaveCompletionRule();

        WaveCompletionDecision decision = rule.OnTimerElapsed(CreateContext());

        Assert.IsTrue(decision.CompleteWave);
        Assert.IsTrue(decision.StopTimer);
        Assert.IsTrue(decision.HasDiagnosticError);
        StringAssert.Contains("requires boss defeat", decision.DiagnosticError);
    }

    [Test]
    public void BossDefeatedIgnoresNonBossEnemyEvents()
    {
        IWaveCompletionRule rule = new BossDefeatedWaveCompletionRule();
        WaveCompletionContext context = CreateContext();

        WaveCompletionDecision registeredDecision = rule.OnEnemyRegistered(EnemyRole.Normal, context);
        WaveCompletionDecision deathDecision = rule.OnEnemyDied(EnemyRole.Normal, context);

        Assert.IsFalse(registeredDecision.CompleteWave);
        Assert.IsFalse(registeredDecision.StopTimer);
        Assert.IsFalse(deathDecision.CompleteWave);
        Assert.IsFalse(deathDecision.StopTimer);
    }

    [Test]
    public void FactoryFallsBackToTimerOnlyForUnknownCompletionMode()
    {
        IWaveCompletionRule rule = WaveCompletionRuleFactory.Create((WaveCompletionMode)999);

        Assert.IsInstanceOf<TimerOnlyWaveCompletionRule>(rule);
        Assert.IsTrue(rule.ShowsCountdownTimer);
        Assert.IsTrue(rule.PlaysCountdownWarning);
    }

    [Test]
    public void ProgressAndHudPayloadsCanCarryRuleTimerVisibility()
    {
        IWaveCompletionRule bossRule = WaveCompletionRuleFactory.Create(WaveCompletionMode.BossDefeated);

        WaveProgressEvent progressEvent = new WaveProgressEvent(30f, 30f, bossRule.ShowsCountdownTimer);
        WaveHudViewData hudViewData = new WaveHudViewData(10, 20, true, 30f, 30f, bossRule.ShowsCountdownTimer);

        Assert.IsFalse(progressEvent.ShowTimer);
        Assert.IsFalse(hudViewData.ShowTimer);
    }

    private static WaveCompletionContext CreateContext()
    {
        return new WaveCompletionContext(
            waveIndex: 9,
            waveNumber: 10,
            elapsedTime: 30f,
            waveDuration: 30f);
    }
}
