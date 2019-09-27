using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;


namespace D9Extended
{
    /*
     * COMPAT TODO:
     *  - Alien Races:
     *      - Bodytype override check
     *  - Psychology
     *      - Don't add gay trait if kinsey enabled
     */

    public static class PawnGenerator
    {
        #region defaults/variables
        public const int MaxTries = 120;

        private static SimpleCurve DefaultAgeGenerationCurve = new SimpleCurve
        {
            {
                new CurvePoint(0.05f, 0f),
                true
            },
            {
                new CurvePoint(0.1f, 100f),
                true
            },
            {
                new CurvePoint(0.675f, 100f),
                true
            },
            {
                new CurvePoint(0.75f, 30f),
                true
            },
            {
                new CurvePoint(0.875f, 18f),
                true
            },
            {
                new CurvePoint(1f, 10f),
                true
            },
            {
                new CurvePoint(1.125f, 3f),
                true
            },
            {
                new CurvePoint(1.25f, 0f),
                true
            }
        };
        private static readonly SimpleCurve LevelRandomCurve = new SimpleCurve
        {
            {
                new CurvePoint(0f, 0f),
                true
            },
            {
                new CurvePoint(0.5f, 150f),
                true
            },
            {
                new CurvePoint(4f, 150f),
                true
            },
            {
                new CurvePoint(5f, 25f),
                true
            },
            {
                new CurvePoint(10f, 5f),
                true
            },
            {
                new CurvePoint(15f, 0f),
                true
            }
        };
        private static readonly SimpleCurve LevelFinalAdjustmentCurve = new SimpleCurve
        {
            {
                new CurvePoint(0f, 0f),
                true
            },
            {
                new CurvePoint(10f, 10f),
                true
            },
            {
                new CurvePoint(20f, 16f),
                true
            },
            {
                new CurvePoint(27f, 20f),
                true
            }
        };
        private static readonly SimpleCurve AgeSkillMaxFactorCurve = new SimpleCurve
        {
            {
                new CurvePoint(0f, 0f),
                true
            },
            {
                new CurvePoint(10f, 0.7f),
                true
            },
            {
                new CurvePoint(35f, 1f),
                true
            },
            {
                new CurvePoint(60f, 1.6f),
                true
            }
        };

        #endregion defaults/variables

        #region static pawngeneration shit
        [StructLayout(LayoutKind.Sequential, Size = 1)]
        private struct PawnGenerationStatus
        {
            public Pawn Pawn
            {
                get;
                private set;
            }

            public List<Pawn> PawnsGeneratedInTheMeantime
            {
                get;
                private set;
            }

            public PawnGenerationStatus(Pawn pawn, List<Pawn> pawnsGeneratedInTheMeantime)
            {
                this = default(PawnGenerationStatus);
                Pawn = pawn;
                PawnsGeneratedInTheMeantime = pawnsGeneratedInTheMeantime;
            }
        }

        private static List<PawnGenerationStatus> pawnsBeingGenerated = new List<PawnGenerationStatus>();
        #endregion static pawngeneration shit

        private static float WorldPawnSelectionWeight(Pawn p) //TODO: evaluate whether to change/expose
        {
            if (p.RaceProps.IsFlesh && !p.relations.everSeenByPlayer && p.relations.RelatedToAnyoneOrAnyoneRelatedToMe)
            {
                return 0.1f;
            }
            return 1f;
        }

