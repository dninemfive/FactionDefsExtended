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
        List<ThingDef> AllowedBuildingStuff;
        List<ThingDef> StuffWeights;
        List<ThingDef> AllowedMortars;
        List<ThingDef> AllowedTurrets;
        //some kind of custom siege blueprint generator, set whether supplies arrive via drop pod or caravan
        List<SettlementGenWeight> settlementBiases;
        PawnKindDef leaderPawnKind;
        PawnKindDef customFirstLeader; //to have a hyper-specific first leader but not have clones show up when they die. 
        float mtbLeaderTermYears;
        float leaderChanceToDieWhenLeaving;
        float NewLeaderRelatedToPreviousChance;
        List<FactionOpinion> factionOpinions;
        bool useVanillaPawnGroupKinds = true; //true: uses Normal and Trader; false: requests for these are intercepted
        List<TraderPawnGroupOverride> traderGroupOverrides;
        PawnKindDefME pawnKindDefaults;
        //some way to override settlement generator
        //Uniforms: override PawnKindDefs conditionally based on a class or smth
        //change FactionOpinion preset to more general FactionTicker, which can be overriden for fun effects
    }
}
