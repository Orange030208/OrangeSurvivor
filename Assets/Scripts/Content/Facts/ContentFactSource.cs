using System.Collections.Generic;

public sealed class ContentFactSource
{
    public Player Player { get; set; }
    public PropertiesManager PropertiesManager { get; set; }
    public WeaponsHolder WeaponsHolder { get; set; }
    public UpgradeRunState UpgradeRunState { get; set; }
    public IReadOnlyList<WeaponDataSO> OwnedWeapons { get; set; }
    public CharacterDataSO CharacterData { get; set; }
    public int WaveNumber { get; set; } = 1;
    public string WaveId { get; set; }
    public string WaveTrackId { get; set; }
    public float WaveProgressPercent { get; set; }
    public int ShopRefreshCount { get; set; }
    public int ShopRerollCount { get; set; }
    public RunProgressionSnapshot ProgressionSnapshot { get; set; }

    public static ContentFactSource ForPlayer(Player player, int waveNumber = 1)
    {
        ContentFactSource source = new()
        {
            Player = player,
            WaveNumber = UnityEngine.Mathf.Max(1, waveNumber),
            ProgressionSnapshot = RunProgressionRuntime.CurrentSnapshot
        };

        if (player == null)
        {
            return source;
        }

        source.PropertiesManager = player.GetComponent<PropertiesManager>();
        source.WeaponsHolder = player.GetComponent<WeaponsHolder>();
        source.CharacterData = player.CharacterData;
        return source;
    }
}
