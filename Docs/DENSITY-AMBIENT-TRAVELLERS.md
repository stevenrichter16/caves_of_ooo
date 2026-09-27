# Ambient life and travelling encounters — C7

Status: Sari, contextual remarks and bounded travellers are implemented. The bounded traveller/contextual-remark native journey passed **13/13** on 27 September 2026, with zero errors and exact editor restoration. Combined standalone core/adversarial verification is122/122 GREEN; Sari previously passed root native acceptance. The native route and its limits are recorded below. CoO-original authoring, no Qud parity claim.

## Sources and non-negotiable boundaries

Read `DENSITY-COMPLETION-PLAN.md` C7; `BREADTH-PASS.md` B4 and its canon/semantics; `FELLING-WORLD-DESIGN.md` §§2.3, 3.1, 3.4–3.5 and 6; `Lore/10_Bible.md` §§IV–V; `Lore/11_SecondSpine.md` C1/C3; `Lore/Codex/12_SariStory_Sill.md`; `Lore/MYSTERY-LEDGER.md` entries 6–8; the Gin Frog passage in `sarisarinama_bestiary_design.md`; and shipped `SillHearsIt` content and ending code.

Tier-one farmland remains quiet except the existing one-shot Sill storylet. Sari ambience begins at tier three. Vessel and Gathered stop it, Practice changes its pitch, Kept leaves it unchanged (the existing Breadth plan explicitly labels Kept's interpretation as CoO-original). No narrator resolves the two readings of sari, gives Urqu intention, ranks endings or explains the Gin Frogs. Gin Frogs and the Glassblown Remnant must not acquire ambient speech. No unseen or hostile actor is advertised by a cosmetic remark.

## Verification sweep and corrections

| Premise | Verified source/API | Decision |
|---|---|---|
| B4 provides numerical chance and spacing constants | It specifies tier 3+, increasing chance, spacing and tier-5 arrival, but no exact percentages or interval | Proposed initial tuning: 2%, 5%, 10% at tiers 3/4/5; a shared twenty-player-turn ambient gap. Clearly authoring, not a canon number. |
| Surface tier or dungeon loot tier can be reused | `WorldMapAuthoring.TierAt` is canon's authored surface table; depth encounter tier uses a different formula | Ambient tier = authored tier plus one below ground, capped at five, as B4 specifies. |
| Any `Overworld.*` string is a valid location | `IsOverworldZoneID` only checks the prefix; `FromZoneID` can return negative coordinates and permits out-of-bounds coordinates/depth | Validate parsed world coordinates and nonnegative depth before ambience. World-map and malformed zones remain quiet. |
| Tier lookup has no allocation | `FromZoneID` calls Split, and ZoneID is publicly mutable | Cache parsed zone context weakly per Zone and rebuild if ZoneID changes. Avoid repeated parsing/string-key construction on quiet turns. |
| World ticks equal player actions | `TurnManager.EndTurn` runs for NPCs and blocked actions; TickCount advances with speed and explicit rests | Hook after the existing SeventhPosition player cleanup and count only valid player EndTurn calls. Persist cosmetic turn/spacing state on the player. |
| Ending state requires a global singleton | `EndingSpine.Enacted(player)` reads the saved player's integer property, with explicit path constants 1–4 | Use that authoritative actor state; don't infer endings from optional global narrative context. |
| Sill still needs implementation | `SillHearsIt.json` is one-shot, triggers in `Overworld.10.10.0`, and sets `sill_sari_heard` | Leave its text, trigger and fact unchanged. Ambient code must never duplicate or set that fact. |
| Zone attachment means the player entered | `OnZoneAttached` runs on generation, access and restore, including read-only retrieval | Do not roll travellers or emit arrival messages from attachment. Require actual player membership and an explicit entry seam or first valid player turn. |
| Every generic entity may speak | Mystery Ledger preserves constitutional Remnant silence; Gin Frogs have no advertisement calls and must remain unexplained | Start remarks with an explicit allowlist of ordinary speaking roles; no blanket Creature/Faction speech. |

## C7a — Sari, bounded and persisted

Use a small coordinator beside `SeventhPositionPart.OnPlayerTurnEnded`, with a sari service and a shared ambient-message budget suitable for the following remarks milestone. A valid actor is a live Player physically present in the supplied Zone; null, absent, foreign-zone, dead and non-player calls do nothing. The cosmetic sequence uses a fixed stable hash of canonical zone ID and persisted player-action count, never Brain, generation, loadout or combat RNG.

At tiers three/four/five, periodic chances are initially 2/5/10 percent. A first tier-five arrival is guaranteed once per distinct canonical zone, persisted on the player to avoid repeated edge crossings or save/load replaying it. An explicit arrival hook should emit immediately after successful transfer. If integration initially uses the first completed player turn, that timing divergence must be recorded and cannot be called immediate arrival. Shared ambient spacing applies to periodic lines; a first tier-five arrival may bypass spacing once, reflecting the authored guarantee. Subsequent crossings do not reset spacing.

Proposed minimal narration is sensory: a soft `sari... sari...`, the same syllables clear at higher tiers, and a changed pitch after Practice. It never explains a source, assigns intent, names a hidden actor or chooses between cosmological readings. Only actual log emission writes `event/SariHeard` with zone, tier, variant and reason. State persists through ordinary entity integer/string properties; no save-version migration or global registry is needed.

TDD: missing-service/hook RED; tier 1/2 silence versus tier 3+, authored-depth mapping, exact spacing boundaries, deterministic replay and increasing hit count across a bounded sequence, first tier-five arrival and revisit/save controls, all four endings, malformed/null/foreign/dead/NPC controls, no Sill fact mutation, no gameplay RNG use, and actual TurnManager integration. Dedicated adversarial checks include alias zone IDs, mutation of ZoneID, overflow and property corruption, absent saved keys, actor identity, diagnostic counts and full save graph restoration.

## C7b — Contextual remarks

Use the same saved message budget, allowing at most one ambient line in a turn and suppressing a remark when sari emitted. Restrict candidates to a small authored set of ordinary speaking roles, actual living visible bodies, neutral/friendly disposition in both directions, and a bounded local radius. Read actual contextual evidence (seated state, adjacent existing fire/well, time band) rather than inventing activities or services. Cosmetic choice remains hash-based. Contextual text receives independent voice/canon review before acceptance. No default-on per-candidate diagnostic spam.

Tests must pair visible/invisible, friendly/hostile, present/removed, speaking/silent, context-present/absent and one-candidate/multiple-candidate states. Probe footprints, walls, duplicate candidates, death and repeated reload. Native acceptance checks the real player-visible log and silence gates without revealing hidden actors.

## C7c — Entry travellers

First verify the actual entry lifecycle and saved state owner with the C6/zone-manager owner. A generation/access callback is not an entry event. Use one persisted roll per eligible surface zone and a small bounded global/route roster with stable encounter/entity IDs. An exhausted or dead encounter stays exhausted across revisit and save/load; ordinary trade consumes real persistent inventory. Legal placement must respect occupied cells, footprint eligibility, reserved approaches and arrival routes. Failure to place must have an explicit retry policy that cannot create duplicate stock.

Origin/destination leads must refer to real authored places reachable on the current world map; no invented settlements, offscreen simulation claim or implicit caravan economy. Reuse ordinary conversation/trade/withdrawal routes. Separate tests for roll-once, placement refusal, revisit, death, save restoration, missing origin/destination and consumed stock precede the entry integration. Native acceptance crosses a real edge, talks/trades, crosses back, and verifies no duplicated actor or refreshed inventory.

## Self-review and remaining acceptance

- Resolved integration: root wires the actual InputHandler zone transition before autosave; generation-time access is not used as player entry.
- 🔵 Authoring constants and one-arrival-per-canonical-zone policy above are explicit implementation choices for restraint and replay safety.
- 🧪 Native log timing, subtlety, visibility and readability remain player-observable acceptance. Private core tests cannot establish soundscape feel or travel density.
- ⚪ No existing saves receive new NPC kits or regenerated maps; cosmetic ambient state starts absent and is written only by new valid player actions.

Implementation and evidence for all three milestones are recorded below. The final native section closes one real traveller and Scribe-context route; it does not establish average encounter density or every voice context.

## C7a implementation and verification

`WorldAmbience.OnPlayerTurnEnded` is wired immediately after SeventhPosition cleanup.
`SariAmbience.OnZoneEntered(Entity, Zone)` is the stable immediate-entry API for the
root-owned InputHandler transition hook. First-turn detection is retained as a
fallback for initial/bootstrap entry and old saves. Positive HP, Player tag,
non-death-handled status, exact SpatialZone and actual anchor membership are
required. Authored chance is 2/5/10 percent and the shared gap is twenty completed
player actions. A first tier-five arrival bypasses that gap once per canonical
zone; legacy ID aliases share the same saved key.

State is saved in namespaced player integer properties. Quiet-turn cosmetic
bookkeeping deliberately writes these implementation keys without dispatching
`IntPropertyChanged`, so it does not trigger gameplay fact reactors. Parsed zone
context is weakly cached and invalidated on ZoneID mutation. The sample is a fixed
integer hash of canonical zone and player action count; it consumes no RNG stream.
No Sill storylet or ending text was edited. Emission records `event/SariHeard` with
zone, tier, variant and reason. New lines describe only an audible sound and the
Practice pitch change.

RED: missing API compile errors before production. A test-only RemoveTag typo was
corrected before accepting that receipt. Initial production compile used the wrong
static bounds API; corrected to WorldMapAuthoring.InBounds. Core GREEN: 26/26.
Dedicated adversarial run: 45 combined, 43 pass and two useful failures. A negative
saved last-message timestamp permanently muted periodic ambience; the next valid
emission now repairs that malformed timestamp. A throwing log observer interrupted
ambient emission after the line was already stored; the receipt/memory now remain
committed and a separate observer-failure diagnostic is recorded. This protects the
ambient emission seam, not every unrelated MessageLog call in the scheduler.
Final core plus dedicated adversarial GREEN: **45/45**. Evidence is in
`Docs/Verification/DensityCompletion/Ambience` (`sari-red.log.gz`,
`sari-adversarial-red.xml.gz`, `sari-green.xml.gz`).

Coverage includes actual scheduler invocation, all four endings, tier/depth and
invalid-zone controls, exact 19/20 action spacing with a real passing hash control,
increasing bounded-sequence frequency, immediate arrival/reentry/alias persistence,
save graph replay, independent players, global Sill fact preservation, zero gameplay
RNG/fact-event use, counter wrap, diagnostics, null/dead/absent actors and malformed
saved state. The standalone runner does not prove native log display, timing,
line feel or the root-owned transition hook. No native call was made by this agent.

Files: NEW SariAmbience.cs, WorldAmbience.cs and metadata; MOD TurnManager.cs (one
hook); NEW DensitySariAmbienceTests.cs (26) and DensitySariAdversarialTests.cs (19)
and metadata; this living doc and receipts. C7b/C7c implementation follows below.

Parent-native update (2026-09-26): root reports141/141 focused native GREEN, comprising45 Sari,4 scheduler,59 local-people and33 world/UI integration cases. It includes the transition-before-autosave path and reflected transition counter-checks. This agent made no Unity calls. Native automated assertions do not establish the subjective subtlety or readability of ambient lines.

## C7b/c verified bounded authoring decisions (2026-09-26)

Remarks use four explicitly speaking roles (Scribe, Innkeeper, Farmer, Warden),
a five-cell physical radius, actual visible occupied cells plus line of sight,
living actor and player membership, and non-hostility in both directions.
Scribe remarks require real seated furniture; innkeepers require a nearby campfire;
farmers require an actual well; wardens may remark on actual surface darkness.
Lines only describe that immediate context. A five-percent isolated hash gate is
checked before the bounded local-cell scan; the existing20-action shared budget
wins, and a Sari emission suppresses a remark in the same action. Silent creatures
are absent from the allowlist, including GinFrog and GlassblownDrifter. Neither RNG
nor gameplay facts are consumed by cosmetic sampling.

Traveller encounters use the actual successful-entry seam, not generation/access.
They are capped at3 per player's saved world journey, with a persisted one-time
roll per canonical eligible surface wilderness zone and deterministic entity IDs.
The authored chance is one in eight. First valid entry consumes the zone's roll,
including misses, blocked placement, or missing route data; no retry on reentry.
Only a manager-owned cached zone and two distinct actual village POIs can supply
an encounter. Placement searches bounded nearby empty, passable, unreserved cells,
away from edge/arrival tiles and hazardous tile state. No path, reservation, liquid,
creature or structure is removed to make space.

A traveller is an ordinary persistent merchant entity with real inventory,
conversation and trade. Route text reads the actual origin/destination POI names;
missing or stale route data gives the existing authored dialogue. No offscreen
movement/economy is simulated. The merchant retains ordinary timed restock (the existing300-turn system), stamped at creation so entry does not double-roll opening stock. Crossing an edge never recreates stock, purse or actor. This is a cap on new encounters, not a promise of permanently finite merchant supplies. Entry API:
`WorldTravellers.OnZoneEntered(Entity, Zone)`. Root owns its InputHandler hook and
the read-only conversation-text decorator; this agent owns the service and tests.


## C7b/c implementation, independent review and remaining gates

`WorldRemarks` adds four lines only: a seated scribe's hands, an innkeeper's real
nearby fire, a farmer's real well, and a warden's actual outdoor darkness. Ordinary
one-cell players scan at most121 cells. An extended player body scans its expanded
bounds once, capped by the finite zone grid; physical distance and sight still
filter candidates. The fixed hash does not use gameplay RNG or fact events.

`WorldTravellers` admits only a live player in the active manager-owned surface
wilderness zone, authored tiers1–3. It commits the one-in-eight zone roll before
placement, factory or route refusal. Four rings at radius4–7 inspect at most176
candidate cells; occupied, reserved, edge, interior, liquid, hazardous, stair and
narrow approach cells are refused. Three encounters per saved player/world seed
is the hard cap. Deterministic IDs and player receipts survive the normal save
graph; the merchant's actual stock, purse and route stay on that entity. Factory
loadout/trader globals are temporarily scoped to private encounter RNG and restored
in a finally block. Ordinary timed merchant restock remains deliberate.

The route decorator requires the current player's saved encounter receipt and
unchanged actor identity, live local participants, and two distinct canonical
village endpoints still present on the actual map. It uses ordinary Merchant_1
Start/Sources conversation and existing trade routes. It never invents settlement
names or an offscreen travelling economy. Root owns successful-entry integration
before actor registration/autosave and the conversation-text composition.

TDD evidence: travellers initially failed compilation only for the missing API;
core21/21 passed. The18-case independent follow-up produced39 total with four
useful failures: copied route properties, another player's dialogue, a changed
encounter identity, and two aliases of the same village could falsely claim a
route. Binding the saved receipt and canonical endpoints resolves all four.
Remarks core28/28 passed; ten independent cases then exposed a dead chair still
qualifying a stale SittingEffect and a physically nearby extended player body
being excluded by its distant save anchor. Dead furniture now refuses, and the
search/sight use physical bodies. One test-only RemovePart generic-call typo was
corrected before the semantic RED run. The final combined Sari45 +remarks38
+travellers39 is **122/122 GREEN** in the standalone runner. Receipts are in
`Verification/DensityCompletion/Ambience`, including the semantic RED XMLs and
`c7-core-adversarial-green.xml.gz`.

Full-world graph controls preserve emptied stock,17 remaining drams, actor identity,
route text and no repeated encounter after load. Other controls cover dead/foreign
actors, hostility, missing villages, negative/capped save counters, existing zero
roll flags, deterministic-ID collision, walls, reserved/interior/hazard/liquid
placement, death/removal, many-zone cap, RNG preservation, actual restock timestamp,
physical visibility, silent creatures, shared spacing and throwing log observers.
No Unity calls were made by this agent. At this core checkpoint, native entry/trade/reentry and actual remark visibility remained root gates; the later native section records their bounded completion. Broad voice feel and density are still not established by these tests.


## C7 integration and cold-eye review

The actual InputHandler successful-transition method now calls travellers after
SetActiveZone and before collecting the new zone's creatures, so the existing
scheduler/Brain/render registration and autosave see the new persistent actor.
ConversationManager.CurrentText composes the traveller route outside LocalPeople;
shared authored nodes remain unchanged. Four integration checks first produced
three intended failures (absent entry seam and undecorated Start/Sources text)
plus one ordinary-merchant control. All four now pass. The transition ordering
assertion is explicitly a source-contract test; it does not execute InputHandler.
The two text tests do execute the production ConversationManager property.
Published-source C7 core/adversarial is122/122, and integrated standalone verification
is **126/126 GREEN**. Three additional native reflection tests are published for
root's next run: actual transition registers one merchant without another turn,
repeated transition preserves depleted stock/purse, cached read-only access does
not roll, and dead-player entry stays quiet (the first test covers two controls).
They have not been executed by this agent.

Q1: entry and revisit use one persisted receipt; actual successful transfer owns
creation, whereas GetZone/restore remain read-only. Shared remark/Sari budget is
written on emission and retained on observer failure. Q2: all services require
living present players; voices require living visible/non-hostile local speakers;
traveller dialogue additionally checks the saved encounter ownership. Q3: positive
context/placement/conversation cases have same-roll refusals for absent, dead,
hostile, stale, blocked and copied records; save graph retains concrete stock and
identity. At that checkpoint the native input seam remained a separate unexecuted gate; the later native journey records actual input evidence. Q4: documented
restock is ordinary timed restock, not permanent finite stock; creation cap is3,
chance1/8, tiers1–3; no simulated caravans, no silent-creature voice, no Sill edits.
Screenshots, conversational feel, subtlety and travel density are not established
by these standalone receipts.


Parent native follow-up: the integrated259-case run had258 pass and one test-only
C7 refusal. The read-only cache test incorrectly demanded an entirely empty zone,
although existing authored-site upgrade hooks can add unrelated native owners on
GetZone. It now checks the intended guarantee: no traveller-marked actor before or
after access and no traveller roll memory. The actual transition positive control
continues to require a registered persistent merchant and depleted stock/purse on
reentry. Root's next native run must confirm this correction.

Independent line review by the LocalPeople reviewer found no concrete canon or
mystery concern. The four lines are practical role/context observations, with no
unseen threat or invented service promise. Traveller text is supported by actual
village POIs and the real merchant trade path. No prose change was requested.

Published-source combined C7+C14/world regression verification is **248/248 GREEN**
(126 C7 plus122 glade/world). Native acceptance remains separately reported above.


## Native acceptance preparation (2026-09-27)

The verified source sweep and bounded keyboard route are recorded in [NativeAcceptance/PLAN.md](Verification/DensityCompletion/Ambience/NativeAcceptance/PLAN.md), before driver implementation. The planned route uses an ordinary new player, a real edge-created Merchant, actual conversation/trade, edge revisit, F5 / real stock mutation / F6, and one actual generated contextual speaker. Labelled approach travel is disclosed; no actors, money, stock, effects or ambient counters are granted or reset. Existing core refusals remain separate from one native journey.

Stock is subject to the existing restock policy: entry after **more than300 scheduler ticks**, with shelf refill below the **three-item** low-water threshold. The planned short revisit checks a not-yet-due stock snapshot; it does not assert permanently finite merchant stock. Traveller creation remains capped and receipt-backed. The shared20-action remark budget, silent characters and exact existing lines are unchanged.

The initial private test-first launcher cleanup extension named `DensityTravellerNativeBatch` before it existed. Root subsequently published the reviewed driver/launcher after its gate. This preparation changed no gameplay mechanics and made no editor calls; the actual first journey and remaining acceptance are recorded below.


The private driver/launcher is now written, with zero errors in full current-runtime, editor-reference and test-reference compilation. Independent review found no concrete source-flow blocker. Before native execution, source inspection corrected one premise: ordinary Item descendants inherit Stacker. The route therefore uses an actual instance/whole stack with no compatible destination merge target, then checks exact combined IDs/counts and stack-aware payment. At that private checkpoint, the actual native journey and image review were pending; root later ran the journey below. No gameplay mechanic was changed.


Native C7 first run `1a4e51d9fab649ee942efefca624c37f` passed the first10 actual checks (entry/scheduler, dialogue/trade, revisit and real F5-sale-F6 graph restoration), then stopped after16.903s because the harness demanded an already-live context from freshly generated inactive residents. Root reviewed the actual talk/trade/restored images and preserved exact cleanup. This is partial acceptance. The [updated source sweep](Verification/DensityCompletion/Ambience/NativeAcceptance/PLAN.md) traces genuine Scribe seating and Farmer well visits to ordinary scheduled turns. The subsequent reviewed harness-only repair activated an actual usable-seating village and observed native waits/movement, without forcing NPC goals, time, probability or a remark. No gameplay change follows from this false precondition.


## Completed bounded native journey (2026-09-27)

Root ran `5cd67d81cd2c4a899f5abab2310a8996`: **13/13 required checks passed**, zero failures/unexpected errors, **37.8453338 seconds**. The [full report](Verification/DensityCompletion/Ambience/NativeAcceptance/5cd67d81cd2c4a899f5abab2310a8996/report.json), actual editor log byte range and compressed segment are preserved with [exact before/after restoration](Verification/DensityCompletion/Ambience/NativeAcceptance/5cd67d81cd2c4a899f5abab2310a8996/restoration.json). This includes the original editor scene/start scene, seed, save root, last-game preference, input setting, background flag and stopped Play state.

The actual edge into `Overworld.16.5.0` created and scheduled `traveller:64:Overworld.16.5.0`. Native conversation named the real Drowned Ledger and Sumphold endpoints. Native purchase moved the same actual item `31275` for9 drams (player50→41, merchant500→509). The real edge revisit preserved consumed stock before timed restock was due. F5, a native sale of that actual item for1 dram, and F6 then restored distinct player/merchant graphs with the exact saved stock, ownership, purses, route receipts, clock and energy. No stock, currency, skills or actors were granted.

After the labelled player-only transfer activated ordinary scheduling in `Overworld.16.1.0`, the driver paid **83 real routine actions**. A genuinely seated nearby Scribe `33855` emitted **“A moment for my hands.”** at ambient action95 / scheduler tick950. The selected routine candidate was Scribe `33811`; acceptance correctly joined the actual emitting owner33855 to its own current seating, visibility, eligibility, fresh diagnostic and synchronous log entry. It did not substitute the selected candidate's context or force either speaker to sit. The next **19 paid actions** produced no additional ambient remark/Sari line, preserving the shared20-action budget. No NPC goals, seating reservations, time, hash outcome or ambient counters were edited.

Root [visually reviewed](Verification/DensityCompletion/Ambience/NativeAcceptance/5cd67d81cd2c4a899f5abab2310a8996/root-visual-review.json) `06-actual-contextual-remark.png`: the exact line is legible beside actual residents, with the ordinary40/40HP and41-drams HUD visible. `08-ordinary-finish.png` was mostly black and is **not accepted as scene/HUD visibility evidence**. The synchronous records and earlier context frame establish the bounded remark, not general visual quality. The other seven captures in this successful run were not all independently reviewed. This foreign-zone route is not Spread style acceptance.

Q1: all13 real required gates passed, including the formerly missing context and spacing gates; the original10-check partial run remains preserved. Q2: the correction addressed inactive-source preselection in the audit, with no gameplay change. Q3: exact entry owner/stock/payment, real post-save sale, replacement graphs and fresh actual-speaker/context evidence prevent a count-only or stale-graph pass; broad silent/hostile/removed/context-absent controls remain in the existing core/native tests. Q4: this is one seeded route with disclosed player approach shortcuts and a finite routine wait, not naturally walked whole-world discovery, all four role contexts, subjective ambient density or permanently finite merchant inventory. Ordinary restock remains **more than300 scheduler ticks** and stock threshold **below3**. Native acceptance does not imply offscreen caravan simulation.
