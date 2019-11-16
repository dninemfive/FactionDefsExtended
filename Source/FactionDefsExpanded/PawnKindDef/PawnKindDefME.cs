using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace D9Extended
{
    public class PawnKindDefME : DefModExtension
    {
        // functional things
        public List<ThingWeight> raceWeights = null;
        public float maleProportion = .5f;
        public SimpleCurve ageCurve = null;             
        public List<SkillRange> SkillRanges = null;
        public List<BodyTypeWeight> BodyTypeWeights = null;        
        public float? centralMelanin = null;
        public float? melaninVariance = null;
        public ColorGenerator hairColorsOverride = null;
        // NYI
        public List<ThingWeight> WeaponWeights = null;
        List<ThingDef> defaultParkas = null;
        List<ThingDef> defaultTuques = null;
        List<TraitChance> traitChances = null;
        List<HediffWeight> hediffWeights = null;
        List<ThingWeight> StuffWeights = null;
        RulePackDef customNameMaker = null;
        List<string> hairTagOverride;
        Dictionary<ThingDef, ColorGenerator> clothingColorGenerators;
        //hediffs/bionics
        //private List<ThingWeight> stuffWeights; //TBI
    }
}
