public interface IHeartSteelDwellTuningProvider
{
    bool AppliesTo(string weaponId);
    HeartSteelDwellSettings Apply(HeartSteelDwellSettings settings);
}
