using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace D9Extended
{
    class ServiceRifleData
    {
        class ServiceRifleDef
        {
            List<ThingDef> weapons;
            float weight;
        }

        List<ServiceRifleDef> options;
        bool differentForEachSettlement;

        //on generation, for each faction, select a random entry from options. 
        //Pawns will select a weighted entry from these iff they have the appropriate or no tags. 
        //E.g. you could have an MG and an AR in the same ServiceRifleDef, and if a pawn only has the MG tag they'll get the latter.
        //do the same thing for uniforms, allow colors and different defs
    }
}
