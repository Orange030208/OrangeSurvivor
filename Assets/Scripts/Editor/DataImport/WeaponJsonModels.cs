#if UNITY_EDITOR
using System;
using System.Collections.Generic;

[Serializable]
public sealed class WeaponJsonFile
{
    public List<WeaponJsonWeapon> weapons = new();
}

[Serializable]
public sealed class WeaponJsonWeapon
{
    public string weaponId;
    public string itemName;
    public int itemPrice;
    public string itemDescription;
    public List<string> tags = new();
    public int openWave = 1;
    public int closeWave;
    public float baseWeight = 1f;
    public float visualForwardAngle = 45f;
    public bool holdAimWhenAttackReady = true;
    public float attackSequenceOccupancy = 0.85f;
    public string attackTimingMode;
    public string targetingMode;
    public List<WeaponJsonSpawnPoint> spawnPoints = new();
    public bool enableHitBox;
    public WeaponJsonVector2 hitBoxSize = new() { x = 1f, y = 1f };
    public WeaponJsonVector2 hitBoxOffset = new();
    public List<WeaponJsonLevelStat> levelStats = new();
}

[Serializable]
public sealed class WeaponJsonSpawnPoint
{
    public string id;
    public WeaponJsonVector2 localPosition = new();
    public float localRotationOffset;
}

[Serializable]
public sealed class WeaponJsonVector2
{
    public float x;
    public float y;
}

[Serializable]
public sealed class WeaponJsonLevelStat
{
    public int level;
    public float attack;
    public float attackSpeed;
    public float criticalChance;
    public float criticalPercent;
    public float range;
    public float knockbackStrength;
    public WeaponJsonBenefit statBenefits;
    public List<WeaponJsonPropModifier> holderModifiers = new();
}

[Serializable]
public sealed class WeaponJsonBenefit
{
    public float attackSpeedBenefitPercent;
    public float criticalChanceBenefitPercent;
    public float criticalPercentBenefitPercent;
    public float rangeBenefitPercent;
    public float knockbackStrengthBenefitPercent;
    public float meleeAttackUsagePercent;
    public float rangedAttackUsagePercent;
    public float magicAttackUsagePercent;
    public float summonAttackUsagePercent;
}

[Serializable]
public sealed class WeaponJsonPropModifier
{
    public string propType;
    public string modifierType;
    public float value;
}
#endif
