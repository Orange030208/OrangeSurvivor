using UnityEngine;
using UnityEngine.EventSystems;

public static class InputRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureSceneInput()
    {
        GameInputService.EnsureInstance();
        EventSystem eventSystem = EventSystem.current ?? Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            GameInputService.ConfigureEventSystem(eventSystem);
        }
    }
}
