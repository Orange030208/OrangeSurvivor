using System;

public sealed class GamePauseMenuContext
{
    public GamePauseMenuContext(Player player, AttributeManager AttributeManager)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        AttributeManager = AttributeManager ?? throw new ArgumentNullException(nameof(AttributeManager));
    }

    public Player Player { get; }
    public AttributeManager AttributeManager { get; }
}