        public static Pawn GeneratePawn(PawnGenerationRequest req)
        {
            if (!req.KindDef.HasModExtension<PawnKindDefME>()) return Verse.PawnGenerator.GeneratePawn(req);
            try
            {
                MiscUtility.LogMessage("GeneratePawn: 1");
                Pawn pawn = GenerateOrRedressPawn(req);
                //candidate to remove
                MiscUtility.DebugMessage("GeneratePawn: 2");
                #region checkdead
                if (pawn != null && !req.AllowDead && pawn.health.hediffSet.hediffs.Any())
                {
                    bool dead = pawn.Dead;
                    bool downed = pawn.Downed;
                    pawn.health.hediffSet.DirtyCache();
                    pawn.health.CheckForStateChange(null, null);
                    if (pawn.Dead)
                    {
                        MiscUtility.LogError("Pawn was generated dead but the pawn generation req specified the pawn must be alive. This shouldn't ever happen even if we ran out of tries because null pawn should have been returned instead in this case. Resetting health...\npawn.Dead=" + pawn.Dead + " pawn.Downed=" + pawn.Downed + " deadBefore=" + dead + " downedBefore=" + downed + "\nrequest=" + req, false);
                        pawn.health.Reset();
                    }
                }
                #endregion checkdead
                if (pawn.Faction == Faction.OfPlayerSilentFail)
                {
                    Find.StoryWatcher.watcherPopAdaptation.Notify_PawnEvent(pawn, PopAdaptationEvent.GainedColonist);
                }
                MiscUtility.DebugMessage("GeneratePawn: 3");
                return pawn;
            }
            catch (Exception e)
            {
                MiscUtility.LogError("Error while generating pawn. Rethrowing. Exception: " + e, false);
                throw;
            }
        }
        private static Pawn GenerateOrRedressPawn(PawnGenerationRequest req)
        {
            MiscUtility.LogMessage("GenerateOrRedressPawn: 1");
            //if (!req.KindDef.HasModExtension<PawnKindDefME>()); //shouldn't happen because I check before doing this
            Pawn pawn = null;
            MiscUtility.LogMessage("GenerateOrRedressPawn: 2");
            #region redress
            if (!req.Newborn && !req.ForceGenerateNewPawn)
            {
                MiscUtility.LogMessage("GenerateOrRedressPawn: 2.5");
                if (req.ForceRedressWorldPawnIfFormerColonist)
                {
                    IEnumerable<Pawn> validCandidatesToRedress = GetValidCandidatesToRedress(req);
                    if (validCandidatesToRedress.Where(PawnUtility.EverBeenColonistOrTameAnimal).TryRandomElementByWeight(WorldPawnSelectionWeight, out pawn))
                    {
                        RedressPawn(pawn, req);
                        Find.WorldPawns.RemovePawn(pawn);
                    }
                }
                if (pawn == null && req.Inhabitant && req.Tile != -1)
                {
                    SettlementBase settlement = Find.WorldObjects.WorldObjectAt<SettlementBase>(req.Tile);
                    if (settlement != null && settlement.previouslyGeneratedInhabitants.Any())
                    {
                        IEnumerable<Pawn> validCandidatesToRedress2 = GetValidCandidatesToRedress(req);
                        if ((from x in validCandidatesToRedress2
                             where settlement.previouslyGeneratedInhabitants.Contains(x)
                             select x).TryRandomElementByWeight(WorldPawnSelectionWeight, out pawn))
                        {
                            RedressPawn(pawn, req);
                            Find.WorldPawns.RemovePawn(pawn);
                        }
                    }
                }
                if (pawn == null && Rand.Chance(ChanceToRedressAnyWorldPawn(req)))
                {
                    IEnumerable<Pawn> validCandidatesToRedress3 = GetValidCandidatesToRedress(req);
                    if (validCandidatesToRedress3.TryRandomElementByWeight(WorldPawnSelectionWeight, out pawn))
                    {
                        RedressPawn(pawn, req);
                        Find.WorldPawns.RemovePawn(pawn);
                    }
                }
            }
            bool redressed;
            #endregion redress
            MiscUtility.LogMessage("GenerateOrRedressPawn: 3");
            if (pawn == null)
            {
                MiscUtility.LogMessage("GenerateOrRedressPawn: 3.5");
                redressed = false;
                pawn = GenerateNewPawnInternal(ref req);
                if (pawn == null)
                {
                    MiscUtility.LogMessage("GenerateOrRedressPawn: 3.50");
                    return null;
                }
                if (req.Inhabitant && req.Tile != -1)
                {
                    MiscUtility.LogMessage("GenerateOrRedressPawn: 3.55");
                    SettlementBase settlementBase = Find.WorldObjects.WorldObjectAt<SettlementBase>(req.Tile);
                    settlementBase?.previouslyGeneratedInhabitants.Add(pawn);
                }
            }
            else
            {
                redressed = true;
            }
            MiscUtility.LogMessage("GenerateOrRedressPawn: 5");
            if (Find.Scenario != null)
            {
                Find.Scenario.Notify_PawnGenerated(pawn, req.Context, redressed);
            }
            MiscUtility.LogMessage("GenerateOrRedressPawn: 4");
            return pawn;
        }

        private static Pawn GenerateNewPawnInternal(ref PawnGenerationRequest req)
        {
            Pawn pawn = null;
            string text = null;
            bool ignoreScenarioRequirements = false;
            bool ignoreValidator = false;
            for (int i = 0; i < MaxTries; i++)
            {
                if (i == 70)
                {
                    MiscUtility.LogError("Could not generate a pawn after " + 70 + " tries. Last error: " + text + " Ignoring scenario requirements.", false);
                    ignoreScenarioRequirements = true;
                }
                if (i == 100)
                {
                    MiscUtility.LogError("Could not generate a pawn after " + 100 + " tries. Last error: " + text + " Ignoring validator.", false);
                    ignoreValidator = true;
                }
                PawnGenerationRequest pawnGenerationRequest = req;
                pawn = TryGenerateNewPawnInternal(ref pawnGenerationRequest, out text, ignoreScenarioRequirements, ignoreValidator);
                if (pawn != null)
                {
                    req = pawnGenerationRequest;
                    break;
                }
            }
            if (pawn == null)
            {
                MiscUtility.LogError("Pawn generation error: " + text + " Too many tries (" + MaxTries + "), returning null. Generation req: " + req, false);
                return null;
            }
            return pawn;
        }

