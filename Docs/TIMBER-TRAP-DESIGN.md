# Timber trap intervention

Status: complete and verified, ready for publication. Baseline main `fb4d5bcd7`, 2026-10-03. Original Caves of Ooo content connection; no Qud source-parity claim. [Executed prompts](TIMBER-TRAP-PROMPTS.md).

## 1. Player choice and scope

Spend **one carried SalvagedTimber and one ordinary action** to permanently jam one visible, adjacent mechanical trap. Its exact owner stays on the ground, visibly wedged, passable and harmless. No XP, recovered timber, loot, skill roll, rearm or reset. Other traps, including another on the same tile, remain live until separately dealt with.

Support exactly **SpikeTrap, BearTrap, FireTrap and PressurePlate**, using their exact native trigger types and an explicit TrapJammingPart on newly instantiated blueprints. FireTrap is pressure-actuated machinery: wood blocks its mechanism, not an emitted flame. Exclude grouped TripWire, magical runes, biological snares and arbitrary subclasses. Ordinary unjammed damage/effects, faction filtering and trigger consumption stay intact.

The ordinary loop already exists: the western glade cellar's finite pallet dismantles into two timber; the trapper's store one surface chunk south has a trapped direct approach and an open bypass. Spending one timber makes that exact direct trap safe but leaves insufficient stock for the two-timber garden wicket until another length is found. Walking around costs no material; confronting threats and preserving the original trap remain alternatives. The action also applies to admitted versions of the same four traps in lairs/ruins and other regions. No new quest, reward cache or global population roll.

### Candidate decision

| Candidate | Decision |
|---|---|
| Timber trap jamming | Selected: connects existing finite material, unsafe routes, inventory, saves and art with one new physical action. |
| Curation inspection annex | Next separate milestone; needs actual two-compartment geometry and containment acceptance. Current 3×2 cage cannot supply that depth through another flag or hand-in. |
| More crop/enemy entries | Defer: connect current 35 cultivated species and existing encounters before expanding catalogues. |
| Rearm/recover/set player traps | Defer: would introduce a new trap economy and ownership/placement contract. Permanent single-target jamming is independently useful. |

## 2. Pre-implementation verification sweep

| Assumption | Current source | Correction |
|---|---|---|
| Only three traps exist; TripWire is deferred. | TriggerOnStepPart includes PressurePlate and coordinated TripWire. Old TRAP-FURNITURE.md predates them. | Four explicit single-mechanism targets; exclude group wires. |
| Every trigger is single-use. | PressurePlate sets ConsumeOnTrigger=false; Spike/Bear/Fire remove their owner. | Jam preserves the current owner and suppresses triggering. Unjammed plate remains repeatable; consumed traps still disappear, with no invented debris. |
| The world action dispatcher automatically wraps transactions. | InputHandler's general fallback fires a raw event. Resource-action branch uses PerformInventoryActionCommand and charges only success. | Route JamTrap into that paid transactional branch. Part/service itself does not charge turns. |
| Saved traps acquire a new blueprint Part on load. | Save Parts are rebuilt from the saved graph. | Explicit opt-in: old cached traps without TrapJammingPart stay literal. Newly instantiated supported blueprints have the action. No save migration or generation manifest bump. |
| Timber is abundant ambient salvage near spawn. | RepairTimberPile occurs at 2.6; Sealbark is Grovelands; Morrowfast mender is distant. | Dependable opening supply is the finite two-timber cellar pallet. Preserve scarcity and the wicket tradeoff. |
| A new trap showcase must be placed. | SpreadExplorationPlan assigns TrappersStore at 11.11; existing 64/1729 source tests confirm placement. Exact hostile/cache/geometry receipts may refuse other seeds. | Reuse current source; no all-seed guarantee or extra free material. |
| Updating blueprint prose teaches this use. | MaterialUseDescription overrides timber Examine; worksite builder overrides trap prose. | Update timber guidance and append dynamic current trap state, so both existing overrides stay useful. |
| A color change makes jam obvious. | Existing original trap meshes ignore trigger state. | Add conspicuous tan timber bracing to current jammed model variants. Retain armed models and ordinary firing/removal. |

