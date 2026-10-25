# Nella composition: independent seed64 Play review

2026-10-25 UTC. Bounded evidence review by the independent review agent; no Unity control, runtime edits, save edits or cleanup. Root performed ordinary input in the real isolated GameBootstrap session. This report accepts the seed64 composition and recorded lifecycle below, not the whole opening release.

## Evidence and visual decision

Reviewed paired PNG/JSON files `nella-composition-64-final-{entry,reserve-harvest,replant,rain,gleaned,save-return,accepted-frontage}` in this directory. All seven PNGs are 1280×800, and all seven readouts report seed64, `Overworld.11.8.0` and flat presentation. The final entry and original `nella-composition-64-before-entry` both place the player at17,24; their ticks differ, so this is a composition comparison rather than identical elapsed gameplay.

Candidate2 materially improves the bounded field hierarchy. The primary has coherent existing furrows around an open service bay and becomes the largest field. Per-parcel counts change from33/26/11/33/40 in the original to38/13/9/17/28. The final105 rows retain substantial remote pieces; rejected candidate1's59 rows left remote counts7/3/10/12. Original PNG brown pixels in the fixed world region change37,060→27,807, versus15,963 in candidate1. This corroborates the visual judgment; a count alone would not establish quality. Approved sprites and the quiet central walking area remain recognizable. No new significant visual defect was found.

Repeated image previews can suppress unchanged regions. Original PNG pixel inspection and paired entity data were used to avoid mistaking that preview behavior for a blank game view.

## Proven crop, inventory and rights lifecycle

| Capture | Tick / player | Proven state |
| --- | --- | --- |
| final-entry | 2500 /17,24 | Player2699 has32HP and50drams. Keeper9130 is at18,9 with Unknown rights; claim coordinates are16,6 and17,6, tray20,7. The original ten resident owners are present. Pitchpod9147 at17,6 is stage2; Marlroot9146 at16,6 is stage0. Both are dry. |
| final-reserve-harvest | 2690 /17,7 | Rights are Granted and drams42, consistent with the recorded8-dram payment. Original Pitchpod9147 is absent. Inventory gains exactly PitchpodResin9150×2 and PitchpodSeed9152×1; all pre-existing carried ID/blueprint/count rows and equipped items remain unchanged. Visible log says Nella opens the tied row/tray and reports3 packed,0left. |
| final-replant | 2700 /17,7 | Seed9152×1 leaves inventory, with no other carried change. New PitchpodCrop9153 is at the same17,6 bed, stage0, recorded growth units0, moisture0. Original9147 does not return. Rights remain Granted and resin9150 remains×2. |
| final-rain | 2710 /17,7 | Inventory and equipment are unchanged. The visible log confirms planting and rain. Both reserve crops and all four public crops record stage0, units1, moisture39. |
| final-gleaned | 3210 /51,18 | Inventory gains exactly Emberwheat9161×1; resin9150×2 is retained. Player is now level2 in the screenshot,42HP,42drams. The log identifies the defeated enemy as a **marlback scrabbler**, then reports1grain packed,0left on the cut row. The row owner8653 remains at51,19. |
| final-save-return | 3280 /51,24 | Screenshot includes Game saved/Game loaded/Autosaved messages. Keeper9130, claim coordinates and Granted rights remain. Inventory ID/blueprint/count rows and equipment exactly match the gleaned capture. |
| final-accepted-frontage | 3770 /17,9 | Player is back beside keeper9130;42HP/42drams, Granted rights, same inventory and equipment. Row8653 explicitly records `harvested:true`. Reserve crop9153 remains at17,6 and Marlroot9146 at16,6; neither was replaced on return. |

The JSON confirms the player's8-dram decrease and rights transition; it does not serialize the keeper's currency balance. No independent claim about the NPC credit is made here.

## Growth and persistence

By tick3210, Marlroot9146 records stage1/units14/moisture0 and replanted Pitchpod9153 stage1/units16/moisture0. Public emberwheat9143/9144 each record stage1/units5/moisture0. These exact stage/unit/moisture triples remain at ticks3280 and3770 while their `lastTick` advances: the watered plants grew and then paused dry during the observed travel/return. This does not claim that a reserve plant reached maturity or completed another harvest cycle.

Public gladroot owners9141/9142 disappear after watering and elapsed growth, consistent with the root-observed automatic harvest; produce is visible at their former public beds in the final frontage capture. The filtered JSON does not enumerate those dropped produce owners, so their exact IDs/yields are not independently established here. Do not assert that all ten original resident owners must remain after time passes. The newly planted Pitchpod9153 is an ordinary planted crop without the original stamp role; it is correctly present in `objects`, not `residentOwners`.

Canonical sorted `(id, blueprint, x, y)` tuples for **all105 field rows match exactly across all seven captures**. The same ripe-row blueprint ID8653 survives harvesting as a depleted owner; its final `harvested:true` distinguishes depletion from respawning. The actual log and inventory prove one harvest yielding one sheaf. A second actual harvest attempt was not captured; repeated-harvest rejection belongs to the separate native test evidence.

Keeper9130, ordinary sign9145, reserve sign9149 and tray9148 keep their IDs/positions throughout. The original reserve Pitchpod9147 and consumed seed9152 do not reappear. Twelve final inventory rows, including resin9150×2 and sheaf9161×1, are unchanged across gleaned→save-return→accepted-frontage. Equipment identities/slots remain LongSword2703 and Buckler2704 throughout; the sampled sword readouts also retain10/10HP, no on-hit effect and no temper. These are the serialized fields compared, not a claim of a byte-identical whole save or of soil-owner IDs, which this readout does not expose.

The retained earned checkpoint exists at `/tmp/coo-fresh-opening-after-nella-composition/20cb263ebed24a8380168954e23981ca/Quick.sav.gz`,203,249bytes. Independently recomputed SHA256: `1513031329358a9ac409a9337740c1a5209971615e5ddc14cd5e2b4d0da035b8`.

## Limits and decision

Accept this seed64 field hierarchy and the demonstrated ordinary grant→gather→replant→rain→glean→save/load/travel/return lifecycle. No actionable significant discrepancy was found in the reviewed evidence. Combat caused real intervening progression: the screenshots show a scrabbler kill,15XP and level2; these seven samples alone do not prove the precise minimum HP or number of melee attacks between captures.

Seed65 remains a separate acceptance gate. This review does not establish physical Steam Deck input/performance, a new packaged build, frame-time improvement, full crop maturation, every claim/denial branch, exact public-produce yields or whole-world graphics completion. Source/native test results are separate from these actual Play captures.
