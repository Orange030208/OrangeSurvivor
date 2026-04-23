using System;

public interface IPlayerHudFacade : IDisposable
{
    void Activate();
    void Deactivate();
    void RequestWaveSnapshot();
    void RequestPlayerLevelSnapshot();
}
