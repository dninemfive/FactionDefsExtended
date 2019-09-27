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
        public static ThingWeight Human = new ThingWeight(ThingDefOf.Human, 1.0f);
        public ThingDef def;
        public float weight, weightMale, weightFemale;

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
    }
}
