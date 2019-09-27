using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace D9Extended
{
    public class ThingWeight
    {
        public static ThingWeight Human = new ThingWeight(ThingDefOf.Human, 100f);
        public ThingDef def;
        public float weight, weightMale, weightFemale;
        public float Weight(Gender gender)
        {
            if (gender == Gender.Female && weightFemale >= 0) return weightFemale;
            if (gender == Gender.Male && weightMale >= 0) return weightMale;
            return weight;
        }

        public ThingWeight(ThingDef d, float w)
        {
            def = d;
            weight = w;
            weightMale = -1;
            weightFemale = -1;
        }

        public ThingWeight(ThingDef d, float wm, float wf)
        {
            def = d;
            weightMale = wm;
            weightFemale = wf;
            weight = (wm + wf) / 2;
        }

        public override bool Equals(object obj)
        {
            return obj is ThingWeight && (ThingWeight)obj.def == this.def;
        }
    }
}
