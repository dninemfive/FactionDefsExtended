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
        public List<ThingWeight> raceWeights = null;// = new List<ThingWeight> { ThingWeight.Human };
        public float maleProportion = .5f;
        public SimpleCurve ageCurve;
        List<ThingDef> defaultParkas = null;
        List<ThingDef> defaultTuques = null;
        //to select weighted thing: ThingDef def = WeaponWeights.RandomElementByWeight((ThingWeight tw) => tw.weight).def;
        List<ThingWeight> StuffWeights = null;
        public List<ThingWeight> WeaponWeights = null;
        public List<ThingWeight> ApparelWeights = null;
        public List<SkillRange> SkillRanges = null;
        public List<BodyTypeWeight> BodyTypeWeights = null;
        List<TraitChance> traitChances = null;
        List<HediffWeight> hediffWeights = null;
        //public ColorGenerator skinColorOverride = null;
        public float? centralMelanin = null;
        public float? melaninVariance = null;
        public ColorGenerator hairColorsOverride = null;
        RulePackDef customNameMaker = null;
        List<string> hairTagOverride;
        //hediffs/bionics
        //private List<ThingWeight> stuffWeights; //TBI
    }
}
