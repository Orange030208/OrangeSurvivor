using System;

public sealed class GamePadUIContext
{
    public GamePadUIContext(Player player)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
    }

    public Player Player { get; }
}
