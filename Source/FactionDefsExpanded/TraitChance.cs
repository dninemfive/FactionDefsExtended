using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace D9Extended
{
    class TraitChance
    {
        public TraitDef trait;
        public float chance;
        public SimpleCurve degreeCurve = new SimpleCurve
        {
            new CurvePoint(0f, 100f)
        };
    }
}
