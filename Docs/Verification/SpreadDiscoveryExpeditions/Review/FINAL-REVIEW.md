# Spread discovery and expeditions — independent final cold-eye review

Review baseline: `6015ba8b9c31bfe52658f8ec8b0bcf6f98218a14`. Reviewed current M1 reports/notes and hooks, M4 source capture/relocation and manager wiring, Wayhouse selection/staging/final acceptance/retention, and narrow save binding. The reviewer did not operate Unity, edit shared Assets, stage, or commit. Root executed and published the named native gates. This is CoO-original content; no Qud parity or foreign lore/assets claim applies.

## Outcome

One meaningful fixed-goods validation gap was reproduced and repaired. No further concrete production blocker was found in the bounded authority, exact-owner, and save review. This does not establish every generated world, whole-campaign economy/balance, visual quality, or overall integration completion. Those remain separate named acceptance gates.

## Finding — fixed goods could change between creation and installation (resolved)

`SpreadWayhouseBuilder.Packet`, around lines187–193, originally checked reward/key blueprints and graph ownership without checking actual item quantity, the promised Buckler commerce value, or the key's NoTrade tag. An actual `ObjectCreated` callback could change these properties while all ownership references remained valid. A final `OnZoneGenerated` callback could do the same after staging. Both paths then accepted and could save the changed goods. This violated the scoped contract of one value20 Buckler and one reusable nontradeable key; it was not an observed mutation in the shipped ordinary blueprints.

The repair adds only refusal predicates to the existing Packet method: actual key/reward quantity1, key NoTrade, and an owned reward CommercePart with Value20. Both existing pre-placement and final-generation validation call that method. It does not normalize or repair products, change ordinary rolls, alter foreign containers/graphs, introduce saved fields, or revalidate mutable player inventory on later saves. Existing foreign ownership, source preservation and untouched/depleted save controls remain.

Eight paired witnesses mutate the same four fields at creation and final-generation boundaries. They retain unchanged-positive controls and verify refusal preserves the ordinary graph or rejects the unsafe final graph before caching/installation metadata. The count helper mutates the actual first Stacker (adding one only if absent) and asserts the exposed gameplay quantity is2.

Evidence classification is explicit:

- Initial private13 had10 controls passing and3 failures; one failure used a false duplicate-Stacker premise, while the value and NoTrade failures were genuine.
- Expanded original native39 had31 controls passing and8 failures, of which6 were genuine and2 were the duplicate-Stacker premise. `Tests/wayhouse-goods-cold-eye-red.xml` is retained unchanged. The first post-guard native53 passed51 and failed those2 malformed witnesses; that receipt is not called GREEN.
- Corrected private39 against the exact pre-guard builder passed31 and failed8 genuine cases (`goods-corrected-before.xml`). Corrected current private39 passed39 (`goods-corrected-after.xml`). Actual runtime/native-test reference compilations both returned0 errors.
- Root's corrected native39 likewise passed31 and failed8, job `67b0f628415f4b26bfd2dfc7625a9e8a`, `Tests/wayhouse-goods-corrected-red.xml`.
- Restored reviewed guard then passed native53/53: Wayhouse50 plus economy3, job `5a0d27b78e814f2abc426a27adddb7eb`, `Tests/wayhouse-final-goods-green.xml`.

The exact pre-guard production SHA is `443ed2299ef2a8156a8522b1675220d14e6ff1f08c6834a4904800c01083cd60`; guarded production is `8a061a7d2a257ff44706e7cf2360db81c8d91ecdd9da74e960051295a230a4cb`. Corrected test SHA is `b399b8e0ec71b656012f2864ac247d994475192718b889f019c624118e58aba4`. A separate peer reread of the minimal guard found no concrete blocker.

## Q1 — Symmetry

M1 offer creation and note writing use the same current manager/map/plan/actor/part/cell/conversation authority; both conversation end and choice refresh discard ephemeral offers. Reading saved notes deliberately has different semantics: it validates bounded historical wire data and never reconsults a moved/dead informant or remote destination. This is required historical behavior, not missing write validation (`SpreadDiscoveryReports:42–85,143–196`; `SpreadDiscoveryNotes:24–78`).

