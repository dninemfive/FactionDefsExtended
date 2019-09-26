using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace D9Extended
{
    class SkillRange
    {
        public IntRange range;
        public SkillDef def;
        /* TODO: this
        public SimpleCurve passionCurve = new SimpleCurve
        {
            new CurvePoint(
        }*/
        public bool clampToRange = false;
    }
}
