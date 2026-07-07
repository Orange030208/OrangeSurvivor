# Survivors Luban Tables

This folder is the table-driven gameplay data workspace for the Survivors project.

## Layout

```text
Luban/Survivors/
  luban.conf
  Defines/
    __root__.xml
  Datas/
    common/
    feature/
    reward/
    item/
    buff/
    enemy/
    economy/
    wave/
```

Definitions live in XML because Luban's Excel definition files do not support polymorphic beans. Data files stay in xlsx so designers can edit content in spreadsheets.

## TableKit Settings

Use these values in `Tools/TableKit/Configuration Table Tool`:

```text
Luban work dir: Luban/Survivors
Luban.dll path: Luban/Tools/Luban/Luban.dll
Target: client
Code Target: cs-bin
Data Target: bin
Code output dir: Assets/Scripts/TableKit/
Data output dir: Assets/Resources/Art/Table/
Runtime path pattern: Art/Table/{0}
Use assembly definition: true
Assembly name: Game.Tables
```

## Feature Model

Content rows reference `featureSetId`. Feature sets are built from:

```text
feature_set.xlsx
feature_set_entry.xlsx
feature.xlsx
```

`feature.FeatureRow.spec` is a polymorphic `feature.FeatureSpec` bean. This avoids one parameter table per feature class while keeping typed generated code.

Use `feature.CustomSpec` only for rare behavior that cannot be expressed by the common specs.

For the first validation pass, complex bean/list cells can stay empty. After TableKit validates the workspace, migrate one small content chain at a time:

```text
reward_card.featureSetId
  -> feature_set.id
  -> feature_set_entry.featureId
  -> feature.id + feature.spec
```

The generated templates intentionally contain header/type/comment rows only. This keeps Luban validation focused on schema correctness before we start filling nested `FeatureSpec` values.

## Data Templates

```text
Datas/common/resource.xlsx
Datas/feature/feature.xlsx
Datas/feature/feature_set.xlsx
Datas/feature/feature_set_entry.xlsx
Datas/reward/reward_card.xlsx
Datas/item/accessory.xlsx
Datas/item/weapon.xlsx
Datas/item/weapon_level.xlsx
Datas/item/weapon_holder_modifier.xlsx
Datas/buff/buff.xlsx
Datas/enemy/enemy.xlsx
Datas/enemy/enemy_base_prop.xlsx
Datas/economy/shop_pool_entry.xlsx
Datas/economy/drop_pool.xlsx
Datas/economy/drop_pool_entry.xlsx
Datas/wave/run_progression_point.xlsx
Datas/wave/wave.xlsx
Datas/wave/wave_spawn_plan.xlsx
```

## Initial Migration Order

1. `reward/reward_card.xlsx`
2. `feature/feature.xlsx` with `ModifyPropSpec`
3. `item/accessory.xlsx`
4. `buff/buff.xlsx`
5. `item/weapon.xlsx` and weapon level tables
6. Enemy, economy, and wave tables
