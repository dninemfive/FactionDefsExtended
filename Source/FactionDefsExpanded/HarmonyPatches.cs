using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Harmony;
using System.Reflection;

namespace D9Extended
{/*
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = HarmonyInstance.Create("com.dninemfive.factiondefsextended");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            MiscUtility.LogMessage("Harmony Loaded");
        }

        [HarmonyPatch(typeof(PawnApparelGenerator.PossibleApparelSet))]
        [HarmonyPatch("AddFreeWarmthAsNeeded")]
        [HarmonyPatch(new Type[] { typeof(FactionDef) })]
        class NewGeneratedFaction
        {
            public bool void AddFreeWarmthAsNeededPrefix(ref PawnApparelGenerator.PossibleApparelSet __instance, NeededWarmth warmth, float mapTemperature)
            {
                if (__instance.def.HasModExtension<FactionDefME>())
                {

                }
            }
        }
        //Faction.TryMakeInitialRelationWith()
        //TileFinder.RandomSettlementTileFor()
        //Faction.GenerateNewLeader()
    }*/
}