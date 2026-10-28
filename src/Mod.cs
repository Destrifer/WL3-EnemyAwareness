using HarmonyLib;
using Il2Cpp;
using MelonLoader;

[assembly: MelonInfo(typeof(EnemyAwareness.Mod), "Enemy Awareness", "0.1.0", "wat3")]
[assembly: MelonGame(null, "Wasteland 3")]

namespace EnemyAwareness;

// An NPC's detection radius is its perception stat, rebuilt natively in Stats.RecalculateAllStatsNpc:
// PerceptionScaling.GetPerceptionRadius(level, rank, class) + bonusStats.perception,
// or Spawner.perceptionOverride when the spawner forces a value. The native code then mirrors
// the stat into MobData for the detection jobs, so the value is scaled before it is written:
// the formula result through a GetPerceptionRadius postfix, a forced value by temporarily
// scaling the spawner's override for the duration of the recalculation.
public sealed class Mod : MelonMod
{
    private HarmonyLib.Harmony? harmony;
    private static MelonPreferences_Entry<float>? multiplierEntry;
    private static bool enabled;
    private static bool errorReported;

    private static bool inRecalc;
    private static int currentBonus;
    private static Spawner? scaledSpawner;
    private static int originalOverride;

    private static float Multiplier => Math.Clamp(multiplierEntry?.Value ?? 2f, 0.1f, 10f);

    public override void OnInitializeMelon()
    {
        try
        {
            var category = MelonPreferences.CreateCategory("EnemyAwareness", "Enemy Awareness");
            multiplierEntry = category.CreateEntry("RadiusMultiplier", 2f, "Radius multiplier",
                "Multiplies the NPC detection radius (1 = vanilla, 2 = twice as far). Range 0.1-10. Restart the game or reload a save after changing.");

            harmony = new HarmonyLib.Harmony("wat3.enemy-awareness");
            var recalc = AccessTools.Method(typeof(Stats), "RecalculateAllStatsNpc")
                ?? throw new MissingMethodException(nameof(Stats), "RecalculateAllStatsNpc");
            var radius = AccessTools.Method(typeof(PerceptionScaling), nameof(PerceptionScaling.GetPerceptionRadius))
                ?? throw new MissingMethodException(nameof(PerceptionScaling), nameof(PerceptionScaling.GetPerceptionRadius));
            harmony.Patch(recalc, prefix: new HarmonyMethod(typeof(Mod), nameof(RecalcPrefix)), postfix: new HarmonyMethod(typeof(Mod), nameof(RecalcPostfix)));
            harmony.Patch(radius, postfix: new HarmonyMethod(typeof(Mod), nameof(RadiusPostfix)));
            enabled = true;
            MelonLogger.Msg($"Enemy Awareness READY | NPC detection radius x{Multiplier:0.##}");
        }
        catch (Exception e)
        {
            enabled = false;
            harmony?.UnpatchSelf();
            MelonLogger.Error("Enemy Awareness DISABLED: " + e);
        }
    }

    private static void RecalcPrefix(Stats __instance)
    {
        inRecalc = false;
        scaledSpawner = null;
        if (!enabled) return;
        try
        {
            var npc = __instance.mob?.TryCast<Npc>();
            if (npc == null) return;
            currentBonus = npc.bonusStats.perception;
            var spawner = npc.spawner;
            if (spawner != null && spawner.overridePerception)
            {
                originalOverride = spawner.perceptionOverride;
                spawner.perceptionOverride = (int)Math.Round(originalOverride * Multiplier);
                scaledSpawner = spawner;
            }
            inRecalc = true;
        }
        catch (Exception e)
        {
            Report(e);
        }
    }

    private static void RecalcPostfix()
    {
        try
        {
            if (scaledSpawner != null) scaledSpawner.perceptionOverride = originalOverride;
        }
        catch (Exception e)
        {
            Report(e);
        }
        finally
        {
            scaledSpawner = null;
            inRecalc = false;
        }
    }

    // The caller adds bonusStats.perception after this call; scale the sum, not just the base.
    private static void RadiusPostfix(ref int __result)
    {
        if (!enabled) return;
        var bonus = inRecalc ? currentBonus : 0;
        __result = (int)Math.Round((__result + bonus) * Multiplier) - bonus;
    }

    private static void Report(Exception e)
    {
        if (errorReported) return;
        errorReported = true;
        MelonLogger.Warning("Enemy Awareness: patch failed for an NPC; its vanilla radius may be kept. " + e);
    }

    public override void OnDeinitializeMelon()
    {
        enabled = false;
        harmony?.UnpatchSelf();
    }
}
