public readonly struct WaveCompletionDecision
{
    public bool CompleteWave { get; }
    public bool StopTimer { get; }
    public string DiagnosticError { get; }
    public bool HasDiagnosticError => !string.IsNullOrWhiteSpace(DiagnosticError);

    public WaveCompletionDecision(bool completeWave, bool stopTimer, string diagnosticError = null)
    {
        CompleteWave = completeWave;
        StopTimer = stopTimer;
        DiagnosticError = diagnosticError;
    }

    public static WaveCompletionDecision None => new WaveCompletionDecision(false, false);
    public static WaveCompletionDecision Complete => new WaveCompletionDecision(true, false);
    public static WaveCompletionDecision StopWithoutCompletion => new WaveCompletionDecision(false, true);

    public static WaveCompletionDecision CompleteWithStoppedTimer(string diagnosticError = null)
    {
        return new WaveCompletionDecision(true, true, diagnosticError);
    }
}
