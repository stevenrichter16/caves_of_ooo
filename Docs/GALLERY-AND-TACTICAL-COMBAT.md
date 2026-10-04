# The continuing inspection: learnable commitments

Status: implemented, native verified and reviewed, 2026-10-03. Baseline main `59a28aa07`. Working prompts are in `GALLERY-AND-TACTICAL-COMBAT-PROMPTS.md`.

## Player outcome

The inspection gallery should offer more than a prescribed gate sequence. The player can read a slow opponent's commitment, step out of its attack line, put a solid object between them, interrupt it with control, or use its recovery to strike or escape. The same behavior on the existing wilderness Shambler makes the lesson useful elsewhere. Chosen adjacent weapon skills must actually affect the selected enemy, making control and target priority dependable in a crowd.

Original Caves of Ooo mechanics/content; the Qud inspiration is discoverable systemic interaction and planning, not copied source or exact parity. No new enemy catalogue, universal stamina/stance system, quest completion flag or new damage formula.

## Verification sweep and corrected premises

| Premise | Actual source and decision |
| --- | --- |
| The gallery already has enough tactical variation | The initial wing offers one free two-timber preparation and one combined reward cabinet. Preserve containment; split supplies by exposure and create useful lateral lanes. |
| Delayed tendril attacks already exist | `KillGoal` gives the half-set ordinary immediate melee. `CombatTacticsPart` dispatches six instantaneous powers. Add a small opt-in commitment Part, not a renamed Lunge. |
| Skills respect the direction selected in play | InputHandler supplies `TargetCell`, but eight adjacent weapon actives ignore it and pick the first adjacent creature. Validate and honor the chosen physical cell; retain legacy no-cell behavior for direct callers. |
| Shank's refusal beside an ally is intentional tactics | Its preview compensates for first-adjacent targeting. With exact target selection, allow the selected hostile and leave the ally untouched. |
| Furniture hides the player | Physics-solid furniture blocks movement and the proposed physical strike, but AI line of sight uses different solidity rules. The slab intercepts the tendril; it does not conceal the player. |
| A warning can be a short particle alone | A paused turn game needs a persistent readable state until the player acts. Use a visible actor tell, inspect/focus state and visible threatened-cell readout; no markers through fog. |
| A stun should just postpone the same attack | Successful stun/freeze/paralysis/sleep breaks the commitment. Refused effects do not. Cancellation and recovery must not allow a second action or an old strike after thawing. |
| Killing leaves a visible preserved body | Half-set CorpseChance is zero. Keep destruction as removal; do not promise a retained corpse or a cure. |
| A new reward service is needed | Existing living Ivrin is required at the ink desk; an escaped hostile can threaten that real service. No invented morality score or magical containment reward. |

Read: `CLAUDE.md`; current annex and receiving living docs; canonical root Curation/Bloom lore; `BrainPart`, `KillGoal`, `CombatTacticsPart`, `CombatSystem`, line targeting, turn manager, effects/application events, Examinable and CellStatusReadout; adjacent weapon skills and the directional input contract; receiving builder, Curation art/source and native scenario.

## Selected design

### 1. Gallery iteration

Keep the 27×9 wing, all four real apertures, public filing, keys, two-timber service repair and the existing half-set's health/speed/weapon. Move the slab to relative `(16,3)` and rail to `(18,1)`, retaining open sidestep lanes above/below the slab and approach to the transfer gate. The slab is a real obstacle to a physical lash.

Keep dressing, gloves and two clay in the near recovery cabinet `(12,1)`. Move the existing two sootroot pulp and one pitchpod resin to a separate conservation case at `(18,5)`. No duplicate or renewable stock. Reuse compatible authored cabinet art with a distinct readable description; a new mesh is only warranted if the existing model cannot communicate the object. A short incursion can recover medical/repair goods while leaving the deeper botanical stock untouched.

The maintenance placard records the strange fixed-direction lash, its pause afterwards, the gallery layout and broken exit. It warns of two containment gates without giving a mandatory step-by-step solution. Optional staff advice can explain preparation and escape. All text distinguishes the deceased person from Bloom-driven movement. Partial recovery, living containment, destruction and departure remain actual physical outcomes.

