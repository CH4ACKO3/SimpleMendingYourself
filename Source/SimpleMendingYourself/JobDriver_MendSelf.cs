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
        private Thing Bench => job.GetTarget(TargetIndex.A).Thing;
        private Thing CostItem => job.GetTarget(TargetIndex.B).Thing;
        private Thing ItemToRepair => job.GetTarget(TargetIndex.C).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
            => pawn.Reserve(Bench, job, 1, -1, null, errorOnFailed)
            && pawn.Reserve(CostItem, job, 1, job.count, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.A, null, storageMode: true);

            Toil doWork = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = Mathf.CeilToInt(RepairUtilities.CalculateRepairDuration(pawn, Bench) / MendSelfMod.Settings.speedMultiplier)
            };
            doWork.WithProgressBarToilDelay(TargetIndex.A);
            doWork.handlingFacing = true;
            doWork.tickAction = () => pawn.rotationTracker.FaceTarget(Bench);
            yield return doWork;

            // 独立 toil：只有 doWork 自然完成后才执行，中断则跳过
            yield return new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Instant,
                initAction = () =>
                {
                    if (CostItem != null && !CostItem.Destroyed)
                        CostItem.Destroy();
                    Thing item = ItemToRepair;
                    if (item != null && !item.Destroyed)
                        RepairUtilities.RepairItem(item);
                }
            };
        }
    }
}
