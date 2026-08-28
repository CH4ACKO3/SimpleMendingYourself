using System.Collections.Generic;
using ComfyCuddlesWithEuterpe;
using RimWorld;
using Verse;
using Verse.AI;

namespace SimpleMendingYourself
{
    public class WorkGiver_MendSelf : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            MapComponent_RepairBenchCache cache = pawn.Map.GetComponent<MapComponent_RepairBenchCache>();
            if (cache != null)
            {
                foreach (Building bench in cache.GetRepairBenches())
                    yield return bench;
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (forced)
                return false;

            MendSelfSettings settings = MendSelfMod.Settings;
            foreach (Thing item in MendableItems(pawn))
                if (QuickIsValidTarget(item, settings))
                    return false;

            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
            => t is Building bench && TryCreateJob(pawn, bench, forced, out _);

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is Building bench && TryCreateJob(pawn, bench, forced, out Job job))
                return job;
            return null;
        }

        private bool TryCreateJob(Pawn pawn, Building bench, bool forced, out Job job)
        {
            job = null;
            CompRepairAssignment repairComp = bench.TryGetComp<CompRepairAssignment>();
            if (repairComp == null)
                return false;

            CompSelfMendPawnFilter pawnFilter = bench.TryGetComp<CompSelfMendPawnFilter>();
            if (pawnFilter != null && !pawnFilter.IsPawnAllowed(pawn))
            {
                if (forced) JobFailReason.Is("SMY_PawnNotAllowed".Translate());
                return false;
            }

            if (!pawn.CanReserve(bench))
            {
                if (forced)
                    JobFailReason.Is("SEX_CannotReserve".Translate(bench.Label,
                        pawn.MapHeld.reservationManager.FirstRespectedReserver(bench, pawn)?.Label ?? "?"));
                return false;
            }

            // Skip candidates with unavailable materials and use the first viable one.
            bool foundCandidate = false;
            Thing firstFailItem = null;

            foreach (Thing item in CandidateItems(pawn, repairComp))
            {
                foundCandidate = true;
                if (!RepairUtilities.TryGetRepairCost(item, pawn, pawn.Map, out List<ThingCount> chosenItems)
                    || chosenItems.Count == 0)
                {
                    if (forced && firstFailItem == null) firstFailItem = item;
                    continue;
                }
                bool canReserveAllIngredients = true;
                foreach (ThingCount chosen in chosenItems)
                {
                    if (chosen.Thing == null || chosen.Thing.Destroyed || !pawn.CanReserve(chosen.Thing, 1, chosen.Count))
                    {
                        canReserveAllIngredients = false;
                        break;
                    }
                }
                if (!canReserveAllIngredients)
                {
                    if (forced && firstFailItem == null) firstFailItem = item;
                    continue;
                }

                job = JobMaker.MakeJob(MendSelfDefOf.SMY_MendSelf);
                job.targetA = item;
                job.targetC = bench;
                job.targetQueueB = new List<LocalTargetInfo>();
                job.countQueue = new List<int>();
                foreach (ThingCount chosen in chosenItems)
                {
                    job.targetQueueB.Add(chosen.Thing);
                    job.countQueue.Add(chosen.Count);
                }
                job.count = 1;
                return true;
            }

            if (forced)
            {
                if (!foundCandidate)
                    JobFailReason.Is("SMY_NoItemsToMend".Translate());
                else if (firstFailItem != null)
                {
                    var cost = RepairUtilities.CalculateRepairCost(firstFailItem);
                    JobFailReason.Is(cost.HasValue
                        ? "SEX_NotEnough".Translate(firstFailItem.Label, cost.Value.Item2, cost.Value.Item1.label)
                        : "SEX_NoRepairCostDefined".Translate(firstFailItem.LabelNoCount));
                }
            }
            return false;
        }

        private IEnumerable<Thing> CandidateItems(Pawn pawn, CompRepairAssignment comp)
        {
            MendSelfSettings settings = MendSelfMod.Settings;
            foreach (Thing item in MendableItems(pawn))
                if (IsValidMendTarget(item, comp, settings))
                    yield return item;
        }

        private static IEnumerable<Thing> MendableItems(Pawn pawn)
        {
            if (pawn.apparel != null)
                foreach (Apparel apparel in pawn.apparel.WornApparel)
                    yield return apparel;

            if (SimpleSidearmsCompat.Active)
            {
                foreach (ThingWithComps weapon in SimpleSidearmsCompat.GetRegisteredWeapons(pawn))
                    yield return weapon;
            }
            else if (pawn.equipment != null)
            {
                foreach (ThingWithComps weapon in pawn.equipment.AllEquipmentListForReading)
                    yield return weapon;
            }
        }

        private static bool QuickIsValidTarget(Thing thing, MendSelfSettings settings)
        {
            return RepairUtilities.IsValidRepairTarget(thing.def)
                && IsWithinConfiguredHpRange(thing, settings);
        }

        private static bool IsValidMendTarget(Thing thing, CompRepairAssignment comp, MendSelfSettings settings)
        {
            if (!comp.itemFilter.Allows(thing) || !IsWithinConfiguredHpRange(thing, settings))
                return false;

            var repairCost = RepairUtilities.CalculateRepairCost(thing);
            return repairCost.HasValue
                && RepairUtilities.IsRepairCostAllowed(repairCost.Value, comp.ingredientFilter);
        }

        private static bool IsWithinConfiguredHpRange(Thing thing, MendSelfSettings settings)
        {
            float hpFraction = (float)thing.HitPoints / thing.MaxHitPoints;
            return hpFraction >= settings.mendThresholdLower && hpFraction <= settings.mendThresholdUpper;
        }
    }
}
