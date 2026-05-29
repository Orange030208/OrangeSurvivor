using System;

public sealed class GamePauseMenuContext
{
    public GamePauseMenuContext(Player player, PropertiesManager propertiesManager)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        PropertiesManager = propertiesManager ?? throw new ArgumentNullException(nameof(propertiesManager));
    }

    public Player Player { get; }
    public PropertiesManager PropertiesManager { get; }
}
