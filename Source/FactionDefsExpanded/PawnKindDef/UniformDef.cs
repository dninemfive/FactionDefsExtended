using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace D9Extended
{
    class UniformDef : Def
    {
        List<ThingDef> forcedApparel;
        Dictionary<ThingDef, ColorGenerator> generators;
    }
}
