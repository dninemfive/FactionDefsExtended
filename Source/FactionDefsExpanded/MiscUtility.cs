using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace D9Extended
{
    class MiscUtility
    {
        public static bool DEBUG = true;
        public static string modid = "FactionDefs Extended";

        public static void LogError(String s)
        {
            Log.Error("[" + modid + "] " + s);
        }

        public static void LogError(String s, bool over)
        {
            Log.Error("[" + modid + "] " + s, over);
        }

        public static void DebugMessage(String s)
        {
            if (DEBUG) Log.Message("[" + modid + "] " + s);
        }
    }
}
