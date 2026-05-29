public static class GameContentAssetPaths
{
    public const string Root = "Assets/GameContent";

    public const string CatalogFolder = Root + "/Catalog";
    public const string CatalogPools = CatalogFolder + "/Pools";
    public const string GameContentCatalog = CatalogFolder + "/Game Content Catalog.asset";
    public const string ChestRewardPool = CatalogPools + "/Chest Reward Pool.asset";
    public const string ShopPool = CatalogPools + "/Shop Pool.asset";
    public const string DropPool = CatalogPools + "/Drop Pool.asset";
    public const string WeaponRewardPool = CatalogPools + "/Weapon Reward Pool.asset";

    public const string RunProgression = Root + "/RunProgression";
    public const string RunProgressionProfile = RunProgression + "/Run Progression Profile.asset";

    public const string Characters = Root + "/Characters";
    public const string CharactersData = Characters + "/Data";
    public const string CharactersPrefabs = Characters + "/Prefabs";
    public const string CharactersSprites = Characters + "/Sprites";
    public const string CharactersAnimations = Characters + "/Animations";
    public const string PlayerLevelConfig = CharactersData + "/Player Level Config.asset";
    public const string DefaultPlayerPrefab = CharactersPrefabs + "/Character.prefab";

    public const string Weapons = Root + "/Weapons";
    public const string WeaponsData = Weapons + "/Data";
    public const string WeaponAttackSequence = WeaponsData + "/AttackSequence";
    public const string WeaponProjectileData = WeaponsData + "/Projectiles";
    public const string WeaponsPrefabs = Weapons + "/Prefabs";
    public const string WeaponProjectilePrefabs = WeaponsPrefabs + "/Projectiles";
    public const string WeaponsSprites = Weapons + "/Sprites";
    public const string WeaponProjectileSprites = WeaponsSprites + "/Projectiles";
    public const string WeaponProjectileAtlas = WeaponProjectileSprites + "/ProjectileCoreAtlas.png";
    public const string WeaponsAnimations = Weapons + "/Animations";
    public const string WeaponProjectileAnimations = WeaponsAnimations + "/Projectiles";
    public const string WeaponsVfx = Weapons + "/VFX";
    public const string WeaponDataList = WeaponsData + "/Weapon Data List.asset";
    public const string DefaultWeaponPrefab = WeaponsPrefabs + "/Weapon.prefab";

    public const string Accessories = Root + "/Accessories";
    public const string AccessoriesData = Accessories + "/Data";
    public const string AccessoriesSprites = Accessories + "/Sprites";
    public const string AccessoryDataList = AccessoriesData + "/Accessory Data List.asset";

    public const string Upgrades = Root + "/Upgrades";
    public const string UpgradeCards = Upgrades + "/Cards";
    public const string UpgradePools = Upgrades + "/Pools";
    public const string UpgradePresentation = Upgrades + "/Presentation";
    public const string UpgradeCardPool = UpgradePools + "/Upgrade Card Pool.asset";
    public const string CardQualityPresentationCatalog = UpgradePresentation + "/Upgrade Card Rarity Presentation Catalog.asset";

    public const string Waves = Root + "/Waves";
    public const string WavesData = Waves + "/Data";
    public const string WaveSpawnPacks = Waves + "/SpawnPacks";
    public const string WavePools = Waves + "/Pools";
    public const string StageDefinition = WavesData + "/Stage Definition.asset";
    public const string WaveSpawnPool = WavePools + "/Wave Spawn Pool.asset";

    public const string Enemies = Root + "/Enemies";
    public const string EnemiesData = Enemies + "/Data";
    public const string EnemiesPrefabs = Enemies + "/Prefabs";
    public const string EnemiesSprites = Enemies + "/Sprites";
    public const string EnemiesAnimations = Enemies + "/Animations";
    public const string EnemiesVfx = Enemies + "/VFX";

    public const string Collections = Root + "/Collections";
    public const string CollectionsData = Collections + "/Data";
    public const string CollectionsPrefabs = Collections + "/Prefabs";
    public const string CollectionsSprites = Collections + "/Sprites";
    public const string CollectionsAnimations = Collections + "/Animations";

    public const string UI = Root + "/UI";
    public const string UIData = UI + "/Data";
    public const string UIPrefabs = UI + "/Prefabs";
    public const string UIPrefabsProp = UIPrefabs + "/Prop";
    public const string UISprites = UI + "/Sprites";
    public const string UIIconSprites = UISprites + "/Icons";
    public const string UIFonts = UI + "/Fonts";
    public const string UIMaterials = UI + "/Materials";
    public const string UIShaders = UI + "/Shaders";
    public const string UIMotion = UI + "/Motion";
    public const string UIViewPages = UIPrefabs + "/Pages";
    public const string UIViewContainers = UIPrefabs + "/Container";
    public const string PropPresentationCatalog = UIData + "/Prop Presentation Catalog.asset";
    public const string TierColorPalette = UIData + "/Tier Color Palette.asset";
    public const string ItemQualityVisualConfig = UIData + "/Item Quality Visual Config.asset";
    public const string ItemQualityIconEffectMaterial = UIMaterials + "/WeaponQualityIconEffect.mat";
    public const string UIFrameworkSettings = UIData + "/OrangeUIFrameworkSettings.asset";
    public const string UIViewCatalog = UIData + "/OrangeUIViewCatalog.asset";
    public const string OrangeCanvasProfile = UIData + "/OrangeCanvasProfile.asset";
    public const string PropertyShowFolder = UI + "/Cyber/PropertyShow";
    public const string PropertyShowPropIconsAtlas = PropertyShowFolder + "/prop_icons.png";
    public const string PropShowerPrefab = UIPrefabsProp + "/PropShower.prefab";
    public const string CyberPropItemPrefab = UIPrefabsProp + "/CyberPropItem.prefab";

    public const string Audio = Root + "/Audio";
    public const string AudioData = Audio + "/Data";
    public const string AudioBgm = Audio + "/BGM";
    public const string AudioSfx = Audio + "/SFX";
    public const string AudioVfx = Audio + "/VFX";
    public const string AudioRuntimeSettings = AudioData + "/Audio Runtime Settings.asset";
    public const string AudioBusSettings = AudioData + "/Audio Bus Settings.asset";
    public const string WoodBlockSfx = AudioVfx + "/WoodBlock1.wav";
    public const string SwipeSfx = AudioVfx + "/Swipe.wav";
    public const string SlapSfx = AudioVfx + "/Slap.wav";

    public const string Combat = Root + "/Combat";
    public const string CombatData = Combat + "/Data";
    public const string CombatBuffs = CombatData + "/Buffs";
    public const string CombatMaterials = Combat + "/Materials";
    public const string CombatVfx = Combat + "/VFX";
    public const string CombatVfxPrefabs = CombatVfx + "/Prefabs";
    public const string DamageTextVisualConfig = CombatData + "/Damage Text Visual Config.asset";
    public const string DamageTextPrefab = CombatVfxPrefabs + "/Damage Text.prefab";

    public const string Map = Root + "/Map";
    public const string MapData = Map + "/Data";
    public const string MapPrefabs = Map + "/Prefabs";
    public const string MapTiles = Map + "/Tiles";
    public const string MapSprites = Map + "/Sprites";
    public const string MapGeneration = MapData + "/Generation";
    public const string MapGenerationProfiles = MapGeneration + "/Profiles";
    public const string MapGenerationTileSets = MapGeneration + "/Tile Sets";
    public const string MapGenerationRules = MapGeneration + "/Rules";
    public const string MapGenerationConstraints = MapGeneration + "/Constraints";
    public const string MapGenerationProfileFolder = MapGeneration;
    public const string TxGrassGroundTheme = MapData + "/TX Grass Ground Theme.asset";
}
