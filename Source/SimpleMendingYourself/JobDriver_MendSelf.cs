using System.Collections.Generic;
using ComfyCuddlesWithEuterpe;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SimpleMendingYourself
{
    public class JobDriver_MendSelf : JobDriver
    {
        private int ingredientTrackingVersion;
        private Thing Bench => job.GetTarget(TargetIndex.C).Thing;
        private Thing ItemToRepair => job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            ingredientTrackingVersion = 1;
            if (!pawn.Reserve(Bench, job, 1, -1, null, errorOnFailed))
                return false;

            List<LocalTargetInfo> targets = job.GetTargetQueue(TargetIndex.B);
            if (targets == null || targets.Count == 0)
                return false;

            for (int i = 0; i < targets.Count; i++)
            {
                int count = (job.countQueue != null && i < job.countQueue.Count) ? job.countQueue[i] : 1;
                if (!pawn.Reserve(targets[i], job, 1, count, null, errorOnFailed))
                    return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Thing bench = Bench;
            Thing itemToRepair = ItemToRepair;

            // Old saves may contain overwritten bench targets and unreliable ingredient references.
            this.FailOn(() => ingredientTrackingVersion != 1);
            this.FailOn(() => bench == null || !bench.Spawned || bench.Map != pawn.Map || bench.IsForbidden(pawn));
            this.FailOn(() => itemToRepair == null || itemToRepair.Destroyed);
            this.FailOn(() => !PlacedIngredientsValid());

            Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B);
            yield return extract;
            Toil getIngredient = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch, canGotoSpawnedParent: true)
                .FailOnForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return getIngredient;
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: true,
                failIfStackCountLessThanJobCount: true, reserve: false, canTakeFromInventory: true);
            yield return JobDriver_DoBill.JumpToCollectNextIntoHandsForBill(getIngredient, TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.InteractionCell);
            // B is no longer needed as an ingredient target until the next extraction.
            // C must remain the bench for every trip, including trips after a stack split.
            yield return Toils_JobTransforms.SetTargetToIngredientPlaceCell(TargetIndex.C, TargetIndex.B, TargetIndex.B);
            yield return PlaceAndReserveIngredients();
            yield return Toils_Jump.JumpIfHaveTargetInQueue(TargetIndex.B, extract);

            Toil doWork = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = bench == null ? 1 : Mathf.CeilToInt(RepairUtilities.CalculateRepairDuration(pawn, bench) / MendSelfMod.Settings.speedMultiplier)
            };
            doWork.WithProgressBarToilDelay(TargetIndex.C);
            doWork.FailOnCannotTouch(TargetIndex.C, PathEndMode.InteractionCell);
            doWork.handlingFacing = true;
            doWork.tickAction = () => pawn.rotationTracker.FaceTarget(bench);
            yield return doWork;

            // 独立 toil：只有 doWork 自然完成后才执行，中断则跳过
            yield return new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Instant,
                initAction = () =>
                {
                    Thing item = itemToRepair;
                    if (item == null || item.Destroyed || job.placedThings.NullOrEmpty() || !PlacedIngredientsValid())
                    {
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }
                    // A placed stack can include materials that were already beside the bench.
                    // Consume only the amount this job actually delivered.
                    foreach (ThingCountClass ingredient in job.placedThings)
                        ingredient.thing.SplitOff(ingredient.Count).Destroy();
                    job.placedThings.Clear();
                    RepairUtilities.RepairItem(item);
                }
            };
        }

        private Toil PlaceAndReserveIngredients()
        {
            Toil toil = ToilMaker.MakeToil("SMY_PlaceAndReserveIngredients");
            toil.initAction = () =>
            {
                IntVec3 cell = job.GetTarget(TargetIndex.B).Cell;
                bool reserved = true;
                if (!cell.IsValid || pawn.carryTracker.CarriedThing == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                bool dropped = pawn.carryTracker.TryDropCarriedThing(cell, ThingPlaceMode.Direct, out _, (thing, count) =>
                {
                    HaulAIUtility.UpdateJobWithPlacedThings(job, thing, count);
                    // The callback receives the actual destination stack, even after merging.
                    if (!pawn.Reserve(thing, job, 1, -1, null, false))
                        reserved = false;
                    pawn.Map.physicalInteractionReservationManager.Reserve(pawn, job, thing);
                });
                if (!dropped || !reserved || pawn.carryTracker.CarriedThing != null)
                    EndJobWith(JobCondition.Incompletable);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private bool PlacedIngredientsValid()
        {
            if (job.placedThings == null)
                return true;
            foreach (ThingCountClass ingredient in job.placedThings)
                if (ingredient.thing == null || !ingredient.thing.Spawned || ingredient.thing.Map != pawn.Map
                    || ingredient.thing.IsForbidden(pawn) || ingredient.Count <= 0 || ingredient.thing.stackCount < ingredient.Count)
                    return false;
            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ingredientTrackingVersion, "ingredientTrackingVersion", 0);
        }
    }
}
