# Machine prerequisite M1a: safe flask transfer

Status: M1a accepted in source and ordinary GameBootstrap play, 2026-11-16; publication in this slice. Original CoO work. No complete machine is accepted.

## Why this increment

Actual GameBootstrap travel from earned Wellmeet8.16 to BrineLens5.16 (seed224401440, F12 off, HP40, saved turn5240) found a Well at13,19 and Cistern at52,2 alongside finite brine. Wellmeet itself already has ordinary water. Neither establishes a worthwhile condenser installation. The condenser remains proposed; do not reduce existing water availability to justify it. Salt-Vault lacks a verified working-water problem. The next site-dependent investigation is a small cold-pumping opportunity around the peat works, preserving its dry alternatives.

The immediate useful prerequisite is redistribution of carried non-water liquids without spilling them on the ground. Players already collect brine, oil and hazardous liquids in reusable flasks. Water has direct vessel transfer; other liquids require a ground pour/refill and its environmental exposure. Splitting a working quantity or consolidating partly filled flasks is useful without owning any machine.

## Contract and scope

- Source and receiver are distinct, unstacked, actually carried `LiquidVesselPart` owners. The receiver is empty or contains the same registered liquid. No mixture, cleansing, conversion or collection-origin provenance is invented.
- Existing water transfer, waterskins, drinking, ordinary collection, pouring and films are unchanged. New actions handle non-water liquid only. Abstract pool units are not currency drams or simulated carried mass.
- Offer “transfer as much as fits” first and one-unit transfer second when different, before ground-pour choices. Preview liquid, exact amount and resulting source/receiver quantities. Receiver numbers enumerate eligible destinations in this menu only, not inventory rows; commands capture identities and the observed volumes/capacities, not positions. No recipient is silently substituted.
- Reject stale quantities/capacities/identity, foreign/equipped/duplicate/removed owners, malformed parts, empty/full/incompatible vessels, invalid actor/current zone or blocked actions. The action cannot create a pool, coating, contact effect or refill.
- Join the normal command transaction; claim actor and both vessels. Failure or interruption before commitment spends nothing; a downstream exception rolls back both volumes and identities. One successful operation costs one ordinary action through both existing input routes. No autonomous work, pending cargo or elapsed/wall-clock job exists.
- Products remain in the same saved physical vessels. Travel/reload uses existing fields; no schema/version change or save migration. Unsupported arbitrary batch metadata is outside this single-identity model.

Independent design review found no pacing/utility blocker; source review must confirm exact action and transaction integration. Neither this handling foundation nor its tests accepts a condenser, pump or any complete machine.

## Verification and delivery

Native RED→GREEN: brine/oil/acid transfers, partial capacity, one-unit split, unchanged water route, no ground exposure, stale commands, aliases/equipment, malformed state, before-veto/after-throw rollback, serial reuse and token-graph reload. Test a transferred liquid through ordinary pour/contact as a cross-system outcome. Target existing liquid and water action regressions plus real UI turn charging.

Actual GameBootstrap: naturally acquire two flasks, inspect both and finite source, collect, split/consolidate through inventory, cancel a preview, refuse an incompatible receiver, pour one transferred unit and observe its existing effect, move/travel/save/fresh Continue. Capture exact identities/amounts and independent gameplay review. Tests/offline fixtures are not actual play.

Performance: action/menu-only bounded inventory scans; no new per-frame/per-turn listener, world scan, cache or background simulation. No renderer change is required. Check real play responsiveness and disclose hardware limits. Publish implementation with this record, the twelve-machine dependency matrix and current continuation only after acceptance gates pass.

## Findings from ordinary play

Sill purchase (22 drams) and Wellmeet barter (goods24 for flask23) yielded the two real12-unit vessels7482/9919. Travel included ordinary sari attacks; a healing tonic recovered HP without F12. BrineLens5.16 seed64 supplies finite BrinePool11448 at40,14. Collection from dry40,13 reduced90 to78 and filled flask7482 with12; no coating was applied. BeforeCheckpoint retains that earned state at turn1740HP34.

