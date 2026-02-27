using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace SimpleMendingYourself
{
    internal static class SimpleSidearmsCompat
    {
        private static bool? _active;
        private static MethodInfo _getMemoryComp;
        private static FieldInfo _rememberedWeapons;
        private static FieldInfo _pairThing;
        private static FieldInfo _pairStuff;

        public static bool Active
        {
            get
            {
                if (!_active.HasValue) Init();
                return _active.Value;
            }
        }

        private static void Init()
        {
            Type memType = GenTypes.GetTypeInAnyAssembly("SimpleSidearms.rimworld.CompSidearmMemory");
            if (memType == null) { _active = false; return; }

            _getMemoryComp = memType.GetMethod("GetMemoryCompForPawn", BindingFlags.Public | BindingFlags.Static);
            _rememberedWeapons = memType.GetField("rememberedWeapons", BindingFlags.Public | BindingFlags.Instance);

            if (_rememberedWeapons != null)
            {
                Type pairType = _rememberedWeapons.FieldType.GetGenericArguments()[0];
                _pairThing = pairType.GetField("thing", BindingFlags.Public | BindingFlags.Instance);
                _pairStuff = pairType.GetField("stuff", BindingFlags.Public | BindingFlags.Instance);
            }

            _active = _getMemoryComp != null && _rememberedWeapons != null && _pairThing != null;
            if (_active.Value)
                Log.Message("[SimpleMendingYourself] SimpleSidearms compatibility enabled.");
        }

        // 返回pawn所有已注册副武器（装备栏 + 背包均过滤rememberedWeapons，排除临时武器）
        public static IEnumerable<ThingWithComps> GetRegisteredWeapons(Pawn pawn)
        {
            if (!Active) yield break;

            object memComp = _getMemoryComp.Invoke(null, new object[] { pawn, true });
            if (memComp == null) yield break;

            IList remembered = _rememberedWeapons.GetValue(memComp) as IList;
            if (remembered == null || remembered.Count == 0) yield break;

            var rememberedPairs = new HashSet<(ThingDef, ThingDef)>();
            foreach (object pair in remembered)
            {
                ThingDef thing = (ThingDef)_pairThing.GetValue(pair);
                ThingDef stuff = (ThingDef)_pairStuff.GetValue(pair);
                rememberedPairs.Add((thing, stuff));
            }

            if (pawn.equipment != null)
                foreach (ThingWithComps weapon in pawn.equipment.AllEquipmentListForReading)
                    if (rememberedPairs.Contains((weapon.def, weapon.Stuff)))
                        yield return weapon;

            if (pawn.inventory != null)
                foreach (Thing item in pawn.inventory.innerContainer)
                    if (item is ThingWithComps twc
                        && (twc.def.IsRangedWeapon || twc.def.IsMeleeWeapon)
                        && twc.GetComp<CompEquippable>() != null
                        && rememberedPairs.Contains((twc.def, twc.Stuff)))
                        yield return twc;
        }
    }
}
