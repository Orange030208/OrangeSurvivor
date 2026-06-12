using UnityEngine;

public struct GameStateChangedEvent
{
    public GameState OldState;
    public GameState NewState;

    public GameStateChangedEvent(GameState oldState, GameState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}

public enum GameFlowCommand
{
    MenuStartClicked,
    ShopContinueClicked,
    GameOverRestartClicked,
    GameOverReturnToMenuClicked,
    StageCompleteRestartClicked,
    StageCompleteReturnToMenuClicked,
    PauseRequested,
    PauseMenuContinueClicked,
    PauseMenuReturnToMenuClicked
}

public struct PlayerSpawnedEvent
{
    public Player Player;

    public PlayerSpawnedEvent(Player player)
    {
        Player = player;
    }
}


public struct EntityDamagedEvent
{
    public Entity Entity;
    public HitResult HitResult;

    public EntityDamagedEvent(Entity entity, HitResult hitResult)
    {
        Entity = entity;
        HitResult = hitResult;
    }
}

public enum EntityDeathReason
{
    Combat = 0,
    WaveCleanup = 1
}

public struct EntityDiedEvent
{
    public Entity Entity;
    public Vector2 Position;
    public Entity Source;
    public EntityDeathReason Reason;

    public EntityDiedEvent(Entity entity, Vector2 position)
        : this(entity, position, null, EntityDeathReason.Combat)
    {
    }

    public EntityDiedEvent(Entity entity, Vector2 position, Entity source)
        : this(entity, position, source, EntityDeathReason.Combat)
    {
    }

    public EntityDiedEvent(Entity entity, Vector2 position, Entity source, EntityDeathReason reason)
    {
        Entity = entity;
        Position = position;
        Source = source;
        Reason = reason;
    }
}
