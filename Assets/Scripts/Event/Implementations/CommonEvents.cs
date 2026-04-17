using UnityEngine;

public struct GameStateChangedEvent : IGameEvent
{
    public GameState OldState;
    public GameState NewState;

    public GameStateChangedEvent(GameState oldState, GameState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}

public struct PauseGameRequestedEvent : IGameEvent
{
}

public struct MenuStartClickedEvent : IGameEvent
{
}

public struct PlayerSpawnedEvent : IGameEvent
{
    public Player Player;

    public PlayerSpawnedEvent(Player player)
    {
        Player = player;
    }
}

public struct GameOverRestartClickedEvent : IGameEvent
{
}

public struct GameOverReturnToMenuClickedEvent : IGameEvent
{
}

public struct StageCompleteRestartClickedEvent : IGameEvent
{
}

public struct StageCompleteReturnToMenuClickedEvent : IGameEvent
{
}

public struct RequestStageCompleteSnapshotEvent : IGameEvent
{
}

public struct StageCompleteSnapshotEvent : IGameEvent
{
    public int CompletedWaves;
    public float SurvivalTime;
    public int KillCount;
    public int GoldEarned;
    public string CharacterName;
    public string MainWeaponName;

    public StageCompleteSnapshotEvent(int completedWaves, float survivalTime, int killCount, int goldEarned, string characterName, string mainWeaponName)
    {
        CompletedWaves = completedWaves;
        SurvivalTime = survivalTime;
        KillCount = killCount;
        GoldEarned = goldEarned;
        CharacterName = characterName;
        MainWeaponName = mainWeaponName;
    }
}

public struct EntityDamagedEvent : IGameEvent
{
    public Entity Entity;
    public HitResult HitResult;

    public EntityDamagedEvent(Entity entity, HitResult hitResult)
    {
        Entity = entity;
        HitResult = hitResult;
    }
}

public struct EntityDiedEvent : IGameEvent
{
    public Entity Entity;
    public Vector2 Position;

    public EntityDiedEvent(Entity entity, Vector2 position)
    {
        Entity = entity;
        Position = position;
    }
}

// 暂停菜单页面内部点击“继续”后的意图事件，交给 GameManager 统一处理。
public struct PauseMenuContinueClickedEvent : IGameEvent
{
}

// 暂停菜单页面内部点击“返回菜单”后的意图事件，交给 GameManager 统一处理。
public struct PauseMenuReturnToMenuClickedEvent : IGameEvent
{
}