        private static Pawn TryGenerateNewPawnInternal(ref PawnGenerationRequest req, out string error, bool ignoreScenarioRequirements, bool ignoreValidator)
        {
            error = null;
            PawnKindDefME extension = req.KindDef.GetModExtension<PawnKindDefME>();
            Pawn pawn = (Pawn)ThingMaker.MakeThing(ThingDefOf.Human, null); //extension.raceWeights.RandomElementByWeightWithFallback(x => x.weight, ThingWeight.Human).def
            pawnsBeingGenerated.Add(new PawnGenerationStatus(pawn, null));
            try
            {
                pawn.kindDef = req.KindDef;
                pawn.SetFactionDirect(req.Faction);
                PawnComponentsUtility.CreateInitialComponents(pawn);
                if (req.FixedGender.HasValue)
                {
                    pawn.gender = req.FixedGender.Value;
                }
                else if (pawn.RaceProps.hasGenders)
                {
                    if (Rand.Value < req.KindDef.GetModExtension<PawnKindDefME>().maleProportion) pawn.gender = Gender.Male;
                    else pawn.gender = Gender.Female;
                }
                else
                {
                    pawn.gender = Gender.None;
                }
                GenerateRandomAge(pawn, req);
                pawn.needs.SetInitialLevels();
                if (!req.Newborn && req.CanGeneratePawnRelations)
                {
                    GeneratePawnRelations(pawn, ref req);
                }
                if (pawn.RaceProps.Humanlike)
                {
                    Faction faction;
                    FactionDef factionType = (req.Faction == null) ? ((!Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out faction, false, true, TechLevel.Undefined)) ? Faction.OfAncients.def : faction.def) : req.Faction.def;
                    pawn.story.melanin = ((!req.FixedMelanin.HasValue) ? PawnSkinColors.RandomMelanin(req.Faction) : req.FixedMelanin.Value);
                    /*
                    if (req.FixedMelanin.HasValue)
                    {
                        pawn.story.melanin = req.FixedMelanin.Value)
                    }
                    else if ((bool)extension?.centralMelanin.HasValue && (bool)extension?.melaninVariance.HasValue)
                    {
                        pawn.story.melanin = RandomMelanin(extension);
                    }
                    else
                    {
                        pawn.story.melanin = PawnSkinColors.RandomMelanin(req.Faction)
                    }*/
                    pawn.story.crownType = ((Rand.Value < 0.5f) ? CrownType.Average : CrownType.Narrow);
                    pawn.story.hairColor = PawnHairColors.RandomHairColor(pawn.story.SkinColor, pawn.ageTracker.AgeBiologicalYears);
                    PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo(pawn, req.FixedLastName, factionType); //TODO: set name/bio chances
                    pawn.story.hairDef = PawnHairChooser.RandomHairDefFor(pawn, factionType); //TODO: set hair tags by pawn
                    GenerateTraits(pawn, req);
                    GenerateBodyType(pawn);
                    GenerateSkills(pawn);
                }
                if (pawn.RaceProps.Animal && req.Faction != null && req.Faction.IsPlayer)
                {
                    pawn.training.SetWantedRecursive(TrainableDefOf.Tameness, true);
                    pawn.training.Train(TrainableDefOf.Tameness, null, true);
                }
                GenerateInitialHediffs(pawn, req);
                if (pawn.workSettings != null && req.Faction != null && req.Faction.IsPlayer)
                {
                    pawn.workSettings.EnableAndInitialize();
                }
                if (req.Faction != null && pawn.RaceProps.Animal)
                {
                    pawn.GenerateNecessaryName();
                }
                if (Find.Scenario != null)
                {
                    Find.Scenario.Notify_NewPawnGenerating(pawn, req.Context);
                }
                if (!req.AllowDead && (pawn.Dead || pawn.Destroyed))
                {
                    DiscardGeneratedPawn(pawn);
                    error = "Generated dead pawn.";
                    return null;
                }
                if (!req.AllowDowned && pawn.Downed)
                {
                    DiscardGeneratedPawn(pawn);
                    error = "Generated downed pawn.";
                    return null;
                }
                if (req.MustBeCapableOfViolence)
                {
                    if (pawn.story != null && pawn.story.WorkTagIsDisabled(WorkTags.Violent))
                    {
                        DiscardGeneratedPawn(pawn);
                        error = "Generated pawn incapable of violence.";
                        return null;
                    }
                    if (pawn.RaceProps.ToolUser && !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                    {
                        DiscardGeneratedPawn(pawn);
                        error = "Generated pawn incapable of violence.";
                        return null;
                    }
                }
                if (!ignoreScenarioRequirements && req.Context == PawnGenerationContext.PlayerStarter && Find.Scenario != null && !Find.Scenario.AllowPlayerStartingPawn(pawn, false, req))
                {
                    DiscardGeneratedPawn(pawn);
                    error = "Generated pawn doesn't meet scenario requirements.";
                    return null;
                }
                if (!ignoreValidator && req.ValidatorPreGear != null && !req.ValidatorPreGear(pawn))
                {
                    DiscardGeneratedPawn(pawn);
                    error = "Generated pawn didn't pass validator check (pre-gear).";
                    return null;
                }
                if (!req.Newborn)
                {
                    GenerateGearFor(pawn, req);
                }
                if (!ignoreValidator && req.ValidatorPostGear != null && !req.ValidatorPostGear(pawn))
                {
                    DiscardGeneratedPawn(pawn);
                    error = "Generated pawn didn't pass validator check (post-gear).";
                    return null;
                }
                for (int i = 0; i < pawnsBeingGenerated.Count - 1; i++)
                {
                    if (pawnsBeingGenerated[i].PawnsGeneratedInTheMeantime == null)
                    {
                        pawnsBeingGenerated[i] = new PawnGenerationStatus(pawnsBeingGenerated[i].Pawn, new List<Pawn>());
                    }
                    pawnsBeingGenerated[i].PawnsGeneratedInTheMeantime.Add(pawn);
                }
                return pawn;
            }
            finally
            {
                pawnsBeingGenerated.RemoveLast();
            }
        }

