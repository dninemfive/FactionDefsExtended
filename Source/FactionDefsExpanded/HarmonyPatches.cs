using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Harmony;
using System.Reflection;

namespace D9Extended
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = HarmonyInstance.Create("com.dninemfive.factiondefsextended");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[Accessible Archotech] Harmony loaded.");
        }

        [HarmonyPatch(typeof(FactionGenerator))]
        [HarmonyPatch("NewGeneratedFaction")]
        [HarmonyPatch(new Type[] { typeof(FactionDef) })]
        class NewGeneratedFaction
        {
            public static void NewGeneratedFactionPostfix(ref Faction __result)
            {
                if (__result.def.HasModExtension<FactionDefME>())
                {

                }
            }
        }
        //Faction.TryMakeInitialRelationWith()
        //TileFinder.RandomSettlementTileFor()
        //Faction.GenerateNewLeader()
    }
}