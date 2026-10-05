# Combat Extended compatibility review

Date: 2026-10-05. SMY 0.1.2, commit 61fb044.

Workshop item: https://steamcommunity.com/sharedfiles/filedetails/?id=2890901044
Package ID: CETeam.CombatExtended
Combat Extended version: 16.7.3.0 (312624729d-rel)
CombatExtended.dll SHA-256: 3102BC2276C583E51FE85AE340E5B986F80452171DB63EA72D04F7651B96AFE3
Simple Mending dependency SHA-256: 54210D9BE6E7D0D77C3B09A5A6112F51EBEDFED619ABE56D5253A39D0DEF030A

## Inspection

SMY uses Simple Mending's item eligibility, repair costs and repair operation. Its sidearm adapters extend equipment selection for Simple Sidearms and Simpler Sidearms. Combat Extended uses the standard equipment path.

Reviewed all XML patches shipped with the installed Simple Mending 1.6 dependency. Dedicated material costs cover Royalty weapons and packs, VFE Empire deserter armor and helmets, and Rimsenal Spacer smart weapons. SMY inherits those definitions through the shared repair utilities.

## Game test

An isolated quicktest loaded Harmony, Core, Combat Extended, Simple Mending and the SMY runtime with a disposable probe. A damaged equipped steel knife, a stored autopistol, medicine, a hand repair bench and steel in a stockpile exercised candidate selection and a real repair job through the WorkGiver and JobDriver.

All 12 checks passed: equipped and stored weapon selection, medicine filtering, candidate deduplication, durability thresholds, bench item and ingredient filters, work admission, correct job target, completed repair, retained equipment and inventory items, and exact consumption of 15 steel. The log confirmed Combat Extended initialization. This fixture exercised peaceful-map repair; ammunition use, loadout changes and combat were outside its test scope.

The result log's `Sidearms active=True` line is a reused probe scenario flag. The enabled-mod configuration above identifies the actual loaded mods. The diagnostic `JobFailReason` reflects intentional rejection checks before successful job creation.

Evidence: [game results](../validation/2026-10-05-combat-extended/results.txt).