### 2. Committed melee

Authored only on CurationHalfSet and Shambler, both slower than the ordinary starting player. With an unobstructed aligned hostile within two cells, the creature spends its action drawing back a tendril. It stores its origin, zone and direction. Its next eligible scheduled action strikes along that same ray, stopping at the first physical obstruction/creature rather than tracking a sidestep. The ordinary selected weapon, hit/penetration/armor/status pipeline resolves one strike. A missed or blocked strike still costs the action.

After resolution it spends one action recovering, with a query-derived two-point DV reduction during that opening. No permanent stat adjustment or repeated penalty stacking. An interrupt, displacement, zone/hostility change or loss of the original live target cancels the pending strike safely. Control consumes the opening instead of resurrecting a stale attack afterwards. Actors without this Part keep their existing behavior. Do not attach it to the player or fast enemies without a separate timing review.

Warnings, current state and threatened cells respect actual visibility. They describe direction/reach and recovery, not hidden dice or a guaranteed damage prediction. Existing actor models remain in use; persistent local cues carry the new state.

### 3. Precise weapon control

Conk, Slam, Disarm, Rend Armor, Hook and Drag, Shank, Flurry and Backstab use the exact selected adjacent physical cell. Require the current zone's owned cell, contact with the actor footprint, a living physically present creature, and no self-target. Invalid, empty, stale, remote or foreign selection refuses without choosing another enemy or spending cooldown. With no supplied target cell, retain validated legacy first-adjacent behavior. Multicell surface selection uses actual contact rather than a distant anchor. Keep each skill's weapon gate, damage/effect, displacement and cooldown rules intact.

Shank AI uses the same exact-target rule, so a friendly bystander does not prevent acting on a different adjacent hostile. Other authored power selection and chance rules remain unchanged.

## Other event archetypes: brainstorm, not this implementation

| Archetype | Player decisions and persistent outcome | Existing support / remaining risk |
| --- | --- | --- |
| Collapsed freight cutting | Haul a beam to clear a shortcut, take an exposed detour, or return better equipped. Released obstruction and finite cargo remain. | Handling/hauling exists; author safe alternate geometry and validate placement. |
| Abandoned mechanical checkpoint | Spend repair timber jamming one mechanism, bypass it, or risk crossing to save materials. Disabled traps stay disabled. | TrapJamming supports four exact mechanical traps; do not promise magical/living trap control. |
| Unusable well | Spend clay/cord restoring a source, ration carried water, or return with materials. Repair and spent stock persist. | Repair recipes and wells work; no invented contamination simulation. |
| Camp beside a hunting ground | Defeat or lead a predator away, then rest/cook; otherwise move on injured. | Existing hostile-radius rest refusal ignores walls; a shut door alone must not be described as safe. |
| Defended medicine cache | Interrupt or pressure a healer, let it spend its finite medicine, or bypass it for another route. | FieldMedicine already consumes actual carried items. Verify corpse loot for the chosen actor. |
| Crossed firing lanes | Break rays with masonry, isolate an enemy around a corner, or brave the direct passage. | Existing ranged previews need clear first impact. Do not promise automatic friendly fire. |
| Scalded salvage floor | Choose a safe perimeter, spend protection, or leave useful salvage beside live steam. | Must author a real hot source; decorative SteamCloud alone is insufficient. |

These archetypes share discoverable physical clues, useful partial outcomes, multiple existing verbs and literal saved aftermath. Avoid copying identity-bound settlement services onto arbitrary stations.

## Implementation and acceptance

1. Observe RED for precise selected targeting and committed-melee lifecycle. Observe separate content RED for split finite stock, lanes and actor opt-in.
2. Implement the two core contracts and gallery content/readouts. Pair success with empty/foreign targets, absent Part, blockers, refused control and unchanged previous owners.
3. Run affected native Unity EditMode suites, including existing combat, skills, effects, turn scheduling, save/load, receiving content and rendering neighbors.
4. Exercise ordinary native gallery input: read the notice; observe a tell; step off its line; use the opening; recover both actual containers; repair/lure/contain; save and reload. Separately verify actual directional skill input in a crowd and combat outcomes. Any controlled setup is disclosed.
5. Inspect screenshots; review Q1 symmetry, Q2 consistency, Q3 counters and Q4 doc agreement. Record limits and low-importance legacy failures, commit with living docs, fetch/rebase and publish authorized main.

