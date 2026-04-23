using UnityEngine;

public static class GameSimulation
{
    private static bool manualOverrideEnabled;
    private static bool manualOverrideIsRunning;

    public static bool IsRunning
    {
        get
        {
            if (manualOverrideEnabled)
            {
                return manualOverrideIsRunning;
            }

            GameManager manager = GameManager.Instance;
            return manager != null && manager.IsSimulationRunning;
        }
    }

    public static void SetManualOverride(bool isRunning)
    {
        manualOverrideEnabled = true;
        manualOverrideIsRunning = isRunning;
    }

    public static void ClearManualOverride()
    {
        manualOverrideEnabled = false;
        manualOverrideIsRunning = false;
    }
}
