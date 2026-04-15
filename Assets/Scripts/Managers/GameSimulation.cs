using UnityEngine;

public static class GameSimulation
{
    public static bool IsRunning
    {
        get
        {
            GameManager manager = GameManager.Instance;
            return manager != null && manager.IsSimulationRunning;
        }
    }
}
