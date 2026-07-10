public interface IHeartSteelStackGainHandler
{
    bool AppliesTo(string weaponId);
    void OnHeartSteelStacksGained(HeartSteelStackGainContext context);
}
