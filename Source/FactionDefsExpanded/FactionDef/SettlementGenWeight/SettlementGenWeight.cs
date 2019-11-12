using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace D9Extended
{
    abstract class SettlementGenWeight
    {
        public float factor;
        public float offset;

        public float FinalValueFor(Faction faction, Tile tile)
        {
            return (factor * ValueFor(faction, tile)) + offset;
        }

        public abstract float ValueFor(Faction faction, Tile tile);
    }
}