        private static void GenerateRandomAge(Pawn pawn, PawnGenerationRequest request)
        {
            PawnKindDefME extension = request.KindDef.GetModExtension<PawnKindDefME>();
            if (request.FixedBiologicalAge.HasValue && request.FixedChronologicalAge.HasValue)
            {
                float? fixedBiologicalAge = request.FixedBiologicalAge;
                bool hasValue = fixedBiologicalAge.HasValue;
                float? fixedChronologicalAge = request.FixedChronologicalAge;
                if ((hasValue & fixedChronologicalAge.HasValue) && fixedBiologicalAge.GetValueOrDefault() > fixedChronologicalAge.GetValueOrDefault())
                {
                    MiscUtility.LogWarning("Tried to generate age for pawn " + pawn + ", but pawn generation request demands biological age (" + request.FixedBiologicalAge + ") to be greater than chronological age (" + request.FixedChronologicalAge + ").", false);
                }
            }
            if (request.Newborn)
            {
                pawn.ageTracker.AgeBiologicalTicks = 0L;
            }
            else if (request.FixedBiologicalAge.HasValue)
            {
                pawn.ageTracker.AgeBiologicalTicks = (long)(request.FixedBiologicalAge.Value * 3600000f);
            }
            else if (extension?.ageCurve != null)
            {
                pawn.ageTracker.AgeBiologicalTicks = (long)(Rand.ByCurve(extension.ageCurve) * GenDate.TicksPerYear);
            }
            else
            {
                float age = 0f;
                int ct = 0;
                do
                {
                    age = ((pawn.RaceProps.ageGenerationCurve == null) ? ((!pawn.RaceProps.IsMechanoid) ? (Rand.ByCurve(DefaultAgeGenerationCurve) * pawn.RaceProps.lifeExpectancy) : Rand.Range(0f, 2500f)) : ((float)Mathf.RoundToInt(Rand.ByCurve(pawn.RaceProps.ageGenerationCurve))));
                    ct++;
                    if (ct > 300)
                    {
                        MiscUtility.LogError("Tried 300 times to generate age for " + pawn, false);
                        break;
                    }
                }
                while (age > (float)pawn.kindDef.maxGenerationAge || age < (float)pawn.kindDef.minGenerationAge);
                pawn.ageTracker.AgeBiologicalTicks = (long)(age * 3600000f) + Rand.Range(0, 3600000);
            }
            if (request.Newborn)
            {
                pawn.ageTracker.AgeChronologicalTicks = 0L;
            }
            else if (request.FixedChronologicalAge.HasValue)
            {
                pawn.ageTracker.AgeChronologicalTicks = (long)(request.FixedChronologicalAge.Value * 3600000f);
            }
            //TODO: incorporate cryptosleep shit into agecurve
            else
            {
                int num3;
                if (request.CertainlyBeenInCryptosleep || Rand.Value < pawn.kindDef.backstoryCryptosleepCommonality)
                {
                    float value = Rand.Value;
                    if (value < 0.7f)
                    {
                        num3 = Rand.Range(0, 100);
                    }
                    else if (value < 0.95f)
                    {
                        num3 = Rand.Range(100, 1000);
                    }
                    else
                    {
                        int max = GenDate.Year(GenTicks.TicksAbs, 0f) - 2026 - pawn.ageTracker.AgeBiologicalYears;
                        num3 = Rand.Range(1000, max);
                    }
                }
                else
                {
                    num3 = 0;
                }
                int ticksAbs = GenTicks.TicksAbs;
                long num4 = ticksAbs - pawn.ageTracker.AgeBiologicalTicks;
                num4 -= (long)num3 * 3600000L;
                pawn.ageTracker.BirthAbsTicks = num4;
            }
            if (pawn.ageTracker.AgeBiologicalTicks > pawn.ageTracker.AgeChronologicalTicks)
            {
                pawn.ageTracker.AgeChronologicalTicks = pawn.ageTracker.AgeBiologicalTicks;
            }
        }