## Readiness

🟢 Working substrate: physical doors, repair, finite stock, native melee/effects, saves, field medicine, authored skills and Curation art.

🟢 This milestone: attack commitment/recovery, authoritative adjacent targeting, persistent cues and revised gallery generation passed the affected native EditMode and real input gates below.

⚪ Existing cached settlements retain old geometry/parts. No save migration, offscreen attack simulation, new disease vector or developer tinkering. Known intermittent attack-pose capture remains a documented lower-importance verification limit, not permission to weaken gameplay checks.


## Implementation log and preliminary review

Implemented the selected gallery, committed-melee, targeting and restrained readout contracts. Reused existing cabinet and creature art; only new persistent threat geometry is generated. Eight adjacent weapon skills now share a canonical physical-contact query, and Shank AI submits the same contact. The new Breaker description reflects chosen-target control. Existing loot quantity, public certification, keys, native doors/repair and tendril damage remain unchanged.

Observed native RED receipts: `Verification/GalleryTactics/targeting-content-red.xml` (37 targeting failures, 16 passing targeting controls, five content failures; core then 30/31 with one test-fixture stat-bound error) and `presentation-red.xml` (34 presentation failures plus the blocked-threat query failure). The core Part began with an observed missing-type compiler RED in `committed-compile-red.txt`. Do not combine these overlapping runs as a unique-case total.

The 55-case targeting adversarial fixture initially had an invalid 33-character metadata GUID and was omitted by Unity. Its GUID was corrected. No claim of prior execution for those cases. Fixture defaults also capped a multiweapon probe's bonus and a speed probe at 30; explicit Max=200 corrected the tests without altering production stats.

### Performance design

Committed attacks scan at most eight rays of two cells at initiation and one two-cell ray at resolution. The new core path uses no per-action collection allocation or LINQ; optional diagnostics/messages allocate at phase transitions. Phase changes dirty the actor and old threatened cells. Read-only threat queries dispatch no events or effect callbacks. Presentation scans current zone owners, considers only visible winding actors, and reuses a fixed 64-chevron pool; nearest actor, stable ID and ray-step order bound crowded displays. Exact cue/transform comparison skips unchanged geometry writes. Menu, pause, zone, disable and disposal paths clear state. This is a source-level budget and bounded lifecycle, not a measured frame-time or zero-GC claim for all called APIs.

### In-phase findings

- 🟡 Fixed: directional adjacent skills silently substituted the first neighboring creature. Chosen cell now owns selection; invalid selection refuses without retargeting or payment.
- 🟡 Fixed: a geometric-only threatened-cell query warned through solid interception. Text/geometry now share actual first-impact cells, with visibility gating on every segment.
- 🟡 Fixed: a readout initially reused callbacks from authoritative action checks. Presentation uses pure fields; effect callbacks remain in scheduled execution.
- 🔵 Fixed: the native containment route waited within two-cell reach of the transfer threshold. It now retreats an additional cell, requiring the real pursuer to enter holding. No enemy movement, damage or AI rule was changed to satisfy the route.
- 🔵 Fixed: an overly broad native-scenario text edit produced an invalid Check signature; caught in source review before native acceptance.
- 🧪 Retained limitation: existing optional attack-pose capture is not evidence of visual quality. Current native screenshots must be inspected separately.
- ⚪ Defensive consideration: melee ray occupants rely on the spatial index's ordinary ownership invariant. No runtime source for a carried/foreign occupant alias was found; selected skill inputs additionally validate ownership because they accept external cell references.


## Verification evidence

