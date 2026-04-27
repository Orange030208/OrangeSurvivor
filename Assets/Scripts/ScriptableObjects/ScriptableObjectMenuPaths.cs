public static class ScriptableObjectMenuPaths
{
    public const string ROOT = "Survivors/";

    public const string GAMEPLAY_ROOT = ROOT + "Gameplay/";
    public const string ENTITY_ROOT = ROOT + "Entity/";
    public const string SYSTEMS_ROOT = ROOT + "Systems/";
    public const string PRESENTATION_ROOT = ROOT + "Presentation/";
    public const string WORLD_ROOT = ROOT + "World/";

    public const string BUFF = GAMEPLAY_ROOT + "Buffs/Buff";
    public const string COLLECTION = GAMEPLAY_ROOT + "Drops/Collection";
    public const string ARCHER_ENEMY = GAMEPLAY_ROOT + "Enemies/Archer Enemy";
    public const string MAGE_ENEMY = GAMEPLAY_ROOT + "Enemies/Mage Enemy";
    public const string BASE_PROP_GROUP = GAMEPLAY_ROOT + "Player/Base Prop Group";
    public const string PLAYER_LEVEL_CONFIG = GAMEPLAY_ROOT + "Player/Level Config";
    public const string PROJECTILE_DEFINITION = GAMEPLAY_ROOT + "Projectiles/Projectile Definition";
    public const string ACCESSORY = GAMEPLAY_ROOT + "Items/Accessories/Accessory";
    public const string ACCESSORY_DATA_LIST = GAMEPLAY_ROOT + "Items/Accessories/Accessory Data List";
    public const string WEAPON_ATTACK_SEQUENCE = GAMEPLAY_ROOT + "Weapons/Attack Sequence";
    public const string WEAPON_DATA = GAMEPLAY_ROOT + "Weapons/Weapon Data";
    public const string WEAPON_DATA_LIST = GAMEPLAY_ROOT + "Weapons/Weapon Data List";
    public const string SPAWN_LOCATION_POLICY = GAMEPLAY_ROOT + "Waves/Spawn Location Policy";
    public const string STAGE_DEFINITION = GAMEPLAY_ROOT + "Waves/Stage Definition";
    public const string WAVE_DEFINITION = GAMEPLAY_ROOT + "Waves/Wave Definition";
    public const string WAVE_FLOW_DEFINITION = GAMEPLAY_ROOT + "Waves/Wave Flow Definition";
    public const string WAVE_REWARD_DEFINITION = GAMEPLAY_ROOT + "Waves/Wave Reward Definition";

    public const string COLLECTION_ANIMATION_CONFIG = ENTITY_ROOT + "Animation/Collection Animation Config";
    public const string ENEMY_ANIMATION_CONFIG = ENTITY_ROOT + "Animation/Enemy Animation Config";
    public const string MELEE_ATTACK_STRATEGY = ENTITY_ROOT + "Attack Strategies/Melee Attack Strategy";
    public const string NO_ATTACK_STRATEGY = ENTITY_ROOT + "Attack Strategies/No Attack Strategy";
    public const string RANGE_ATTACK_STRATEGY = ENTITY_ROOT + "Attack Strategies/Range Attack Strategy";
    public const string CIRCLE_KITE_STRATEGY = ENTITY_ROOT + "Movement Strategies/Circle Kite Strategy";
    public const string DIRECT_CHASE_STRATEGY = ENTITY_ROOT + "Movement Strategies/Direct Chase Strategy";
    public const string RETREAT_STRATEGY = ENTITY_ROOT + "Movement Strategies/Retreat Strategy";

    public const string AUDIO_RUNTIME_SETTINGS = SYSTEMS_ROOT + "Audio/Runtime Settings";
    public const string AUDIO_SFX_CATALOG = SYSTEMS_ROOT + "Audio/Sfx Catalog";
    public const string UI_FRAMEWORK_SETTINGS = SYSTEMS_ROOT + "UI/Framework Settings";
    public const string UI_PREFAB_CATALOG = SYSTEMS_ROOT + "UI/Prefab Catalog";

    public const string ITEM_QUALITY_VISUAL_CONFIG = PRESENTATION_ROOT + "Items/Quality Visual Config";
    public const string MAP_GROUND_THEME = WORLD_ROOT + "Map/Ground Theme";
}
