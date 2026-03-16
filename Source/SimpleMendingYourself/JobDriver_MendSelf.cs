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
        private List<Thing> ingredients = new List<Thing>();
        private Thing Bench => job.GetTarget(TargetIndex.C).Thing;
        private Thing ItemToRepair => job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
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

            this.FailOn(() => bench == null || bench.Destroyed || bench.IsForbidden(pawn));
            this.FailOn(() => itemToRepair == null || itemToRepair.Destroyed);

            foreach (Toil ingredientToil in JobDriver_DoBill.CollectIngredientsToils(TargetIndex.B, TargetIndex.C, TargetIndex.C))
            {
                ingredientToil.AddFinishAction(() =>
                {
                    Thing carriedThing = ingredientToil.actor.carryTracker.CarriedThing;
                    if (carriedThing != null)
                        ingredients.Add(carriedThing);
                });
                yield return ingredientToil;
            }

            Toil doWork = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = Mathf.CeilToInt(RepairUtilities.CalculateRepairDuration(pawn, bench) / MendSelfMod.Settings.speedMultiplier)
            };
            doWork.WithProgressBarToilDelay(TargetIndex.C);
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
                    if (item == null || item.Destroyed)
                        return;
                    RepairUtilities.ConsumeIngredients(ingredients);
                    RepairUtilities.RepairItem(item);
                }
            };
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref ingredients, "ingredients", LookMode.Reference);
        }
    }
}
