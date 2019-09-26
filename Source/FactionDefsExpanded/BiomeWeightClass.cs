using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Verse;
using RimWorld;
using UnityEngine;

namespace D9Extended
{
    class BiomeWeightClass : IExposable
    {
        public BiomeDef biome;
        public float weight;

        public BiomeWeightClass(BiomeDef b, float w)
        {
            biome = b;
            weight = w;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref biome, "biome");
            Scribe_Values.Look(ref weight, "weight", 1);
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1)
            {
                MiscUtility.LogError("Misconfigured BiomeWeightClass: " + xmlRoot.OuterXml, false);
            }
            else
            {
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "biome", xmlRoot.Name);
                weight = (float)ParseHelper.FromString(xmlRoot.FirstChild.Value, typeof(float));
            }
        }
    }
}
