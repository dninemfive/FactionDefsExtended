using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Verse;
using RimWorld;

namespace D9Extended
{
    class FactionOpinion : IExposable
    {
        public FactionDef faction;
        public OpinionType opinion = OpinionType.Neutral;

        public enum OpinionType
        {
            Neutral,        //no effect. Default.
            Allied,         //always has the same opinion of the player as the other
            Rivals,         //always has the opposite opinion of the player as the other
            NeverHostile,
            AlwaysHostile
        }

        public FactionOpinion(FactionDef f, OpinionType o)
        {
            faction = f;
            opinion = o;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref faction, "faction");
            Scribe_Values.Look(ref opinion, "opinion", OpinionType.Neutral);
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1)
            {
                MiscUtility.LogError("Misconfigured FactionOpinion: " + xmlRoot.OuterXml, false);
            }
            else
            {
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "faction", xmlRoot.Name);
                opinion = (OpinionType)ParseHelper.FromString(xmlRoot.FirstChild.Value, typeof(Enum));
            }
        }
    }
}