        #region GeneratePawnRelations &c
        //copy of vanilla code. TODO: allow setting pawn relations somehow?

        private static PawnRelationDef[] relationsGeneratableBlood = (from rel in DefDatabase<PawnRelationDef>.AllDefsListForReading
                                                                      where rel.familyByBloodRelation && rel.generationChanceFactor > 0f
                                                                      select rel).ToArray();

        private static PawnRelationDef[] relationsGeneratableNonblood = (from rel in DefDatabase<PawnRelationDef>.AllDefsListForReading
                                                                         where !rel.familyByBloodRelation && rel.generationChanceFactor > 0f
                                                                         select rel).ToArray();

        private static void GeneratePawnRelations(Pawn pawn, ref PawnGenerationRequest request)
        {
            if (pawn.RaceProps.Humanlike)
            {
                Pawn[] array = (from x in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead
                                where x.def == pawn.def
                                select x).ToArray();
                if (array.Length != 0)
                {
                    int num = 0;
                    Pawn[] array2 = array;
                    foreach (Pawn pawn2 in array2)
                    {
                        if (pawn2.Discarded)
                        {
                            MiscUtility.LogWarning("Warning during generating pawn relations for " + pawn + ": Pawn " + pawn2 + " is discarded, yet he was yielded by PawnUtility. Discarding a pawn means that he is no longer managed by anything.", false);
                        }
                        else if (pawn2.Faction != null && pawn2.Faction.IsPlayer)
                        {
                            num++;
                        }
                    }
                    float num2 = 45f;
                    num2 += (float)num * 2.7f;
                    PawnGenerationRequest localReq = request;
                    Pair<Pawn, PawnRelationDef> pair = GenerateSamples(array, relationsGeneratableBlood, 40).RandomElementByWeightWithDefault((Pair<Pawn, PawnRelationDef> x) => x.Second.generationChanceFactor * x.Second.Worker.GenerationChance(pawn, x.First, localReq), num2 * 40f / (float)(array.Length * relationsGeneratableBlood.Length));
                    if (pair.First != null)
                    {
                        pair.Second.Worker.CreateRelation(pawn, pair.First, ref request);
                    }
                    Pair<Pawn, PawnRelationDef> pair2 = GenerateSamples(array, relationsGeneratableNonblood, 40).RandomElementByWeightWithDefault((Pair<Pawn, PawnRelationDef> x) => x.Second.generationChanceFactor * x.Second.Worker.GenerationChance(pawn, x.First, localReq), num2 * 40f / (float)(array.Length * relationsGeneratableNonblood.Length));
                    if (pair2.First != null)
                    {
                        pair2.Second.Worker.CreateRelation(pawn, pair2.First, ref request);
                    }
                }
            }
        }

        private static Pair<Pawn, PawnRelationDef>[] GenerateSamples(Pawn[] pawns, PawnRelationDef[] relations, int count)
        {
            Pair<Pawn, PawnRelationDef>[] array = new Pair<Pawn, PawnRelationDef>[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = new Pair<Pawn, PawnRelationDef>(pawns[Rand.Range(0, pawns.Length)], relations[Rand.Range(0, relations.Length)]);
            }
            return array;
        }
        #endregion GeneratePawnRelations &c

