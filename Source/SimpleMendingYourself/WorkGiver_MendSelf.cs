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
            if (pawn.apparel != null)
                foreach (Apparel apparel in pawn.apparel.WornApparel)
                    if (QuickIsValidTarget(apparel, settings))
                        return false;
            if (pawn.equipment != null)
                foreach (ThingWithComps weapon in pawn.equipment.AllEquipmentListForReading)
                    if (QuickIsValidTarget(weapon, settings))
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

            // 逐个遍历候选物品，跳过材料不足或无法预订的，找到第一个可行的
            bool foundCandidate = false;
            Thing firstFailItem = null;

            foreach (Thing item in CandidateItems(pawn, repairComp))
            {
                foundCandidate = true;
                if (!RepairUtilities.TryGetRepairCostOnMap(item, pawn.Map, out Thing costThing, out int count))
                {
                    if (forced && firstFailItem == null) firstFailItem = item;
                    continue;
                }
                if (!pawn.CanReserve(costThing, 1, count))
                {
                    if (forced && firstFailItem == null) firstFailItem = item;
                    continue;
                }

                job = JobMaker.MakeJob(MendSelfDefOf.SMY_MendSelf, bench, costThing, item);
                job.count = count;
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

        // 枚举所有通过filter的候选物品（不含材料检查，让TryCreateJob逐个尝试）
        private IEnumerable<Thing> CandidateItems(Pawn pawn, CompRepairAssignment comp)
        {
            MendSelfSettings settings = MendSelfMod.Settings;
            if (pawn.apparel != null)
                foreach (Apparel apparel in pawn.apparel.WornApparel)
                    if (IsValidMendTarget(apparel, comp, settings))
                        yield return apparel;
            if (pawn.equipment != null)
                foreach (ThingWithComps weapon in pawn.equipment.AllEquipmentListForReading)
                    if (IsValidMendTarget(weapon, comp, settings))
                        yield return weapon;
        }

        // 预检版本：不依赖台的filter，用于ShouldSkip
        private static bool QuickIsValidTarget(Thing thing, MendSelfSettings settings)
        {
            if (!RepairUtilities.IsValidRepairTarget(thing.def))
                return false;
            float hpFrac = (float)thing.HitPoints / thing.MaxHitPoints;
            return hpFrac >= settings.mendThresholdLower && hpFrac <= settings.mendThresholdUpper;
        }

        // 完整检查：台的itemFilter（含台配置的HP%范围）和ingredientFilter，与mod设置HP%取交集
        private static bool IsValidMendTarget(Thing thing, CompRepairAssignment comp, MendSelfSettings settings)
        {
            if (!comp.itemFilter.Allows(thing))
                return false;
            float hpFrac = (float)thing.HitPoints / thing.MaxHitPoints;
            if (hpFrac < settings.mendThresholdLower || hpFrac > settings.mendThresholdUpper)
                return false;
            return RepairUtilities.IsRepairCostAllowed(thing, comp.ingredientFilter);
        }
    }
}
