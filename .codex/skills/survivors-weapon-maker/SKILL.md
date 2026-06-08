---
name: survivors-weapon-maker
description: "Single-weapon numeric design for the Survivors Unity project. Use when Codex needs to create, propose, review, or tune one weapon's Lv1-Lv4 stats, price, tags, attack type usage, external stat benefits, equivalent DPS, balance risks, and asset-ready runtime fields such as projectile spawn points and visual-forward angle compensation."
---

# Survivors Weapon Maker

## Overview

Use this skill only for one weapon's numeric design. Generate a complete, fill-ready stat proposal when the user gives a short concept. If the user gives explicit requirements, preserve them unless they violate project units, hard boundaries, or balance rules.

## Hard Gates

- Read the project's gameplay numeric rules and numeric implementation rules under `Docs/` before changing or proposing weapon stats.
- Use project units exactly: percent points for percentages, distance points for range, attack-speed points for attack speed.
- Always output all four levels.
- Use equivalent DPS as the primary power target, not naked single-target DPS alone.
- Keep level growth focused: each level should mainly improve 1-2 directions.
- Default `holderModifiers` to empty. Add holder-wide properties only when the user explicitly asks for global or build-defining effects.
- Do not let user-specified fields bypass boundary, unit, or risk reporting.

## Protocol

1. Infer or confirm the weapon profile:
   - quality: Normal or Powerful;
   - primary tag: Heavy, Fast, Precision, or Growth;
   - attack shape: single target, short melee sweep, large melee sweep, multi-shot/spread, piercing line, area/ring, summon, or persistent area;
   - primary attack type: MeleeAttack, RangedAttack, MagicAttack, or SummonAttack.
2. Select target equivalent DPS bands from the quality table.
3. Pick a conservative coverage multiplier from the coverage table.
4. Build Lv1-Lv4 base stats so equivalent DPS lands in band while preserving the weapon's shortcoming.
5. Fill attack type usage, external benefits, and empty holder modifiers.
6. Select the Lv1 price from quality and total strength.
7. If the weapon fires projectiles, include at least one spawn point definition, usually `muzzle`, and make the local offset explicit.
8. Always include the visual forward angle assumption in the final answer. `0` means the sprite is already upright; angled sprites must use the measured compensation, such as `45` for the current diagonal atlas.
9. Output the stat table, calculation summary, runtime-field notes, and risk notes.

## Target Power

Use these equivalent DPS bands by default:

| Quality | Lv1 | Lv2 | Lv3 | Lv4 |
| --- | ---: | ---: | ---: | ---: |
| Normal | 18-26 | 27-39 | 43-62 | 65-90 |
| Powerful | 24-34 | 36-51 | 57-82 | 85-120 |

Lv4 should usually be about `3.3x-4.1x` of Lv1 equivalent DPS. If a concept demands a lower naked DPS, compensate with clearly stated coverage, control, safety, or mechanism value.

## Price

Use only these Lv1 price points:

| Quality | Price points |
| --- | --- |
| Normal | 20 / 25 / 30 |
| Powerful | 35 / 40 / 45 |

Choose a higher point when equivalent DPS, coverage, range safety, external benefit scaling, or growth potential is high. Choose a lower point when the weapon has low reliability, short range, high windup, or weak low-level performance.

## DPS Calculation

Calculate naked single-target DPS with no external player property contribution:

```text
attacksPerSecond = AttackSpeed / 100
critExpectedMultiplier = 1 + (CriticalChance / 100) * max(0, CriticalPercent / 100 - 1)
nakedDps = Attack * attacksPerSecond * critExpectedMultiplier
equivalentDps = nakedDps * coverageMultiplier
```

Mention when external attack type usage will make the weapon scale harder in real builds.

## Coverage Multipliers

Choose conservatively:

| Attack shape | Default multiplier |
| --- | ---: |
| Single direct hit | 1.00 |
| Short melee sweep | 1.10-1.25 |
| Large melee sweep / heavy sweep | 1.20-1.45 |
| Multi-shot / spread | 1.15-1.45 |
| Piercing line | 1.10-1.50 |
| Area / ring | 1.30-1.80 |
| Summon / persistent area | 1.20-1.70 |

