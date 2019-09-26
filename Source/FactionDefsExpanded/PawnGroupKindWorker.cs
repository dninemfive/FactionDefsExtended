using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace D9Extended
{
    class PawnGroupKindWorker_FE : PawnGroupKindWorker
    {
        public PawnGroupKindWorkerME modExtension => def.GetModExtension<PawnGroupKindWorkerME>();
        public bool hasModExtension => def.HasModExtension<PawnGroupKindWorkerME>();
        bool isTrader => modExtension.type == PawnGroupKindWorkerME.Type.Trader;

        public override float MinPointsToGenerateAnything(PawnGroupMaker groupMaker)
        {
            return (from x in groupMaker.options
                    where x.kind.isFighter
                    select x).Min((PawnGenOption g) => g.Cost);
        }

        protected override void GeneratePawns(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Pawn> outPawns, bool errorOnZeroResults = true)
        {
            if (!CanGenerateFrom(parms, groupMaker))
            {
                if (errorOnZeroResults)
                {
                    MiscUtility.LogError("Cannot generate pawns for " + parms.faction + " with " + parms.points + ". Defaulting to a single random cheap group.", false);
                }
            }
            else {
                if (!isTrader)
                {
                    bool flag = parms.raidStrategy == null || parms.raidStrategy.pawnsCanBringFood || (parms.faction != null && !parms.faction.HostileTo(Faction.OfPlayer));
                    Predicate<Pawn> predicate = (parms.raidStrategy == null) ? null : ((Predicate<Pawn>)((Pawn p) => parms.raidStrategy.Worker.CanUsePawn(p, outPawns)));
                    bool flag2 = false;
                    foreach (PawnGenOption item in PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints(parms.points, groupMaker.options, parms))
                    {
                        PawnKindDef kind = item.kind;
                        Faction faction = parms.faction;
                        int tile = parms.tile;
                        bool allowFood = flag;
                        bool inhabitants = parms.inhabitants;
                        Predicate<Pawn> validatorPostGear = predicate;
                        PawnGenerationRequest request = new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer, tile, false, false, false, false, true, true, 1f, false, true, allowFood, inhabitants, false, false, false, null, validatorPostGear, null, null, null, null, null, null);
                        Pawn pawn = D9Extended.PawnGenerator.GeneratePawn(request);
                        if (parms.forceOneIncap && !flag2)
                        {
                            pawn.health.forceIncap = true;
                            pawn.mindState.canFleeIndividual = false;
                            flag2 = true;
                        }
                        outPawns.Add(pawn);
                    }
                }
                else
                {
                    if (!parms.faction.def.caravanTraderKinds.Any())
                    {
                        MiscUtility.LogError("Cannot generate trader caravan for " + parms.faction + " because it has no trader kinds.", false);
                    }
                    else
                    {
                        PawnGenOption pawnGenOption = groupMaker.traders.FirstOrDefault((PawnGenOption x) => !x.kind.trader);
                        if (pawnGenOption != null)
                        {
                            MiscUtility.LogError("Cannot generate arriving trader caravan for " + parms.faction + " because there is a pawn kind (" + pawnGenOption.kind.LabelCap + ") who is not a trader but is in a traders list.", false);
                        }
                        else
                        {
                            PawnGenOption pawnGenOption2 = groupMaker.carriers.FirstOrDefault((PawnGenOption x) => !x.kind.RaceProps.packAnimal);
                            if (pawnGenOption2 != null)
                            {
                                MiscUtility.LogError("Cannot generate arriving trader caravan for " + parms.faction + " because there is a pawn kind (" + pawnGenOption2.kind.LabelCap + ") who is not a carrier but is in a carriers list.", false);
                            }
                            else
                            {
                                if (parms.seed.HasValue)
                                {
                                    Log.Warning("[FDE] Deterministic seed not implemented for this pawn group kind worker. The result will be random anyway.", false);
                                }
                                TraderKindDef traderKindDef = (parms.traderKind == null) ? parms.faction.def.caravanTraderKinds.RandomElementByWeight((TraderKindDef traderDef) => traderDef.CalculatedCommonality) : parms.traderKind;
                                Pawn pawn = GenerateTrader(parms, groupMaker, traderKindDef);
                                outPawns.Add(pawn);
                                ThingSetMakerParams parms2 = default(ThingSetMakerParams);
                                parms2.traderDef = traderKindDef;
                                parms2.tile = parms.tile;
                                parms2.traderFaction = parms.faction;
                                List<Thing> wares = ThingSetMakerDefOf.TraderStock.root.Generate(parms2).InRandomOrder(null).ToList();
                                foreach (Pawn slavesAndAnimalsFromWare in GetSlavesAndAnimalsFromWares(parms, pawn, wares))
                                {
                                    outPawns.Add(slavesAndAnimalsFromWare);
                                }
                                GenerateCarriers(parms, groupMaker, pawn, wares, outPawns);
                                GenerateGuards(parms, groupMaker, pawn, wares, outPawns);
                            }
                        }
                    }
                }
            }
        }

        #region tradersonly
        private Pawn GenerateTrader(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, TraderKindDef traderKind)
        {
            PawnKindDef kind = groupMaker.traders.RandomElementByWeight((PawnGenOption x) => x.selectionWeight).kind;
            Faction faction = parms.faction;
            int tile = parms.tile;
            PawnGenerationRequest request = new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer, tile, false, false, false, false, true, false, 1f, false, true, true, parms.inhabitants, false, false, false, null, null, null, null, null, null, null, null);
            Pawn pawn = D9Extended.PawnGenerator.GeneratePawn(request);
            pawn.mindState.wantsToTradeWithColony = true;
            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, true);
            pawn.trader.traderKind = traderKind;
            parms.points -= pawn.kindDef.combatPower;
            return pawn;
        }
        //TODO: custom StockGenerator_Slaves allowing setting custom PawnKinds as slaves
        private IEnumerable<Pawn> GetSlavesAndAnimalsFromWares(PawnGroupMakerParms parms, Pawn trader, List<Thing> wares)
        {
            for (int i = 0; i < wares.Count; i++)
            {
                Pawn p = wares[i] as Pawn;
                if (p != null)
                {
                    if (p.Faction != parms.faction)
                    {
                        p.SetFaction(parms.faction, null);
                    }
                    yield return p;
                }
            }
        }
        private void GenerateCarriers(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, Pawn trader, List<Thing> wares, List<Pawn> outPawns)
        {
            List<Thing> list = (from x in wares
                                where !(x is Pawn)
                                select x).ToList();
            int i = 0;
            int num = Mathf.CeilToInt((float)list.Count / 8f);
            PawnKindDef kind = (from x in groupMaker.carriers
                                where parms.tile == -1 || Find.WorldGrid[parms.tile].biome.IsPackAnimalAllowed(x.kind.race)
                                select x).RandomElementByWeight((PawnGenOption x) => x.selectionWeight).kind;
            List<Pawn> list2 = new List<Pawn>();
            for (int j = 0; j < num; j++)
            {
                PawnKindDef kind2 = kind;
                Faction faction = parms.faction;
                int tile = parms.tile;
                PawnGenerationRequest request = new PawnGenerationRequest(kind2, faction, PawnGenerationContext.NonPlayer, tile, false, false, false, false, true, false, 1f, false, true, true, parms.inhabitants, false, false, false, null, null, null, null, null, null, null, null);
                Pawn pawn = D9Extended.PawnGenerator.GeneratePawn(request);
                if (i < list.Count)
                {
                    pawn.inventory.innerContainer.TryAdd(list[i], true);
                    i++;
                }
                list2.Add(pawn);
                outPawns.Add(pawn);
            }
            for (; i < list.Count; i++)
            {
                list2.RandomElement().inventory.innerContainer.TryAdd(list[i], true);
            }
        }
        private void GenerateGuards(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, Pawn trader, List<Thing> wares, List<Pawn> outPawns)
        {
            if (groupMaker.guards.Any())
            {
                float points = parms.points;
                foreach (PawnGenOption item2 in PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints(points, groupMaker.guards, parms))
                {
                    PawnKindDef kind = item2.kind;
                    Faction faction = parms.faction;
                    int tile = parms.tile;
                    bool inhabitants = parms.inhabitants;
                    PawnGenerationRequest request = new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer, tile, false, false, false, false, true, true, 1f, false, true, true, inhabitants, false, false, false, null, null, null, null, null, null, null, null);
                    Pawn item = PawnGenerator.GeneratePawn(request);
                    outPawns.Add(item);
                }
            }
        }
        #endregion tradersonly

        public override bool CanGenerateFrom(PawnGroupMakerParms parms, PawnGroupMaker groupMaker)
        {
            if (hasModExtension && modExtension.type == PawnGroupKindWorkerME.Type.Trader) return base.CanGenerateFrom(parms, groupMaker) && groupMaker.traders.Any() && (parms.tile == -1 || groupMaker.carriers.Any((PawnGenOption x) => Find.WorldGrid[parms.tile].biome.IsPackAnimalAllowed(x.kind.race)));
            if (!base.CanGenerateFrom(parms, groupMaker))
            {
                return false;
            }
            if (!PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints(parms.points, groupMaker.options, parms).Any())
            {
                return false;
            }
            return true;
        }

        public override IEnumerable<PawnKindDef> GeneratePawnKindsExample(PawnGroupMakerParms parms, PawnGroupMaker groupMaker)
        {
            if (isTrader)
            {
                Log.Message("PawnGroupKindWorker_FE.GeneratePawnKindsExample: Hey, this is just how the base game does it!");
                throw new NotImplementedException();
            }
            else
            {
                foreach (PawnGenOption p in PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints(parms.points, groupMaker.options, parms))
                {
                    yield return p.kind;
                }
            }
        }
    }
}
