# Rules of Engagement compatibility validation

Date: 2026-10-05. SMY 0.1.2, commit 55836a0.

Workshop item: https://steamcommunity.com/workshop/filedetails/?id=3809985752
Package ID: asunib.RulesOfEngagement
AsuRimArmoury.dll SHA-256: 079FBA03ED218A6ED74840581746B3836C32CB99F95747B1B370D8AF1DBED9B6
Simple Mending dependency SHA-256: 54210D9BE6E7D0D77C3B09A5A6112F51EBEDFED619ABE56D5253A39D0DEF030A

## Inspection

Reviewed the downloaded DLL through decompilation. RoE stores main-hand and equipped off-hand items in the pawn's equipment container. Stowed items use the inventory container. SMY's standard equipped-item enumeration covers the equipped main and off hand. The Simple Sidearms and Simpler Sidearms adapters are selected through their own types or package IDs.

RoE's Patch_StartJob_Tools runs ToolSwitching.OnJobStart. Jobs whose work type has a tool stat can select a suitable tool, and other jobs restore the remembered previous weapon. SMY's repair driver captures the selected item reference, so a selected weapon can finish its repair after RoE moves it into inventory during that restoration.

## Game test

An isolated quicktest loaded Harmony, Core, Simple Mending, RoE and the SMY 0.1.2 runtime, plus a disposable probe. The fixture used RoE's native EquipOff to equip a steel knife, created real SMY jobs through its WorkGiver, and advanced game ticks through the actual JobDriver. A second repair exercised RoE's remembered-tool restoration during StartJob.

Passed checks cover:

- Main-hand and equipped off-hand candidate selection; the equip-first workflow for stowed sidearms.
- Candidate deduplication and ordinary medicine filtering.
- HP thresholds, bench item filters and ingredient filters.
- Completed equipped off-hand repair, retained main/off-hand identity and exact material consumption.
- Completed repair after native RoE tool restoration moved the target into inventory, with exact material consumption.

The game log review passed. The result log's diagnostic JobFailReason comes from the intentional rejection checks before a successful job creation. This fixture tested repair behavior in a peaceful map with RoE's default settings and a remembered-tool restoration. Combat, patrol and escort functionality remain described by RoE's own documentation.

Evidence: [game results](../validation/2026-10-05-rules-of-engagement/results.txt).