        private static void GenerateTraits(Pawn pawn, PawnGenerationRequest request)
        {
            if (pawn.story != null)
            {
                if (pawn.story.childhood.forcedTraits != null)
                {
                    List<TraitEntry> forcedTraits = pawn.story.childhood.forcedTraits;
                    for (int i = 0; i < forcedTraits.Count; i++)
                    {
                        TraitEntry traitEntry = forcedTraits[i];
                        if (traitEntry.def == null)
                        {
                            MiscUtility.LogError("Null forced trait def on " + pawn.story.childhood, false);
                        }
                        else if (!pawn.story.traits.HasTrait(traitEntry.def))
                        {
                            pawn.story.traits.GainTrait(new Trait(traitEntry.def, traitEntry.degree, false));
                        }
                    }
                }
                if (pawn.story.adulthood != null && pawn.story.adulthood.forcedTraits != null)
                {
                    List<TraitEntry> forcedTraits2 = pawn.story.adulthood.forcedTraits;
                    for (int j = 0; j < forcedTraits2.Count; j++)
                    {
                        TraitEntry traitEntry2 = forcedTraits2[j];
                        if (traitEntry2.def == null)
                        {
                            MiscUtility.LogError("Null forced trait def on " + pawn.story.adulthood, false);
                        }
                        else if (!pawn.story.traits.HasTrait(traitEntry2.def))
                        {
                            pawn.story.traits.GainTrait(new Trait(traitEntry2.def, traitEntry2.degree, false));
                        }
                    }
                }
                int num = Rand.RangeInclusive(2, 3);
                //TODO: Psychology Kinsey compat
                //      complete restructure. Generate a trait pool for the PawnKindDef (perhaps cache at beginning of group generation) and override weight as appropriate
                if (request.AllowGay && (LovePartnerRelationUtility.HasAnyLovePartnerOfTheSameGender(pawn) || LovePartnerRelationUtility.HasAnyExLovePartnerOfTheSameGender(pawn)))
                {
                    Trait trait = new Trait(TraitDefOf.Gay, RandomTraitDegree(pawn, TraitDefOf.Gay), false);
                    pawn.story.traits.GainTrait(trait);
                }
                while (pawn.story.traits.allTraits.Count < num)
                {
                    TraitDef newTraitDef = DefDatabase<TraitDef>.AllDefsListForReading.RandomElementByWeight((TraitDef tr) => tr.GetGenderSpecificCommonality(pawn.gender));
                    Trait trait2;
                    if (!pawn.story.traits.HasTrait(newTraitDef) && (newTraitDef != TraitDefOf.Gay || (request.AllowGay && !LovePartnerRelationUtility.HasAnyLovePartnerOfTheOppositeGender(pawn) && !LovePartnerRelationUtility.HasAnyExLovePartnerOfTheOppositeGender(pawn))) && (request.Faction == null || Faction.OfPlayerSilentFail == null || !request.Faction.HostileTo(Faction.OfPlayer) || newTraitDef.allowOnHostileSpawn) && !pawn.story.traits.allTraits.Any((Trait tr) => newTraitDef.ConflictsWith(tr)) && (newTraitDef.conflictingTraits == null || !newTraitDef.conflictingTraits.Any((TraitDef tr) => pawn.story.traits.HasTrait(tr))) && (newTraitDef.requiredWorkTypes == null || !pawn.story.OneOfWorkTypesIsDisabled(newTraitDef.requiredWorkTypes)) && !pawn.story.WorkTagIsDisabled(newTraitDef.requiredWorkTags))
                    {
                        int degree = RandomTraitDegree(pawn, newTraitDef);
                        if (!pawn.story.childhood.DisallowsTrait(newTraitDef, degree) && (pawn.story.adulthood == null || !pawn.story.adulthood.DisallowsTrait(newTraitDef, degree)))
                        {
                            trait2 = new Trait(newTraitDef, degree, false);
                            if (pawn.mindState != null && pawn.mindState.mentalBreaker != null)
                            {
                                float breakThresholdExtreme = pawn.mindState.mentalBreaker.BreakThresholdExtreme;
                                breakThresholdExtreme += trait2.OffsetOfStat(StatDefOf.MentalBreakThreshold);
                                breakThresholdExtreme *= trait2.MultiplierOfStat(StatDefOf.MentalBreakThreshold);
                                if (!(breakThresholdExtreme > 0.4f))
                                {
                                    pawn.story.traits.GainTrait(trait2);
                                }
                                continue;
                            }
                            pawn.story.traits.GainTrait(trait2);
                        }
                    }
                    continue;                    
                }
            }
        }
        private static void DiscardGeneratedPawn(Pawn pawn)
        {
            if (Find.WorldPawns.Contains(pawn))
            {
                Find.WorldPawns.RemovePawn(pawn);
            }
            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
            List<Pawn> pawnsGeneratedInTheMeantime = pawnsBeingGenerated.Last().PawnsGeneratedInTheMeantime;
            if (pawnsGeneratedInTheMeantime != null)
            {
                for (int i = 0; i < pawnsGeneratedInTheMeantime.Count; i++)
                {
                    Pawn pawn2 = pawnsGeneratedInTheMeantime[i];
                    if (Find.WorldPawns.Contains(pawn2))
                    {
                        Find.WorldPawns.RemovePawn(pawn2);
                    }
                    Find.WorldPawns.PassToWorld(pawn2, PawnDiscardDecideMode.Discard);
                    for (int j = 0; j < pawnsBeingGenerated.Count; j++)
                    {
                        pawnsBeingGenerated[j].PawnsGeneratedInTheMeantime.Remove(pawn2);
                    }
                }
            }
        }
        private static void GenerateBodyType(Pawn pawn)
        {
            PawnKindDefME ext;
            if (pawn.story.adulthood != null)
            {
                pawn.story.bodyType = pawn.story.adulthood.BodyTypeFor(pawn.gender);
            }
            else if ((ext = pawn.kindDef.GetModExtension<PawnKindDefME>()) != null && ext.BodyTypeWeights != null)
            {
                if (pawn.gender == Gender.Female)
                {
                    ext.BodyTypeWeights.TryRandomElementByWeight(x => x.femaleWeight, out BodyTypeWeight result);
                    pawn.story.bodyType = result.type;
                }
                else
                {
                    ext.BodyTypeWeights.TryRandomElementByWeight(x => x.maleWeight, out BodyTypeWeight result);
                    pawn.story.bodyType = result.type;
                }
            }
            else if (Rand.Value < 0.5f)
            {
                pawn.story.bodyType = BodyTypeDefOf.Thin;
            }
            else
            {
                pawn.story.bodyType = ((pawn.gender != Gender.Female) ? BodyTypeDefOf.Male : BodyTypeDefOf.Female);
            }
        }
        private static void GenerateInitialHediffs(Pawn pawn, PawnGenerationRequest request)
        {
            int num = 0;
            while (true)
            {
                AgeInjuryUtility.GenerateRandomOldAgeInjuries(pawn, !request.AllowDead);
                PawnTechHediffsGenerator.GenerateTechHediffsFor(pawn);
                PawnAddictionHediffsGenerator.GenerateAddictionsAndTolerancesFor(pawn);
                if (request.AllowDead && pawn.Dead)
                {
                    break;
                }
                if (!request.AllowDowned && pawn.Downed)
                {
                    pawn.health.Reset();
                    num++;
                    if (num <= 80)
                    {
                        continue;
                    }
                    Log.Warning("Could not generate old age injuries for " + pawn.ThingID + " of age " + pawn.ageTracker.AgeBiologicalYears + " that allow pawn to move after " + 80 + " tries. request=" + request, false);
                }
                break;
            }
            if (!pawn.Dead)
            {
                if (request.Faction != null && request.Faction.IsPlayer)
                {
                    return;
                }
                int num2 = 0;
                while (true)
                {
                    if (pawn.health.HasHediffsNeedingTend(false))
                    {
                        num2++;
                        if (num2 <= 10000)
                        {
                            TendUtility.DoTend(null, pawn, null);
                            continue;
                        }
                        break;
                    }
                    return;
                }
                MiscUtility.LogError("Too many iterations.", false);
            }
        }
        private static void GenerateGearFor(Pawn pawn, PawnGenerationRequest request)
        {
            PawnKindDefME ext = pawn.kindDef.GetModExtension<PawnKindDefME>();
            /*
            if (ext != null)
            {
                //TODO: uniforms and service rifles
                //      also, this entire feature
                if (ext.ApparelWeights != null)
                {
                    GenerateStartingApparelFor(pawn, request);
                }
                else
                {
                    PawnApparelGenerator.GenerateStartingApparelFor(pawn, request);
                }
            }
            else
            {*/
                PawnApparelGenerator.GenerateStartingApparelFor(pawn, request);
                PawnWeaponGenerator.TryGenerateWeaponFor(pawn);
            //}
            PawnInventoryGenerator.GenerateInventoryFor(pawn, request);
        }
        public static int RandomTraitDegree(Pawn pawn, TraitDef traitDef)
        {
            //PawnKindDefME ext = pawn.kindDef.GetModExtension<PawnKindDefME>();
            if (traitDef.degreeDatas.Count == 1)
            {
                return traitDef.degreeDatas[0].degree;
            }
            return traitDef.degreeDatas.RandomElementByWeight((TraitDegreeData dd) => dd.commonality).degree;
        }
        #region skills
        private static void GenerateSkills(Pawn pawn)
        {
            List<SkillDef> allDefsListForReading = DefDatabase<SkillDef>.AllDefsListForReading;
            for (int i = 0; i < allDefsListForReading.Count; i++)
            {
                SkillDef skillDef = allDefsListForReading[i];
                int num = FinalLevelOfSkill(pawn, skillDef);
                SkillRecord skill = pawn.skills.GetSkill(skillDef);
                skill.Level = num;
                if (!skill.TotallyDisabled)
                {
                    float num2 = (float)num * 0.11f;
                    float value = Rand.Value;
                    if (value < num2)
                    {
                        if (value < num2 * 0.2f)
                        {
                            skill.passion = Passion.Major;
                        }
                        else
                        {
                            skill.passion = Passion.Minor;
                        }
                    }
                    skill.xpSinceLastLevel = Rand.Range(skill.XpRequiredForLevelUp * 0.1f, skill.XpRequiredForLevelUp * 0.9f);
                }
            }
        }
        private static int FinalLevelOfSkill(Pawn pawn, SkillDef sk)
        {
            PawnKindDefME ext = pawn.kindDef.GetModExtension<PawnKindDefME>();
            float num;
            bool useExt = ext != null && ext.SkillRanges != null && ext.SkillRanges.Where(x => x.def == sk).Any();
            SkillRange range = null;
            if (useExt)
            {
                range = ext.SkillRanges.First(x => x.def == sk);
                num = range.range.RandomInRange;
            }
            else
            {
                num = (!sk.usuallyDefinedInBackstories) ? Rand.ByCurve(LevelRandomCurve) : ((float)Rand.RangeInclusive(0, 4));
            }            
            foreach (Backstory item in from bs in pawn.story.AllBackstories
                                       where bs != null
                                       select bs)
            {
                foreach (KeyValuePair<SkillDef, int> item2 in item.skillGainsResolved)
                {
                    if (item2.Key == sk)
                    {
                        num += (float)item2.Value * Rand.Range(1f, 1.4f);
                    }
                }
            }
            for (int i = 0; i < pawn.story.traits.allTraits.Count; i++)
            {
                int num2 = 0;
                if (pawn.story.traits.allTraits[i].CurrentData.skillGains.TryGetValue(sk, out num2))
                {
                    num += (float)num2;
                }
            }
            float num3 = Rand.Range(1f, AgeSkillMaxFactorCurve.Evaluate((float)pawn.ageTracker.AgeBiologicalYears));
            num *= num3;
            num = LevelFinalAdjustmentCurve.Evaluate(num);
            if (useExt && range.clampToRange) return Mathf.Clamp(Mathf.RoundToInt(num), range.range.min, range.range.max);
            else return Mathf.Clamp(Mathf.RoundToInt(num), 0, 20);
        }
        #endregion skills
        #region redress
        private static IEnumerable<Pawn> GetValidCandidatesToRedress(PawnGenerationRequest request)
        {
            IEnumerable<Pawn> enumerable = Find.WorldPawns.GetPawnsBySituation(WorldPawnSituation.Free);
            if (request.KindDef.factionLeader)
            {
                enumerable = enumerable.Concat(Find.WorldPawns.GetPawnsBySituation(WorldPawnSituation.FactionLeader));
            }
            return from x in enumerable
                   where IsValidCandidateToRedress(x, request)
                   select x;
        }
        private static bool IsValidCandidateToRedress(Pawn pawn, PawnGenerationRequest request)
        {
            if (pawn.def != request.KindDef.race) return false;
            if (!request.WorldPawnFactionDoesntMatter && pawn.Faction != request.Faction) return false;
            if (!request.AllowDead && (pawn.Dead || pawn.Destroyed)) return false;
            if (!request.AllowDowned && pawn.Downed) return false;
            if (pawn.health.hediffSet.BleedRateTotal > 0.001f) return false;
            if (!request.CanGeneratePawnRelations && pawn.RaceProps.IsFlesh && pawn.relations.RelatedToAnyoneOrAnyoneRelatedToMe) return false;
            if (!request.AllowGay && pawn.RaceProps.Humanlike && pawn.story.traits.HasTrait(TraitDefOf.Gay)) return false;
            if (request.ValidatorPreGear != null && !request.ValidatorPreGear(pawn)) return false;
            if (request.ValidatorPostGear != null && !request.ValidatorPostGear(pawn)) return false;
            if (request.FixedBiologicalAge.HasValue && pawn.ageTracker.AgeBiologicalYearsFloat != request.FixedBiologicalAge) return false;
            if (request.FixedChronologicalAge.HasValue && (float)pawn.ageTracker.AgeChronologicalYears != request.FixedChronologicalAge) return false;
            if (request.FixedGender.HasValue && pawn.gender != request.FixedGender) return false;
            if (request.FixedLastName != null && ((NameTriple)pawn.Name).Last != request.FixedLastName) return false;
            if (request.FixedMelanin.HasValue && pawn.story != null && pawn.story.melanin != request.FixedMelanin) return false;
            if (request.Context == PawnGenerationContext.PlayerStarter && Find.Scenario != null && !Find.Scenario.AllowPlayerStartingPawn(pawn, true, request))return false;
            if (request.MustBeCapableOfViolence)
            {
                if (pawn.story != null && pawn.story.WorkTagIsDisabled(WorkTags.Violent)) return false;
                if (pawn.RaceProps.ToolUser && !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) return false;
            }
            return true;
        }

        private static float ChanceToRedressAnyWorldPawn(PawnGenerationRequest request)
        {
            int pawnsBySituationCount = Find.WorldPawns.GetPawnsBySituationCount(WorldPawnSituation.Free);
            float num = Mathf.Min(0.02f + 0.01f * ((float)pawnsBySituationCount / 10f), 0.8f); //TODO: unhardcode?
            if (request.MinChanceToRedressWorldPawn.HasValue)
            {
                num = Mathf.Max(num, request.MinChanceToRedressWorldPawn.Value);
            }
            return num;
        }

        public static void RedressPawn(Pawn pawn, PawnGenerationRequest request)
        {
            try
            {
                pawn.ChangeKind(request.KindDef);
                GenerateGearFor(pawn, request);
                if (pawn.Faction != request.Faction)
                {
                    pawn.SetFaction(request.Faction, null);
                }
                if (pawn.guest != null)
                {
                    pawn.guest.SetGuestStatus(null, false);
                }
            }
            finally
            {
            }
        }
        #endregion redress
    }
}
