using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace D9Extended
{
    class PawnGroupKindWorkerME : DefModExtension
    {
        public enum Type
        {
            Normal,
            Trader
        }
        public Type type = Type.Normal;
        //NYI
        int minPawns;
        int maxPawns;
    }
}
