using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace D9Extended
{
    class GenWeight_Biome : SettlementGenWeight
    {
        List<BiomeWeight> biomes;
        public override float ValueFor(Faction fac, Tile til)
        {
            return WeightFromBiome(til.biome);
        }
        private float WeightFromBiome(BiomeDef b)
        {
            foreach(BiomeWeight bw in biomes)
            {
                if (bw.biome == b) return bw.weight;
            }
            return 1f;
        }
    }
}