Do not stack strong control, high safety, and high coverage for free. If coverage is high, lower naked stats, lower external benefits, increase price, or add a risk note.

## Field Defaults

### Attack Type Usage

- Use the primary attack type by default; keep other attack types at `0`.
- Normal primary usage: Lv1 `80-110`, Lv4 `110-150`.
- Growth/build primary usage: Lv1 `100-120`, Lv4 `150-195`.
- Allow the core type to exceed `100` as build scaling. Use secondary types only when the user explicitly requests a hybrid weapon.

### Base Stat Tendencies

Use these tendencies unless the concept gives a stronger reason:

| Profile | Attack | AttackSpeed | CriticalChance | CriticalPercent | Range | KnockbackStrength |
| --- | --- | --- | --- | --- | --- | --- |
| Heavy melee | high | 45-70 | 0-6 | 160-200 | 150-270 | 18-55 |
| Fast melee | low-medium | 150-300 | 8-18 | 170-200 | 130-170 | 4-6 |
| Fast ranged | low | 200-350 | 2-5 | 150-165 | 650-760 | 2-4 |
| Precision ranged | medium | 75-115 | 8-24 | 175-225 | 760-1050 | 5-10 |
| Growth magic | low-medium | 75-135 | 0-8 | 160-205 | 620-820 | 0-5 |
| Summon / persistent | low | 70-110 | 0-5 | 160-175 | 700-850 | 0-2 |

### External Benefits

Set benefits by tag and shape:

| Profile | AttackSpeedBenefit | CriticalChanceBenefit | CriticalPercentBenefit | RangeBenefit | KnockbackBenefit |
| --- | ---: | ---: | ---: | ---: | ---: |
| Heavy | 50-65 | 55-75 | 70-95 | 80-100 | 70-95 |
| Fast melee | 75-85 | 70-85 | 75-95 | 60-70 | 35-40 |
| Fast ranged | 80-85 | 40-50 | 40-50 | 70-85 | 25-30 |
| Precision | 70-85 | 80-95 | 90-110 | 90-105 | 55-70 |
| Growth magic | 70-85 | 60-75 | 70-90 | 80-100 | 30-35 |
| Summon / persistent | 60-75 | 40-55 | 50-65 | 80-100 | 20-25 |

External benefits may exceed `100` only on core high-level fields, usually capped at `105-110`, and must include a risk note.

### Holder Modifiers

Default to `[]`. If explicitly requested, use `Add` by default and report that holder modifiers affect all held weapons, not only this weapon.

## Output Contract

Always output:

- inferred profile: quality, tag, attack shape, primary attack type;
- Lv1 price and reason;
- Lv1-Lv4 table with `Attack`, `AttackSpeed`, `CriticalChance`, `CriticalPercent`, `Range`, `KnockbackStrength`;
- Lv1-Lv4 attack type usage;
- Lv1-Lv4 external benefits;
- runtime implementation notes for asset-ready generation, including `visualForwardAngle` and `spawnPoints` when the weapon uses projectiles;
- holder modifiers, usually `[]`;
- calculation summary with naked DPS, coverage multiplier, equivalent DPS, and Lv4/Lv1 ratio;
- risk notes and any user requirement that forced a deviation.

## Verification Checklist

- The proposal has exactly four levels.
- Equivalent DPS is inside or intentionally near the selected quality band.
- Lv4/Lv1 equivalent DPS ratio is usually `3.3-4.1`.
- Attack type usage follows the primary type rule or clearly marks a requested hybrid.
- Benefits over `100` are only core, high-level, small-overcap values.
- High coverage or strong control has a corresponding tradeoff.
- Holder modifiers are empty unless explicitly requested.
- Projectile weapons include at least one explicit muzzle spawn point.
- Visual forward angle is stated, with `0` reserved for upright sprites and a measured compensation used for angled art.
