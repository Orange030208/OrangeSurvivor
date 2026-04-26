using System.Collections.Generic;

public interface IInitialAccessoryProvider
{
    IReadOnlyList<AccessoryDataSO> InitialAccessories { get; }
}