- Native Unity EditMode affected sweep: **770/770 passed**, zero skipped. Includes all 179 new combat/readout/content cases, existing skill and combat neighbors, Curation ownership/generation/art/input, effects, scheduling and saved-state checks. Raw `Verification/GalleryTactics/affected-green.xml`.
- Native generated-gallery input: **22/22 passed**, zero unexpected errors, `Verification/CurationAnnex/Native/b8d7bdf30f0943bfbfcaf88180197411/report.json`. Full public filing route still works. The player stood on the actual eastward lash, stepped from `(29,19)` to `(30,18)`, retained 40 HP and observed recovery. Four ordinary waits drew the same 14-HP subject fully into holding; actual door closure contained it. Both stocks depleted separately. Twelve waits and F5/F6 retained the repaired gate, closed boundaries, same subject, positions, inventory and emptied containers.
- Visually inspected native fixed-lash, recovery and restored-aftermath screenshots. The two amber arrows align with the threatened cells, disappear on resolution, and the normal focus/log convey danger/recovery. New case and existing three-dimensional furniture/actors render in the live settlement. These finite views do not establish every camera, theme or visual preference.

**Can verify (script-observable):** deterministic state transitions, exact targeting and refusal, physical interception, effect cancellation, actor timing, saved state, source-owned finite supplies, real keyboard action/door/repair/payment behavior and actual unchanged HP after sidestepping.

**Cannot verify (visual / feel):** spontaneous player discovery, enjoyment, all-build balance or reaction readability across every camera. The gallery route uses one disclosed travel shortcut into the real generated settlement; it changes no local actor/stock/AI state. Existing cached settlements are not migrated. Separate controlled-crowd acceptance is explicitly distinguished from ordinary generated encounters.


- Native directional-skill acceptance: **8/8 passed**, zero unexpected errors, `Verification/GalleryTactics/NativeTargeting/21635e0900594393acb118fe5b385141/report.json`. Actual N/Breaker selection uses the original cudgel, stats and hotbar. In an explicitly controlled stationary crowd, Slam-right damages the east foe against a real wall, spends exactly one turn/cooldown and leaves the northern foe untouched. Conk-left at an empty cell consumes no turn, energy or cooldown. No HP/stat/weapon/skill/RNG override. The first audit stopped after successful Slam because its bystander check assumed a StatusEffectsPart existed; only that test observer was corrected. Failed receipt `60629425f73e4d12acd02e606d1cfd43` is retained.
- Editor verification limitation resolved: Unity became stuck reporting compilation while holding loaded assemblies; no result from that state is counted. Restart and actual corrected native compilation restored verification. An in-progress audit file briefly produced two compile errors during restart; both were corrected before the 770-case sweep. Current production and audit code compile without errors.

### Q1–Q4 final review

Q1: begin/resolve/cancel/recovery and end-turn expiry were checked together; recovery never permanently edits DV and a cancelled windup cannot reappear. Renderer clear/dispose paths mirror creation and modal entry. Q2: all eight selected skills and the Shank preview share one contact query; physical interception and visible cues share the actual ray. Q3: positive cases are paired with absent opt-in, hidden/foreign/removed owners, empty targets, blockers, refused control, unchanged bystanders and legacy no-selection behavior. Q4: descriptions state real two-cell reach and recovery, separate deceased person from growth, distinguish near/deep supplies and preserve actual finite quantities. No outstanding high/medium finding; prior pose-capture and broad feel/balance limits remain explicitly bounded.

### Files and scope

Core: new `CommittedMeleePart`, `CombatIntentReadout`; narrow Brain/KillGoal/CombatSystem integration; shared adjacent-target helper and eight consumers; Shank tactics preview. Presentation: pooled `CombatIntentRenderer`, ZoneRenderer lifecycle, actual InputHandler visibility gate, Examine/Look integration. Content: six surgically changed blueprints and one conservation-case addition (parsed diff receipt `Verification/GalleryTactics/blueprint-diff.json`); receiving placement/stock, compatible Curation art mapping, maintenance/document/dialogue clues and corrected Breaker copy. Verification: six new focused fixtures (179 cases), existing annex stock/save/absence pins, updated Shank compensation pin, expanded real annex driver and isolated native Breaker driver/launcher. Living annex history links this iteration.

No scope divergence: the seven additional archetypes remain brainstormed future content as specified. Existing compatible models were sufficient and were inspected in game; no new mesh or animation system was required. This milestone does not claim Qud source parity, old-save migration, universal enemy delays or all-skill native visual coverage.
