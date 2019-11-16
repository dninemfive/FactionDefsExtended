using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace D9Extended
{/*
    public static class PawnApparelGenerator
    {
        //TODO: get min/max temperature based on race stats
        private const float StartingMinTemperature = 12f;
        private const float TargetMinTemperature = -40f;
        private const float StartingMaxTemperature = 32f;
        private const float TargetMaxTemperature = 30f;
        private const float maxIterations = 3;
        private static StringBuilder debugSb;

        public static void GenerateStartingApparelFor(Pawn pawn, PawnGenerationRequest request)
        {
            if (pawn.RaceProps.ToolUser && pawn.RaceProps.IsFlesh)
            {
                pawn.apparel.DestroyAll(DestroyMode.Vanish);
                float randomInRange = pawn.kindDef.apparelMoney.RandomInRange;                
                NeededWarmth neededWarmth = ApparelWarmthNeededNow(pawn, request, out float mapTemperature);
                bool generateHeadgear = Rand.Value < pawn.kindDef.apparelAllowHeadgearChance;
                debugSb = null;
                if (DebugViewSettings.logApparelGeneration)
                {
                    debugSb = new StringBuilder();
                    debugSb.AppendLine("Generating apparel for " + pawn);
                    debugSb.AppendLine("Money: " + randomInRange.ToString("F0"));
                    debugSb.AppendLine("Needed warmth: " + neededWarmth);
                    debugSb.AppendLine("Headgear allowed: " + generateHeadgear);
                }
                if (randomInRange < 0.001f)
                {
                    GenerateWorkingPossibleApparelSetFor(pawn, randomInRange, generateHeadgear);
                }
                else
                {
                    int num = 0;
                    while (true)
                    {
                        GenerateWorkingPossibleApparelSetFor(pawn, randomInRange, generateHeadgear);
                        if (DebugViewSettings.logApparelGeneration)
                        {
                            debugSb.Append(num.ToString().PadRight(5) + "Trying: " + workingSet.ToString());
                        }
                        if (num < 10 && Rand.Value < 0.85f)
                        {
                            float num2 = Rand.Range(0.45f, 0.8f);
                            float totalPrice = workingSet.TotalPrice;
                            if (totalPrice < randomInRange * num2)
                            {
                                if (DebugViewSettings.logApparelGeneration)
                                {
                                    debugSb.AppendLine(" -- Failed: Spent $" + totalPrice.ToString("F0") + ", < " + (num2 * 100f).ToString("F0") + "% of money.");
                                }
                                goto IL_035e;
                            }
                        }
                        if (num < 20 && Rand.Value < 0.97f && !workingSet.Covers(BodyPartGroupDefOf.Torso))
                        {
                            if (DebugViewSettings.logApparelGeneration)
                            {
                                debugSb.AppendLine(" -- Failed: Does not cover torso.");
                            }
                        }
                        else if (num < 30 && Rand.Value < 0.8f && workingSet.CoatButNoShirt())
                        {
                            if (DebugViewSettings.logApparelGeneration)
                            {
                                debugSb.AppendLine(" -- Failed: Coat but no shirt.");
                            }
                        }
                        else
                        {
                            if (num < 50)
                            {
                                bool mustBeSafe = num < 17;
                                if (!workingSet.SatisfiesNeededWarmth(neededWarmth, mustBeSafe, mapTemperature))
                                {
                                    if (DebugViewSettings.logApparelGeneration)
                                    {
                                        debugSb.AppendLine(" -- Failed: Wrong warmth.");
                                    }
                                    goto IL_035e;
                                }
                            }
                            if (num >= 80)
                            {
                                break;
                            }
                            if (!workingSet.IsNaked(pawn.gender))
                            {
                                break;
                            }
                            if (DebugViewSettings.logApparelGeneration)
                            {
                                debugSb.AppendLine(" -- Failed: Naked.");
                            }
                        }
                        goto IL_035e;
                        IL_035e:
                        num++;
                    }
                    if (DebugViewSettings.logApparelGeneration)
                    {
                        debugSb.Append(" -- Approved! Total price: $" + workingSet.TotalPrice.ToString("F0") + ", TotalInsulationCold: " + workingSet.TotalInsulationCold);
                    }
                }
                if ((!pawn.kindDef.apparelIgnoreSeasons || request.ForceAddFreeWarmLayerIfNeeded) && !workingSet.SatisfiesNeededWarmth(neededWarmth, true, mapTemperature))
                {
                    workingSet.AddFreeWarmthAsNeeded(neededWarmth, mapTemperature);
                }
                if (DebugViewSettings.logApparelGeneration)
                {
                    Log.Message(debugSb.ToString(), false);
                }
                workingSet.GiveToPawn(pawn);
                workingSet.Reset(null, null);
                if (pawn.kindDef.apparelColor != Color.white)
                {
                    List<Apparel> wornApparel = pawn.apparel.WornApparel;
                    for (int i = 0; i < wornApparel.Count; i++)
                    {
                        wornApparel[i].SetColor(pawn.kindDef.apparelColor, false);
                    }
                }
            }
        }

        private static NeededWarmth ApparelWarmthNeededNow(Pawn pawn, PawnGenerationRequest request, out float mapTemperature)
        {
            int tile = request.Tile;
            if (tile == -1)
            {
                Map anyPlayerHomeMap = Find.AnyPlayerHomeMap;
                if (anyPlayerHomeMap != null)
                {
                    tile = anyPlayerHomeMap.Tile;
                }
            }
            if (tile == -1)
            {
                mapTemperature = 21f;
                return NeededWarmth.Any;
            }
            NeededWarmth neededWarmth = NeededWarmth.Any;
            Twelfth twelfth = GenLocalDate.Twelfth(tile);
            mapTemperature = GenTemperature.AverageTemperatureAtTileForTwelfth(tile, twelfth);
            for (int i = 0; i < 2; i++)
            {
                NeededWarmth neededWarmth2 = CalculateNeededWarmth(pawn, tile, twelfth);
                if (neededWarmth2 != 0)
                {
                    neededWarmth = neededWarmth2;
                    break;
                }
                twelfth = twelfth.NextTwelfth();
            }
            if (pawn.kindDef.apparelIgnoreSeasons)
            {
                if (request.ForceAddFreeWarmLayerIfNeeded && neededWarmth == NeededWarmth.Warm)
                {
                    return neededWarmth;
                }
                return NeededWarmth.Any;
            }
            return neededWarmth;
        }
        public static NeededWarmth CalculateNeededWarmth(Pawn pawn, int tile, Twelfth twelfth)
        {
            float num = GenTemperature.AverageTemperatureAtTileForTwelfth(tile, twelfth);
            if (num < pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin, null) - 4f)
            {
                return NeededWarmth.Warm;
            }
            if (num > pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin, null) + 4f)
            {
                return NeededWarmth.Cool;
            }
            return NeededWarmth.Any;
        }
    }*/
}