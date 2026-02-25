using System.Collections.Generic;
using ComfyCuddlesWithEuterpe;
using Verse;

namespace SimpleMendingYourself
{
    public class MapComponent_RepairBenchCache : MapComponent
    {
        private HashSet<Building> cachedRepairBenches = new HashSet<Building>();
        private bool initialized = false;

        public MapComponent_RepairBenchCache(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                initialized = false;
            }
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (!initialized)
            {
                RebuildCache();
                initialized = true;
            }
        }

        public IEnumerable<Building> GetRepairBenches()
        {
            if (!initialized)
            {
                RebuildCache();
                initialized = true;
            }
            return cachedRepairBenches;
        }

        public void Register(Building bench)
        {
            if (bench != null)
            {
                cachedRepairBenches.Add(bench);
            }
        }

        public void Unregister(Building bench)
        {
            if (bench != null)
            {
                cachedRepairBenches.Remove(bench);
            }
        }

        private void RebuildCache()
        {
            cachedRepairBenches.Clear();
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                if (thing is Building building && thing.TryGetComp<CompRepairAssignment>() != null)
                {
                    cachedRepairBenches.Add(building);
                }
            }
        }
    }

    public class CompProperties_RepairBenchCacheNotifier : CompProperties
    {
        public CompProperties_RepairBenchCacheNotifier()
        {
            compClass = typeof(CompRepairBenchCacheNotifier);
        }
    }

    public class CompRepairBenchCacheNotifier : ThingComp
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (parent is Building building && parent.Map != null && parent.TryGetComp<CompRepairAssignment>() != null)
            {
                MapComponent_RepairBenchCache cache = parent.Map.GetComponent<MapComponent_RepairBenchCache>();
                cache?.Register(building);
            }
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            if (parent is Building building && map != null)
            {
                MapComponent_RepairBenchCache cache = map.GetComponent<MapComponent_RepairBenchCache>();
                cache?.Unregister(building);
            }
        }
    }
}
