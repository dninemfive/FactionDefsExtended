using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace D9Extended
{
    public class FactionDefME : DefModExtension
    {
        // Apparel to resort to when spawning into an overly-cold map. Defaults to the base game's parka and tuque, respectively. Extended PawnKinds have a boolean useParkas if you want to override these, and they'll prioritize tagged apparel if it satisfies cold needs.
        List<ThingDef> defaultParkas; 
        List<ThingDef> defaultTuques;
        List<ThingDef> AllowedBuildingStuff;
        List<ThingDef> StuffWeights;
        List<Thing> AllowedMortars;
        List<Thing> AllowedTurrets;
        GenerationBias biases;
        PawnKindDef leaderPawnKind;
        Pawn CustomFirstLeader;
        float NewLeaderRelatedToPreviousChance;
        List<FactionOpinion> factionOpinions;
        bool useVanillaPawnGroupKinds = true; //true: uses Normal and Trader; false: requests for these are intercepted
        List<TraderPawnGroupOverride> traderGroupOverrides;
        //basic PawnKindDef data
        //some way to override settlement generator
    }
}
