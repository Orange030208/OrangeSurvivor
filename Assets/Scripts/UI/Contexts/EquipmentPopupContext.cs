using System;

public sealed class EquipmentPopupContext
{
    public EquipmentPopupContext(
        WeaponsHolder weaponsHolder,
        AccessoryManager accessoryManager,
        CurrencyWallet currencyWallet)
    {
        WeaponsHolder = weaponsHolder ?? throw new ArgumentNullException(nameof(weaponsHolder));
        AccessoryManager = accessoryManager ?? throw new ArgumentNullException(nameof(accessoryManager));
        CurrencyWallet = currencyWallet ?? throw new ArgumentNullException(nameof(currencyWallet));
    }

    public WeaponsHolder WeaponsHolder { get; }
    public AccessoryManager AccessoryManager { get; }
    public CurrencyWallet CurrencyWallet { get; }
}