M4 claims receipts only after a complete geometry plan, then moves the same owners and checks their complete captured state before reporting commitment. The rollback moves only still-owned exact references currently at their staged destinations. Receipt capture invalidates at each new source build and never runs during load/attach. Source capture-on/off and paired full-manager controls retain ordinary stock/count/gear/RNG projections; the later composer alone changes allowed coordinates (`SpreadWildernessSituationBuilder:33–112`).

Wayhouse provisional staging, final acceptance and saved installation are separate. The original staged plan survives a replacement of current selection, and final acceptance validates it before caching. Save binds only the finally accepted exact cached graph; load binds that serialized graph without recreating goods. Missing legacy metadata remains disabled. The null-manager save path remains guarded (`SpreadWayhousePlan:17–31,68–99`; `OverworldZoneManager:1114–1125`; `SaveSystem:345–349,419–428`).

## Q2 — Cross-feature consistency

All three report families share one bounded historical wire and current-offer protocol. Tokens are ephemeral UI revisions, not saved gameplay randomness. Current map/selection changes prevent new writes; existing historical notes remain readable. Only fixed family keys are read, so unknown prefixed properties neither expand the journal bound nor suppress valid families.

Cargo/shelter composition consumes existing owners, performs no factory/random calls and adds no content. Wayhouse deliberately has a different economy contract: one random container/group is replaced by a fixed reward, key-only sack, and one normal guard/loadout. Its three-seed economy fixture reports the actual stock/gear delta and optional refusals; it does not infer player income or claim neutrality. Both late builders exclude current rare/Wayhouse authority and preserve cached/restored state.

## Q3 — Counter-checks and proof limits

Reviewed existing adversarial coverage includes same-ID replacement player, removed/dead/hostile speaker, foreign part backlinks, stale cache/map/plan/node, earlier callback invalidation, refreshed/unoffered tokens, malformed notes, unknown families, and historical save/load with the original informant absent. No remote GetZone/stock/actor lookup exists in report building or note reading.

Receipt/composer coverage includes stale/rebuilt sources, duplicate/empty/carried root owners, produced-blueprint substitution, stock/quantity changes, current Wayhouse replacement, consumed source reuse, unrelated physical-only blockers/creatures, hazard and absent-bypass refusals, finite search budget, and actual full-manager caller RNG/count/stock/coordinate projection. Wayhouse adds changed final packet, foreign takeover, exact key/door ownership, route blockage, untouched/depleted save and refused unload/revisit. The new goods witnesses probe the previously missing factory/final boundary rather than repeating these existing controls.

Native source/UI and journey reports are not interchangeable. The reviewer independently executed only the isolated focused goods corpus, inspected the authoritative native XML totals, and read the current production/tests. Final integrated tests, actual scene cards, route viability and perceived performance require their own retained runs/views. No broad additional test suite was invented merely to inflate coverage.

## Q4 — Documentation and scope

The current master plan describes three historical reports, existing finite grain, 4300 late composition after haulables, actual source replacement instead of early suppression, and explicit generated-value economy deltas. These match the reviewed source. The main plan remains root-owned and was not overwritten by this review.

Ordinary wilderness cache/regeneration policy remains separate from the one accepted Wayhouse graph's narrow retention. The inert anchor does not reconstruct rewards, live owners or cleared status. A selected address does not promise successful installation or surviving enemies. Controlled callback corruption at final acceptance rejects the whole unsafe uncached graph; it is distinct from an ordinary optional footprint refusal.

The paused/capped journey and visual acceptance limits must remain explicit in closeout. Paired cold generation is not natural acquisition. Timing a late builder at4301 excludes later final acceptance validation; whole GetZone measurements include that cost. Submitted meshes/viewport bounds do not prove unobscured pixels; viewed frames remain required.
