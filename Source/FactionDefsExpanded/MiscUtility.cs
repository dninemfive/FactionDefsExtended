using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace D9Extended
{
    static class MiscUtility
    {
        public static bool DEBUG = true;
        public static string modid = "FactionDefs Extended";
        public static string prefix => "[" + modid + "] ";
        #region log
        public static void LogMessage(String s)
        {
            Log.Message(prefix + s);
        }

        public static void LogWarning(String s)
        {
            Log.Warning(prefix + s);
        }

        public static void LogWarning(String s, bool whatev)
        {
            Log.Warning(prefix + s, whatev);
        }

        public static void LogError(String s)
        {
            Log.Error(prefix + s);
        }

        public static void LogError(String s, bool over)
        {
            Log.Error(prefix + s, over);
        }

        public static void DebugMessage(String s)
        {
            if (DEBUG) Log.Message(prefix + s);
        }
        #endregion log
        #region ?
        public static bool FalseIfNull(bool? b)
        {
            if (b == null) return false;
            return (bool)b;
        }
        #endregion ?
        public static List<ThingWeight> WeaponWeightsFor(Pawn pawn)
        {
            if (pawn.def.HasModExtension<PawnKindDefME>()) return pawn.def.GetModExtension<PawnKindDefME>().WeaponWeights;
            if (pawn.Faction.def.HasModExtension<FactionDefME>()) return pawn.Faction.def.GetModExtension<FactionDefME>().pawnKindDefaults?.WeaponWeights;
            return null;
        }
    }
}
