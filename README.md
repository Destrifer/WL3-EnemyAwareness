# Enemy Awareness (Wasteland 3, MelonLoader)

Multiplies the NPC detection radius in Wasteland 3: the red circle that starts combat when you step into it. Default: x2.

## Requirements
- [MelonLoader](https://github.com/LavaGang/MelonLoader) 0.7.x installed into Wasteland 3.

## Install
1. Download `EnemyAwareness-0.1.0.zip` from [Releases](../../releases).
2. Copy `Mods/EnemyAwareness.dll` into `Wasteland 3/Mods`.
3. Start the game once. Then edit `Wasteland 3/UserData/MelonPreferences.cfg`:
   ```
   [EnemyAwareness]
   RadiusMultiplier = 2.0
   ```
   1 = vanilla, 2 = twice as far, 3 = three times... (0.1 to 10). Restart the game or reload a save after changing it.

## How it works
An NPC's perception radius is rebuilt in `Stats.RecalculateAllStatsNpc` as
`PerceptionScaling.GetPerceptionRadius(level, rank, class) + bonusStats.perception`,
or taken from `Spawner.perceptionOverride` when the spawner forces it. The mod scales the final value in both cases via Harmony patches.

## Uninstall
Delete `Mods/EnemyAwareness.dll`.