Inspected authority: TriggerOnStepPart and four concrete mechanisms, RepairablePart, PerformInventoryActionCommand, InventoryTransaction/InventoryTransferSnapshot, InputHandler resource branch, WorldInteractionSystem/WorldAffordanceQuery, ExaminablePart/MaterialUseDescription, saved public Part fields, SpreadExplorationPlan/Worksites, GleanersCellarBuilder/pallet, SpreadScenerySource/Recipes/3DLibrary and existing art builder/native expedition driver. Historical trap bleeding issue is resolved and not part of this milestone.

## 3. Mechanics contract

`TrapJammingPart` is a sealed Part with Name `TrapJamming`, public saved bool `Jammed`, constants JamCommand `JamTrap` and TimberBlueprint `SalvagedTimber`. `IsSupported(owner)` requires its exact current single Part plus exactly one of the four exact mechanical trigger classes and a matching supported blueprint. `IsJammed(owner)` is true only for a current supported jammed owner. Public `CanOfferJam(actor,zone)` is read-only, `DescribeJamming()` provides factual current state, and `TryJam(actor,zone)` enters the ordinary command without independently charging time.

The menu offers `jam mechanism (1 salvaged timber)` on an unjammed current supported trap. It may show the requirement without current stock; a no-material attempt is a free specific refusal. Require a living actionable Player, actual current inventory, same current zone/cache, actual grounded actor/target anchors, reach at most one cell, currently visible/explored cell and visible RenderPart. Reject carried/equipped/removed/replaced/stale/duplicate/hidden/distant/foreign/unsupported/dead targets, malformed Parts, and action-blocked actors. No querying hidden contents or firing arbitrary action callbacks during hints.

Payment accepts one exact actually carried SalvagedTimber unit. Refuse ground/container/equipped sources, broken backlinks, duplicate references/IDs, nonpositive stacks and body/equipment aliases. Claim actor, trap and selected material in the existing transaction. Snapshot exact inventory mutation, consume one real unit, revalidate the current owner/context after callbacks, then set Jammed with undo. Before-veto, failure/exception or outer rollback restores original timber, jam state and all unrelated transfers correctly. No replacement unit or owner is manufactured. Once the command owns its claims, nested/reentrant attempts cannot debit or apply twice. This does not promise isolation against arbitrary recursive BeforeInventoryAction producers; see review limits below.

The trigger checks current authoritative jam state before damage/status/consumption. Suppression applies only to that owner. Preserve the original faction gate, rune/wire behavior and every unjammed target's current trigger/removal semantics. Jammed pressure plates remain inert for repeated movers. Do not remove the trigger Part or globally change ConsumeOnTrigger. Keep callback-order limits explicit: a synchronous callback manually invoking unrelated movement inside an open inventory transaction is not ordinary turn scheduling and must not be advertised as general transactional isolation.

After committed payment/state, publish the ordinary Interact gesture, owner-cell dirty mark, factual message and a furniture TrapJammed receipt. Refusals record their reason without success feedback. Observe actual current owners when emitting presentation; no stale view or substitute trap. Repeated action is a free refusal with no further consumption.

## 4. Readability, art and persistence

Timber Examine lists its one-unit trap use alongside two-unit repairs. Dynamic trap readout distinguishes exposed mechanism, needed material and visibly wedged harmless state. Existing trapper prose still explains the bypass. Nearby/focused current hints point to the normal menu, with no quest arrow, global marker or hidden-trap detection. Wrong/legacy/noncurrent targets receive no fictitious action.

Use the existing original scenery library and cuboid/palette source. Jammed spike teeth are held down, bear jaws are blocked by a board, fire actuator is visibly braced and plate edge is wedged. Tan physical crosspieces distinguish the state from dark steel at game scale. Preserve old armed model IDs and bounds; route only the exact current opted-in state to jammed variants. Ordinary single-use triggers still disappear after firing; no new persistent spent-state system. Existing player Interact is enough; no new character rig.

Full replacement save tests must preserve the same logical trap ID with a new Entity reference, Jammed=true, unchanged remaining timber and safe future movement. Cached return does not reset or re-enable it. Legacy missing-Part owners keep their original action/trigger behavior. Existing manifests, source counts, loot and caller RNG are untouched.

Readiness: 🟢 actual timber and trap sources, transactions, ordinary menus, saved bools and original model pipeline; 🟢 jamming/paid input/current-state art and native route; 🧪 unaided discoverability, economy and all-seed usefulness need playtesting.

## 5. Implementation gates and review

