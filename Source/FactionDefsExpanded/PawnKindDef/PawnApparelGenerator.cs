using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace D9Extended
{
    public static class PawnApparelGenerator
    {
        //TODO: get min/max temperature based on race stats
        private const float StartingMinTemperature = 12f;
        private const float TargetMinTemperature = -40f;
        private const float StartingMaxTemperature = 32f;
        private const float TargetMaxTemperature = 30f;
        private const float maxIterations = 3;
    }
}