The first menu put transfer at action19, below18 pour directions. F1 showed static material guidance but omitted the existing live contents formatter. Native reader RED reproduces both actual findings (5 failures/13 controls). The correction prioritizes transfers22/21 and uses existing ItemExamineService in the inventory decision reader; no hidden information or new simulation is introduced. Receiver-local labels replace the misleading raw pack index.

Cold pumping remains unapproved: Peat Works17.7 has a60-unit MirePool at34,8 but existing dry routes already reach salvage. Five12-unit flask collections are an alternative, not a demonstrated new access objective. Exhausted natural pools deliberately retain their coating source, so zero units does not imply dry footing. A future pump slice must establish a reviewed finite sump/resource objective and explicit depletion consequences rather than claiming the existing pool already supports it.

## Accepted evidence and limitations

- Native baseline RED:30 tests,20 expected missing-action failures/10 controls. Stack-review RED:4 failures/11 controls. Actual-reader/order RED:5 failures/13 controls. Long-name RED:3 failures/30 controls.
- Final regression `native-final258.xml`:258/258,8.4565517s. Final compact-label follow-up `native-long-label-green51.xml`:51/51,1.6932799s, including3 new long-name cases and the48 existing transfer/boundary tests. **261 distinct tests across receipts, not a261-test combined run.** Earlier193/10/208 passes are retained but not added. Excluded job2f0609b6aa384844ad4faaadc99dc1f5 returned zero tests during a missing-namespace compile overlap; corrected before accepted runs.
- Actual seed64/F12off: naturally acquired flasks7482/9919, each12 capacity; collected12 brine from source11448 at40,14 (90→78). Cancellation preserves quantities. Split11+1, consolidate0+12, split1+11, then pour1 at40,12 in Spread11.10 yields finite puddle12530 and visible briny/contact state. Direct transfer produces no pool or coating. This demonstrates an ordinary use outside machinery and an existing environmental consequence.
- Filled the empty7482 with3 water from the ordinary finite source36,9. The9919 brine11 menu offers no transfer despite9 free capacity in the water vessel. Actual reader shows water3/12. F5AfterCheckpoint:2110HP9 at37,10. Fresh GameBootstrap Continue matches exact IDs, contents, puddle, HP, position and F12off. Subsequent actualUp to37,9 then world travel11.10→10.10 preserves3water/11brine, saved2160HP9; see AfterTravelCheckpoint and after-travel-state.json.
- Combat remains ordinary. A sky-sari killed the first transfer attempt after its normal turn. A later attempt died when Shift+Comma picked up a loose waterskin before ascent. Both are excluded failed attempts, not successful acceptance. Reloading the already saved quantities and stepping off the loose item allowed normal travel. The pre-existing keyboard pickup/ascent collision is a separate follow-up; no combat balance or invulnerability change was made. The final QA save is low-health and is not a safe starting point for unrelated future errands.
- Final compact labels (`src`, `to #`) preserve the full registered liquid name and exact quantities. `transfer-menu-final.png` is actual final-source UI; the older transfer-menu capture is retained as the earlier iteration. Contents and short transfer help reuse existing formatter/reader paths; no hidden-owner description is added.
- Independent bounded source and gameplay/visual review:0 remainingP1/P2 after stack metadata, menu-local numbering, reader omission, ordering and long-name corrections. Review accepts M1a only.
- Performance: menu/action-only inventory scans, no new frame/turn listeners, background jobs or world scans. Ordinary UI/play remained responsive; native timing is not a frame-time benchmark. No long-save performance or hardware qualification claimed. Mac packaging is pending the first coherent processing milestone; existing packages remain unchanged.
- Preservation at03:11:31UTC: all84 prior dirty tracked files match the earlier snapshot; normal editor/standalone save trees match. Only native test/session preference keys differ (PT_Run and Unity session IDs/count); ordinary gameplay preferences remain unchanged. Unity stopped, root override null, scene clean. Existing Metal memoryless-depth messages and transient MCP websocket reconnects are distinguished from gameplay exceptions.

Actual screenshots and state receipts: `Docs/Verification/MachineSystems/LiquidHandling/`. No gameplay mutation, spawned supplies, invincibility or offline substitute was used to establish this loop. Missing-marker fixture load attempts are excluded and later roots contain proper earned saves.
