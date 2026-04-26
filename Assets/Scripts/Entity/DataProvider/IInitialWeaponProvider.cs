using System.Collections.Generic;

public interface IInitialWeaponProvider
{
    IReadOnlyList<WeaponEntry> InitialWeapons { get; }
}