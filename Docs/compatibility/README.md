# Simple Mending Yourself 1.6 Compatibility List

[简体中文](README.zh-CN.md)

Updated: 2026-10-05 · Simple Mending Yourself 0.1.2

This file lists the base requirements, dedicated adapters and reviewed mod combinations. Each entry describes the supported behavior, relevant settings and validation coverage. Please share the mod names and a log when reporting a combination that needs attention.

## Base game and requirements

**RimWorld 1.6 — Compatible**

Pawns with Basics enabled can repair their worn apparel and equipped weapons. Repair jobs use the chosen bench, collect and reserve materials, and restore durability after the work finishes. The configured HP range, bench filters and allowed-pawn list determine eligibility.

Equipment from official DLC and other mods follows the same repair-target and material definitions supplied by Simple Mending. Eligible apparel and weapons enter the same repair workflow.

**[Simple Mending](https://steamcommunity.com/sharedfiles/filedetails/?id=3657705987) — Required integration**

Uses Simple Mending's hand and electric repair benches, item and ingredient filters, repair-cost calculation and final repair operation. SMY adds personal equipment selection, an allowed-pawn tab and a repair-speed multiplier. Materials in storage contribute to Simple Mending's resource availability check.

Validation: game tests with SMY 0.1.2 cover work admission, filtering, completed repairs and exact material consumption. See the [Simpler Sidearms validation](../development/SimplerSidearmsCompatibility.md) and [Rules of Engagement validation](../development/RulesOfEngagementCompatibility.md).

**[Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) — Compatible**

The tested sidearm and combat combinations load Harmony for their own patches. SMY adds its work type entry and bench controls through standard game definitions and components.

## Sidearms and combat

**[Simple Sidearms](https://steamcommunity.com/sharedfiles/filedetails/?id=927155256) — Integration**

Reads the pawn's registered weapon list and includes matching weapons in both equipment and inventory. Weapon definition and material identify each registered type. Bench item and ingredient filters, the allowed-pawn list and HP thresholds apply to these repair jobs.

Validation: source review of the dedicated registered-weapon adapter.

**[Simpler Sidearms](https://steamcommunity.com/sharedfiles/filedetails/?id=3809933830) — Integration, SMY 0.1.2+**

Includes equipped weapons and eligible melee or ranged weapons carried in inventory. Temporarily carried weapons also qualify when they meet the equip, bench and HP conditions. Repairs preserve the item's current holder and consume the materials delivered for the job.

Validation: 12 game checks with Simpler Sidearms and five baseline checks, covering candidate selection, filters, completed inventory-weapon repair, equipment retention and material consumption. [Validation record](../development/SimplerSidearmsCompatibility.md).

**[Rules of Engagement](https://steamcommunity.com/sharedfiles/filedetails/?id=3809985752) — Compatible for equipped weapons**

Supports repairs of main-hand and equipped off-hand weapons. For a sidearm stored in inventory, equip it through RoE's weapon controls to add it to the repair candidates. RoE continues to manage loadouts, stances and weapon switching.

A repair job keeps its chosen item reference when RoE restores a previous weapon at job start. The selected item can finish repairing after that restoration moves it into inventory. Bench filters, HP thresholds and material costs continue to apply.

Validation: 20 game checks with RoE, including completed off-hand repair, retained dual-wield identity, tool restoration during job start and exact material consumption. [Validation record](../development/RulesOfEngagementCompatibility.md).

## Choosing a combination

Use Simple Mending and SMY as the repair foundation. Choose the weapon-management system for the colony and follow its own combination guidance. The entries above describe SMY's support for each system; the linked validation records name the configurations used in testing.

For equipment mods, use the repair bench's item and ingredient filters to select the desired gear and materials. Simple Mending's repair definitions provide the cost and eligibility rules; SMY's settings provide the personal durability range and speed multiplier.

## Keeping this list current

The English and Chinese files in this directory are maintained together. Each reviewed mod entry carries a direct Workshop link, a concrete support description and its validation record. Future compatibility updates are published at these same GitHub file URLs.

