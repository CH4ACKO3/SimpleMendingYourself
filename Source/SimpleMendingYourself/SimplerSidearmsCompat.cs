using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SimpleMendingYourself
{
    internal static class SimplerSidearmsCompat
    {
        public static bool Active => ModsConfig.IsActive("zzz.simplersidearms");

        // Simpler Sidearms has no registered loadout; eligible inventory weapons are sidearms.
        public static IEnumerable<ThingWithComps> InventoryWeapons(Pawn pawn)
        {
            if (pawn.inventory == null) yield break;
            foreach (Thing item in pawn.inventory.innerContainer)
                if (item is ThingWithComps weapon && !weapon.Destroyed
                    && (weapon.def.IsMeleeWeapon || weapon.def.IsRangedWeapon)
                    && weapon.GetComp<CompEquippable>() != null
                    && EquipmentUtility.CanEquip(weapon, pawn, out _, false))
                    yield return weapon;
        }
    }
}