1. Native RED for real content opt-in, transaction debit/state/trigger suppression, legacy and rune controls, readouts and current-state models. Tests use reflection until missing API exists; no production before root observes RED.
2. Core exact authority/payment/rollback and trigger gate; paid InputHandler wiring. Four factory blueprints surgically add one Part each; parsed diff proves other blueprints/fields unchanged.
3. Dynamic readout/hints, timber prose and distinct original jammed model states; import through existing reviewed art builder with new evidence destination.
4. Paired positives/counters and adversarial callbacks/ownership/outer rollback/replacement saves. Affected trap/rune, repair/material, input/hint, generation and rendering regressions; avoid a whole-art sweep unrelated to changed APIs.
5. Bounded isolated ordinary native journey: actual Duelist, cellar pallet harvest2, ordinary travel one south, exact current trap world-menu jam, one paid action/one timber, safe crossing, native F5/unsaved change/F6. No grants, AI suppression, stat edits or clock manipulation. Preserve refusal rather than repeatedly expanding a driver; controlled fixtures and ordinary evidence stay separate.
6. Independent Q1 symmetry/Q2 consistency/Q3 branch controls/Q4 docs versus implementation. Fix significant findings, inspect actual pixels, update this doc and prompts, commit with CLAUDE§2.3, fetch/rebase and push main.

## 6. Evidence, implementation log and self-review

Native RED job `6965f3ddee9a405fa1bd1f7e8cc8695e` completed 106 cases before production; the raw Unity XML reports 100 failed and 6 passing controls, zero skipped. MCP failure list is capped 25; exact totals come from the retained XML, not inference. [Receipt](Verification/TimberTraps/native-red.json), [XML](Verification/TimberTraps/native-red.xml). Production began after this observation.

Surgical blueprint proof: 666 before/after; exactly SpikeTrap, BearTrap, FireTrap and PressurePlate add one TrapJamming Part. Every existing field/part and other blueprint is unchanged. Examinable is already inherited from PhysicalObject and was not duplicated. [Parsed comparison](Verification/TimberTraps/blueprint-diff.json).

### Review fixes and counter-checks

- 🟡 Ambiguous saved IDs could pay: five hypotheses failed in native Unity. Reject missing/duplicated actor/target/material identities; the full-zone identity scan runs only on execution.
- 🟡 A moved/detached/replaced owner could animate after a committed callback: three native hypotheses failed. Capture the paid owner/anchor and emit the gesture only if that same supported visible owner/Part remains current. The committed payment receipt stays factual.
- 🟡 The actual generated store retained an unconditional “springs” warning after jamming. Independent content and presentation reviews found it. A generated-source test failed before changing the cue to “While armed”. Keep the bypass clue.
- 🔵 The menu test initially expected a removed target to enter the inventory transaction. Source review shows the existing reach gate correctly refuses it earlier. Corrected the expectation; both scheduler and material remain unchanged. Production reach behavior was retained.
- ⚪ No current effect reparents a TrapJammingPart while its AllowAction callback evaluates. Such custom reparenting remains outside this milestone's guarantees.
- ⚪ No current BeforeInventoryAction producer recursively invokes JamTrap before claims begin. General callback recursion/isolation is an existing transaction-framework concern; this milestone covers actual consumers and claimed/After callbacks, not arbitrary extension code.
- 🧪 Eight native mesh/prefab variants were imported and the offline Blender comparison was visually inspected. The ordinary journey also passed; root viewed the real menu, reader, jammed spike-trap and restored-state screenshots. Human unaided discovery and long-term balance remain playtest questions.

The paired review run completed 122 cases: 121 passed; only the deliberately new generated-cue assertion failed. Earlier review RED completed 121 with 8 core failures and 1 removed-target expectation failure. Receipts and XML are retained beside the initial RED. The separately observed menu-wiring RED ran 6 cases before adding JamTrap to the paid resource branch.

Final post-GREEN Q1–Q4 review found no significant unresolved issue. Q1–Q4 source review compares repair transaction ordering, consistent four-mechanism contracts, positive/refusal/save/legacy controls and docs against actual Parts, triggers, hints and models. This is CoO-original gameplay, not a Qud port; there is no parity claim. New models use the existing Spread presentation scope. Mechanical jamming/readout can work on admitted traps elsewhere without claiming new 3D coverage there.

### Implementation map

