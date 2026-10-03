# Simpler Sidearms compatibility validation

2026-10-03, SMY 0.1.2. Simple Mending dependency SHA-256: 54210D9BE6E7D0D77C3B09A5A6112F51EBEDFED619ABE56D5253A39D0DEF030A. Simpler Sidearms package: zzz.simplersidearms; DLL SHA-256: 2C26E505F9C102F53814807235EA9FA5674DBE9B86B16E909793FFD3DC9098BC.

An isolated RimWorld quicktest loaded Harmony, Simple Mending, SMY and Simpler Sidearms. Twelve checks passed: candidate selection, normal equipped weapon retention, non-weapon exclusion, deduplication, work admission, HP threshold, bench item/material filters, job creation, actual repair completion, equipment/inventory retention, and exact material consumption. A second profile without Simpler Sidearms passed five checks confirming original candidate behavior.

The repair ran through the real JobDriver and game ticks. Initial fixture attempts omitted a stockpile: Simple Mending's CanColonyAffordRepair requires resources counted in storage. After adding a stockpile and updating the resource counter, the repair completed. The diagnostic JobFailReason printed after successful creation is stale from the earlier intentional rejection checks.

The Simple Sidearms registered-weapon branch is unchanged and takes precedence; this check did not rerun that mod. Simpler Sidearms has no registration list, so temporarily carried eligible weapons are included. No new Harmony patches or third-party binary references were added.

Evidence: [enabled](../validation/2026-10-03-simpler-sidearms/enabled.txt), [disabled](../validation/2026-10-03-simpler-sidearms/disabled.txt).
