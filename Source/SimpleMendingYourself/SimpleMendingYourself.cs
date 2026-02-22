using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace SimpleMendingYourself
{
    [DefOf]
    public static class MendSelfDefOf
    {
        public static JobDef SMY_MendSelf;
    }

    public class MendSelfSettings : ModSettings
    {
        public float mendThresholdUpper = 0.8f;
        public float mendThresholdLower = 0.2f;
        public float minLaborSpeed = 1.0f;
        public float speedMultiplier = 0.5f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref mendThresholdUpper, "mendThresholdUpper", 0.8f);
            Scribe_Values.Look(ref mendThresholdLower, "mendThresholdLower", 0.2f);
            Scribe_Values.Look(ref minLaborSpeed, "minLaborSpeed", 1.0f);
            Scribe_Values.Look(ref speedMultiplier, "speedMultiplier", 0.5f);
            base.ExposeData();
        }
    }

    public class MendSelfMod : Mod
    {
        internal static MendSelfSettings Settings;

        public MendSelfMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<MendSelfSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("SMY_UpperThreshold".Translate(Settings.mendThresholdUpper.ToStringPercent("F0")));
            Settings.mendThresholdUpper = listing.Slider(Settings.mendThresholdUpper, 0.01f, 1f);

            listing.Label("SMY_LowerThreshold".Translate(Settings.mendThresholdLower.ToStringPercent("F0")));
            Settings.mendThresholdLower = listing.Slider(Settings.mendThresholdLower, 0f, 0.99f);

            listing.Label("SMY_MinLaborSpeed".Translate(Settings.minLaborSpeed.ToStringPercent("F0")));
            Settings.minLaborSpeed = listing.Slider(Settings.minLaborSpeed, 0f, 2f);

            listing.Label("SMY_SpeedMultiplier".Translate(Settings.speedMultiplier.ToStringPercent("F0")));
            Settings.speedMultiplier = listing.Slider(Settings.speedMultiplier, 0.1f, 2f);

            listing.End();
        }

        public override string SettingsCategory() => "SMY_ModName".Translate();
    }

    // 存储每个修补台允许自我修补的pawn（空集=全部允许）
    public class CompProperties_SelfMendPawnFilter : CompProperties
    {
        public CompProperties_SelfMendPawnFilter()
        {
            compClass = typeof(CompSelfMendPawnFilter);
        }
    }

    public class CompSelfMendPawnFilter : ThingComp
    {
        private HashSet<int> disallowedPawnIds = new HashSet<int>();

        public bool IsPawnAllowed(Pawn pawn) => !disallowedPawnIds.Contains(pawn.thingIDNumber);

        public void SetPawnAllowed(Pawn pawn, bool allowed)
        {
            if (allowed)
                disallowedPawnIds.Remove(pawn.thingIDNumber);
            else
                disallowedPawnIds.Add(pawn.thingIDNumber);
        }

        public void AllowAll() => disallowedPawnIds.Clear();

        public void DisallowAll(List<Pawn> pawns)
        {
            foreach (Pawn p in pawns)
                disallowedPawnIds.Add(p.thingIDNumber);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            var list = new List<int>(disallowedPawnIds);
            Scribe_Collections.Look(ref list, "disallowedPawnIds", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                disallowedPawnIds = list != null ? new HashSet<int>(list) : new HashSet<int>();
        }
    }
}