- TrapJammingPart: current-owner eligibility, one real material payment, reversible saved state, diagnostics and committed gesture.
- TriggerOnStepPart: shared jam-state gate ahead of damage/effects/consumption.
- InputHandler: native paid resource-action dispatch.
- Objects.json: exactly four opt-in Parts, no existing fields changed.
- ExaminablePart, MaterialUseDescription, WorldAffordanceQuery, SpreadExplorationWorksites: factual discovery and conditional hazard prose.
- SpreadScenery source/recipes/library/editor builder and ArtSource: 8 original braced states, borrowed palette, preserved 56 earlier model definitions and assets.
- Six TrapJamming test classes: core/adversarial, actual source, rendering, launcher and actual selected world-menu payment.
- SpreadDiscoveryNativePlayer.TrapJamming and launcher wiring: isolated bounded ordinary-key journey with no material/stat/AI grants.

### Performance

No new Update loop, cache, shader or renderer. Hints inspect only the current owner/actor Parts and status fields without arbitrary callbacks. Material/zone identity scans and transaction snapshots run only on actual action execution. Changed owner marks its cell dirty; meshes reuse the approved palette. No new profile was run, so no frame-rate improvement is claimed.

### Final verification

- **641/641 selected native Unity EditMode tests passed**, zero failures/skips, job `7de88088f60b44e2a2672dd0744dd410`. Includes **122 new cases** (22 core, 53 adversarial, 14 content, 25 presentation, 6 actual-menu input, 2 launcher) and 519 affected existing trap/rune, repair/material, world-action/hint, scenery and worksite checks. [Raw result](Verification/TimberTraps/native-green.json), [full XML](Verification/TimberTraps/native-green.xml). This is a targeted native regression sweep, not a full-project run or the standalone reference runner.
- **Ordinary native journey:11/11 checks, complete=true, 0 failures, 0 unexpected errors.** First declared seed 64 Duelist attempt completed without retry or grants.123 local inputs plus 1 map step yielded 124 completed player turns; normal map travel additionally advanced 10 world-clock ticks. It finished at 35/40 HP with 1 timber, both starting tonics still present, the original pallet gone and the two-timber wicket unrepaired. The live player fought real threats on the route.
- Native F5, a real unsaved step, F6 and another crossing proved new object references retain the same logical IDs, jammed state and remaining material. Both actual crossings left HP unchanged. [Full journey report](Verification/SpreadDiscoveryExpeditions/Native/77d7e740bb62465eaaad6b32ce22b1c6/report.json).
- Exact original trap 9051 changed from `spread-scenery-spiketrap-0` to `spread-scenery-spiketrap-jammed-0`; current mesh submission/bounds were recorded independently of screenshots. Root viewed the reader, real menu, jammed trap and restored crossing images: broad tan bracing is visible against dark ground/steel. The restored screenshot has the player standing over the same trap, so it does not expose every mesh detail. The offline gallery covers all four mechanisms; this ordinary journey visually covers the spike trap only.
- A read-only [cleanup probe](Verification/TimberTraps/native-cleanup.json) confirmed Play stopped, isolation inactive with exit code 0, save-root override null and SampleScene restored. Its reviewed save-isolation launcher uses a disposable save root and restores previous preferences; no user save was loaded or overwritten by this journey.

**Can verify:** the named scripted checks, exact native material and turn payment, safe owner-specific movement, actual save replacement and observed screenshots on the declared route.

**Cannot verify:** unaided discoverability, difficulty/economy across seeds/builds, campaign replayability, every biome's art, or every possible custom callback. No claim that this single intervention supplies all remaining depth.

### Implementation example

```csharp
// The original trigger and its saved owner stay intact.
if (TrapJammingPart.IsJammed(ParentEntity))
    return true;
// Otherwise use the original trigger/faction/consumption path.
```

The same authoritative current-owner check selects the physical braced model. Inventory payment is reversible until the shared transaction commits; only successful native menu dispatch advances one player action.

### Where to try it

On a fresh glade world, descend the western glade cellar stairs and dismantle the finite timber pallet. Return aboveground, travel one surface chunk south to the trapper's store and examine its exposed spike trap from beside it. Choose **jam mechanism (1 salvaged timber)**. The free bypass remains available. Existing saved traps without the added Part do not gain this action automatically.

## 7. Scope divergences and next step

Deliberate limits: no migration for existing saved traps without the opt-in; no wood recovery, rearm, fuel simulation or generic disarming; 3D state coverage inherits the existing Spread renderer. Custom callback isolation is bounded as recorded above. Next independent content design: the Curation inspection annex, with real geometry and existing owner continuity rather than another abstract certification.
