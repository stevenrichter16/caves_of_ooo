# North encounter motion and readability

Status: **native source and ordinary Editor encounter accepted; package delivery next**, 2026-10-27 UTC.
This is the next scene-level pass after the East steam correction. Its outcome
is a readable encounter with the existing North Patchbearer, including its
finite medicine state, rather than another unrelated sprite. This document
was prepared by reading source, archived captures and checkpoint bytes. No
Unity session, gameplay, runtime code, art or saved fixture was changed for
the initial audit. The dated continuation below records subsequent work.

Read with [six-scene polish](GRAPHICS-SIX-SCENE-POLISH.md),
[the current delivery plan](GRAPHICS-COMPLETION-PLAN.md),
[art direction](LIVE-16X24-ART-DIRECTION.md),
[existing motion](LIVE-16X24-MOTION.md) and
[North works](LIVE-16X24-NORTH-WORKS.md).

## Outcome and boundaries

The player can distinguish the Patchbearer approaching, attacking, taking a
hit and recovering in ordinary flat16×24 gameplay. Its patched harness remains
recognizable throughout; the healing slot reflects a real carried healing
tonic. The encounter should also expose the player's existing attack, hurt,
guard/parry text and health changes clearly enough to assess their timing.

Preserve both approved North idle frames and every earlier atlas. Reuse the
approved Gleaner anatomy and established flat motion grammar as design/source
references; do not substitute the Gleaner's unstrapped runtime frames. Keep
the layout, hostile population, equipment, medicine, combat numbers, AI,
action costs and turn order unchanged. No extra enemy, new service, supplied
tonic, forced damage, teleport or test-only ability grant belongs in ordinary
acceptance. Do not make global portable-actor admission a side effect.

This is original Caves of Ooo work using the approved Qud-like readability
direction, not copied Qud art or a claim of engine parity. No new Qud-source
research is needed to establish this concrete presentation gap.

## Verification sweep and corrected premises

| Premise to avoid | Verified source/evidence and correction |
| --- | --- |
| Patchbearer has no native identity yet | `FlatPixelOpeningArtCatalog.WorksFrameOf`, lines394–413, already selects North works frame7 or8. Both are approved. The new need is truthful motion. |
| It is a recolored human with a permanent flask | `Objects.json:3680` inherits `MarlbackGleaner`, which inherits `MarlbackScrabbler`. Approved art uses shale plates, blunt snout, grasping claws, patched straps and a small cradle. Preserve that silhouette. The simulation inherits `Body`; `EntityFactory.InitializeAnatomy`, around466–510, defaults unspecified anatomy to Humanoid. That mechanical slot layout is not permission to replace the approved visual anatomy. |
| Empty harness means no supplies/equipment | Authored `Loadout.Carry` is `HealingTonic;Antidote`. The inherited cutting-tool loadout remains separate. Only the healing-slot pixels differ; an empty healing cradle says nothing about antidote or equipped weapons. |
| Low HP alone determines the bottle image | `FieldMedicinePart.FindCarriedMedicine`, lines24–49, checks the actual positive local carried HealingTonic, ownership, custody, tonic behavior and consumption eligibility. Health/control gates belong to use, not display. Healthy actors retain visible medicine. |
| Every self-use is a healing drink | `TryUseMedicine`, lines99–141, tries an applicable cure before healing. Healing requires40% HP or below, missing HP and a valid hostile threat. `EmitSelfUse(actor, zone)` occurs after actual use, dirtying and diagnostic emission, but carries no item identity. Antidote use can occur while the healing bottle remains. |
| There is no self-use event | `EntityVisualHooks.EmitSelfUse`, lines75–94, exists and rejects dead, removed, foreign/custodied actors. `FieldMedicineVisualHookTests` checks after-consumption ordering. The flat `AnimatedEntityRenderer` subscribes to movement/attack/cast/damage/death, not self-use. Do not add a duplicate gameplay hook. |
| A current Patchbearer self-use animation test proves flat support | `PatchbearerArtTests.NativeSelfUseGestureSurvivesTheFullToEmptyBodyRefresh` exercises `SpawnRing3DPresenter` and3D prefabs. It is a useful behavioral precedent, not flat16×24 acceptance. |
| Adding the species to the existing four-actor list is harmless | `FlatPixelActorMotionCatalog.IsPortableActor` also admits native idle/portrait art outside local scenes through `FlatPixelGreyboxCatalog.TryResolvePresentation`. Keep its four portable identities unchanged; new local motion needs an explicit local policy. |
| Switching a medicine asset is already safe during motion | `AnimatedEntityRenderer.FlatPixel.ValidFlatView` requires asset reference equality. A naive full/empty asset swap releases the view; `ObserveFlatCell` does not currently rebind an existing asset. Test and implement an intentional state refresh before relying on it. |
| Defense has no feedback | `BladeDefense.cs:137` already publishes `Guarded`/`Parried` event text in addition to the log. Observe the actual result before inventing a guard pose or another marker. |
| The old North still proves motion quality | `six-scene-18-north-pipe-combat.png` visibly records a measured guard, dagger damage, healing14HP and drinking a tonic. It is a still, not timing, motion or controller-feel proof. |
| An archived North checkpoint is necessarily before combat | `six-scene-north-pipe64` is tick370,25/40HP, after the documented fight. The earliest fresh opening is seed224401440, not64. See checkpoints below. |
| The worksite has a fixed spawn coordinate | `SpreadExplorationWorksites.TryPlaceCore`, lines24–96, uses current population receipts, fitting and rotations. Its optional North enrichment replaces one existing hostile and adds a Patchbearer at local offset6,0. It may fall back when the enriched shape cannot fit; actors also move after generation. Locate the actual disclosed owner. |

## Existing action routes to reuse

| Actual event | Current flat route and intended use |
| --- | --- |
| Successful movement | `MovementSystem.NotifyVisualMove` updates facing and emits `EntityVisualHooks.EmitMoved`; `AnimatedEntityRenderer.OnMoved` calls `MoveFlatView` for an existing eligible view. Keep immediate simulation movement,100ms two-pose display, adjacent forced idle slide and distant snap. Blocked movement must remain idle. |
| Accepted melee, including a miss | `CombatSystem.PerformMeleeAttack` emits Attack; `OnAttack` uses actual facing and `RequestFlatReaction(Attack)`. Keep pre-attack-veto versus accepted-miss distinction. A generic equipment-neutral strike must not paint a fixed dagger or imply every attack is a rake. |
| Positive damage | `CombatSystem.ApplyDamage` publishes Damage; `OnDamage` asks for Hurt only for positive damage and a valid current view. Zero damage does not imply injury. |
| Same-frame attack and retaliation | Existing flat policy exposes a reaction for at least60ms, coalesces one pending reaction and has40ms recovery. Reuse it; do not insert simulation pauses or an unbounded animation queue. |
| Death/removal | `OnDeath` captures the visible owner before ordinary removal for its short existing fade. Hidden deaths and simple removal must not create a ghost body. No dedicated new death art is required. |
| Finite medicine | `KillGoal` and `FleeGoal` try the real treatment before continuing their action. A successful use replaces that whole action. Static supply reconciliation already follows actual inventory. Preserve this route and display the resulting full/empty body correctly during any reaction. |
| Self-use gesture, if later justified | The committed actor-only event is available. The minimal pass does not require a new state/pose. If actual capture demonstrates a significant remaining ambiguity, design a neutral self-treatment gesture or a properly identified event; never represent every self-use as drinking the healing bottle or repurpose Cast/Attack. |

The current canonical poses face east and flip for west. North/south retain
the last horizontal orientation. This pass does not promise four or eight
newly authored directional rows. Flat rendering stays unlit white, square
pixel aligned, with no new bounce, shadow, shake or pseudo3D lunge.

## Readiness and smallest implementation slices

- 🟢 Real actor, combat, finite inventory, event routes, approved idle pair and
  real combat reference are available.
- 🟢 Earliest earned fresh checkpoint has an ungenerated North graph and full
  starting HP; a seed64 alternative also exists.
- 🟡 Motion derivatives, local admission and medicine-state continuity need
  source production and native tests before integration.
- 🧪 Fresh ordinary approach, time-resolved reactions, return/revisit and
  packaged playback are pending. Static captures do not close these gates.
- ⚪ Additional Magpie/Viper/Nella motion, all-weapon overlays, a special
  drinking animation and new defense art are outside this bounded pass.

### N1 — Capture the encounter before changing it

Use one unchanged earned checkpoint in an isolated normal save root, record
hashes, and Continue normally. Walk into North with ordinary input. Capture
1× and2× overview/approach, exact Patchbearer Examine, one sequence of
movement/contact/retaliation and recovery. Record seed, game ID, tick, zone,
player/actor IDs and positions, HP and actual healing-slot state. Save a new
earned pre-contact checkpoint when possible; never overwrite the archive.
Use the captured scene and the approved two idle frames for imagegen design.
Reject invented equipment, enlarged bodies, replaced anatomy or changed
layout. Reuse the present geometry; no greybox rebuild is needed merely to
change motion.

### N2 — Author the minimum motion derivatives

Proposed owned source: `ArtSource/Live16x24/north-patchbearer-motion/` and
`Tools/Live16x24/build_north_patchbearer_motion.py`. Produce a minimal atlas
of WalkA, WalkB, Attack and Hurt for each healing-slot state: eight derivative
cells if the concept confirms this is sufficient. Borrow the existing two
idle frames byte-for-byte at runtime for idle/recovery/portraits. Keep
equipment-neutral hands and the same silhouette proportions, baseline,
palette and cradle distinction. No new idle variation is needed.

Before production, run a source verification RED for missing/incorrect
outputs. Use the established editable native integer-cell Blender workflow,
direct native and contact renders, binary alpha and16×24 frames. Concept
images remain references, not downsampled production sprites. Store parts,
cells, blend, manifest, references, exact prompt, verifier and reproduction
notes. Verify approved atlas/generator preservation and deterministic decoded
pixels. Defer Assets import until the editor owner is ready. New Unity files
need fresh valid `.meta` GUIDs and Point/uncompressed/no-mipmap settings.

### N3 — Integrate local motion with truthful inventory state

Prefer a small local adapter/catalog alongside the existing motion catalog.
It may reuse the existing compact `EntityVisualAsset` frame player. Keep
`IsPortableActor` unchanged. Require the same actual owner/parts/appearance
and local-scene authority used by its current idle art, plus existing current
graph, actual visible top owner and custody gates. Preserve the six-scene
admission boundary rather than silently introducing a global fifth identity;
North is the ordinary acceptance location, not a claim that every such
encounter has been reviewed.

Cache full/empty assets once; do not allocate sprites or scan all entities
per frame. When actual local medicine state changes for a tracked eligible
view, switch its body/state frames atomically while preserving current cell,
facing, movement position, reaction elapsed time and at most one pending
reaction. Do not restart or drop Hurt simply because the bottle disappeared.
Supply watchers must continue handling a stationary actor, coverage and
uncovering. Reconcile only disclosed tracked owners and local inventory.
On a missing/malformed atlas, leave the existing correct static body visible.

Do not change gameplay to guarantee a treatment or attack for the camera.
If scope requires a shared renderer change, keep existing four actors and
legacy presentation behavior under regression tests. Self-use pose integration
is an explicit later decision, not an automatic dependency of this slice.

### N4 — Review the whole encounter and deliver

Re-run the same archived checkpoint and ordinary route. Compare actual
motion and both medicine states against the design and baseline. Fix concrete
Yellow-or-higher visual/state failures, then capture the same player action
sequence at1×/2×, quiet idle, reader/menu return, travel and revisit. If the
guard/parry text or player's hurt remains unreadable, record the precise
observed failure and address that next; a combat still is insufficient.

Update this document, six-scene acceptance and coverage in the same scoped
commit using `CLAUDE.md` §2.3. Refresh packages only after source/test/art
acceptance, retain old builds and use normal packaged Continue. Linux build
success and a Mac encounter do not establish physical Deck playback or
sustained performance.

## Bounded RED → GREEN and counter-checks

Use the existing `FlatPixelActorMotionTests` fixture and real-content
`EntityEquipmentContentFixture` where possible. Add only tests which protect
the new invariants; no duplicate general animation framework is needed.

| Invariant | Positive and counter/adversarial evidence |
| --- | --- |
| Exact stateful owner | Actual `MarlbackPatchbearer` with owned parts resolves both stock states. Wrong blueprint, foreign/missing Body/Brain/FieldMedicine/Inventory/Physics/Render, visual overrides, carried/equipped, corpse and foreign graph refuse this identity. Pair with a valid otherwise-identical owner. |
| No portable scope expansion | The new owner works inside current admitted local graphs. Nonlocal world/portrait remains its prior fallback; the four existing portable actors still work. POI replacement, wrong biome and detached/foreign manager refuse local art. |
| Actual medicine only | Full state uses the actual positive carried HealingTonic. Floor tonic, foreign custody, zero stack, equipped tonic and different medicine do not fill the healing slot. An Antidote-only actor keeps empty healing art; healthy valid carried tonic stays full. |
| Correct source/recovery | Actual resource sprites match approved full/empty idles, correct native rect/pivot, distinct deliberate walk/attack/hurt derivatives and recovery. Missing either atlas retains static art without invisibility or duplicate bodies. |
| Simulation-owned movement/combat | Real successful movement shows both walk poses; blocked movement does not. Accepted miss shows attack; veto does not. Positive damage shows Hurt; zero does not. HP, inventory, position, RNG and action energy are unaffected by rendering/ticking. |
| Medicine changes during reaction | Start actual attack/hurt, commit actual medicine use or transfer, then render. Correct empty body appears without resetting elapsed time, losing pending reaction or replaying a fake attack. Reverse transfer restores full state. No-supply rejection leaves frames and turns unchanged. |
| Supply remains independently truthful | Consuming a cure while healing tonic remains does not empty the healing slot. Successful healing use spends exactly one real unit and one action through existing routes; flat display adds no second action. |
| Visibility and lifecycle | Covered actor retains only bounded reconciliation state; uncover restores its real state. Hidden/remembered owners, removal, death, replacement, fullscreen, renderer switch, zone switch and save/load cannot leave live ghosts or reveal medicine. Unrelated dirty cells retain cached views. |
| Shared regression | Existing four-actor pose/queue/projection tests, global flat admission, North medicine watcher/world/portrait tests, actual FieldMedicine hook tests and scoped3D state-switch tests remain green. Do not label the3D tests as proof of flat motion. |

Run genuinely failing new assertions before production integration, then the
focused native suite, paired counters and a cold-eye review. Record exact test
identities/counts and any existing failures. The audit itself ran no Unity
tests; these are gates, not reported passes.

## Earned checkpoints and ordinary route

The following facts were read from unchanged gzip format8 header/current-zone
and cell-reference bytes, cross-checked against `Quick.json`. This is a bounded
prefix audit, not a full save validator or current-Unity load certification.

| Checkpoint | Verified state and use |
| --- | --- |
| `Verification/Live16x24/Fixtures/playable-zoom-fresh-opening` | **Earliest usable pre-North candidate:** seed224401440, game`b343f331bf5d40a189c86e4aaba9aa47`, tick10, HP40/40, player40,12 in`Overworld.11.10.0`. Only the glade is cached; North has not generated. `Quick.sav.gz` SHA256`e93b4b6394dcf1aae671881fed91393f3c410befa18f3870416588a3d640fe7f`. Ordinary Warden start provenance is in `GRAPHICS-G3-PLAYABLE-ZOOM.md`. |
| `Verification/Live16x24/Fixtures/playable-zoom-earned-stairs224401440` | Same game, tick240, HP40/40, glade28,19. Glade and cellar cached, North absent. Later alternative if a glade route obstacle makes it useful; not the earliest start. |
| `Verification/Live16x24/Fixtures/east-shelter-pre-generation64` | Seed64, different game`8d2b4d6184484425819ffe087ed83dae`, tick1040, HP40/40, glade79,16. Only glade cached. SHA256`8ef117dd004c00196da5028a74a6f76a8d8881551778a28baa449a0f3fbe362a`. Same seed as old combat does not reproduce its history, IDs, time or exact pursuit. |
| `Verification/Live16x24/Fixtures/six-scene-north-pipe64` | Seed64, tick370, HP25/40, North31,20. **Post-fight; do not use to claim fresh living-enemy acceptance.** SHA256`ff46ccb5220dd0889f31e31975da5fcf1015dc37e67888ea5a2e185eecd061b6`. |

The older fixture-specific `/tmp/coo-earned-temper-audit-final.py` full-body
audit rejected the fresh-opening fixture on its duplicate-ID assumption.
No universal-save conclusion was drawn from that failure. This plan uses
only its independently checked primitive reader to inspect the documented
header and cached-cell prefix; it did not skip that assertion and call a full
audit green. No fixture bytes were rewritten.

Ordinary action sequence:

1. Back up and isolate the active save/preferences with the existing reversible
   workflow. Copy the chosen unchanged fixture into the isolated slot, launch
   SampleScene or the current app and choose Continue. Confirm the observed
   seed, tick, HP, coordinate and flat mode before moving. Archive a failed
   load as a failure; do not repair the fixture to force a visual pass.
2. At glade40,12, use ordinary arrows to approach the north edge. `L` and
   cursor arrows inspect actual terrain/owners; Enter opens the selected
   action menu/Examine; Escape returns. PageUp/PageDown select the existing
   zoom levels. Use individually observed steps around actual walls and
   occupants. Do not treat an inferred count of Up presses as a verified
   walkthrough. Cross the north boundary into`Overworld.11.9.0`.
3. Locate the disclosed worksite and actual Patchbearer using the visible
   world and Examine. Its six-scene64 precedent was owner4924 at29,18 while
   the player reached32,23/tick230; those are reference coordinates only.
   The earliest fixture has another seed. Archive F5 through normal input at
   a safe pre-contact point when practical, recording current stock and HP.
4. Start time-resolved Game capture before a movement key. Use individual
   ordinary approach steps and deliberate bump attacks, observing actual
   turn outcomes. Capture attack→retaliation/Hurt→idle, both walk poses and
   movement-interruption behavior. Existing En Garde/hotbar input may be used
   only if available in that actual start; capture real guard/parry results,
   not a scripted success. Retreat or use an actually carried tonic if needed.
5. If actual combat triggers treatment, capture the use log/HP change and
   full-to-empty healing slot through recovery, then Examine the survivor
   if safe. If it dies before treating, record that outcome honestly; the
   pre-contact archive allows another ordinary attempt. Never prolong a
   fight with debug HP, forced hits or replenished enemy supplies.
6. Finish with ordinary movement over the former actor cell, quiet frames,
   inventory/reader open-close, F5/full restart and glade return/North revisit
   where the earned state permits. Capture no body duplication, stale bottle,
   actor ghost or persistent motion. Keep each checkpoint's provenance.

Use actual1280×800 Game output at1× and2×. A short end-of-frame sequence or
video should observe the real input interval without slowing simulation;
separate stills of the same pose do not prove animation. Read-only capture
helpers may observe, never dispatch gameplay hooks or mutate actors to
manufacture the event.

## Self-review and reporting limits

🟡 **Asset-state continuity is the main integration risk.** The current
reference-equality contract makes a naive full/empty swap unsafe; preserve
reaction timing and cover/uncover invalidation under a failing test first.

🧪 **No fresh route or current-build load was performed during this audit.**
The prefix proves an earlier ungenerated North state, not that any requested
encounter pose will occur at a particular coordinate or tick. Confirm in N1.

⚪ **Self-use pose deliberately optional.** A real event exists, but lacks
medicine identity. Existing stock state plus log is truthful. Add a pose only
for a demonstrated readability need and do not claim bottle-specific drinking
from the generic event.

Can verify now: source/state/event contracts, approved art provenance, actual
archived combat still and bounded serialized checkpoint prefix. Cannot verify
now: newly improved animation, feel, fresh Continue/route, GPU cost, package
parity or physical Steam Deck performance. Record those separately when run.

Implementation log: pending N1 capture, then source RED/design/native art,
integration RED→GREEN, ordinary encounter review and scoped delivery.
Files changed in this audit: this document only.

## N1–N3 continuation — 2026-10-27 02:24 UTC

The unchanged earliest fixture loaded through ordinary Continue: seed224401440,
one cached glade, player40,12,40HP,tick10. Normal Up input crossed into North
at40,24,tick140. Its Patchbearer4940 was actually at6,19 initially, not the
seed64 reference location. Ordinary approach through the pipe worksite produced
an earned pre-contact checkpoint at11,19,tick510,36HP. The adjacent Scrabbler
was defeated normally; subsequent approach and bump attacks reached the
Patchbearer. It genuinely healed18HP and consumed its healing tonic. Exact
Examine showed the empty cradle and the separate remaining Antidote. Final
recorded exchange: player6,21,2HP,tick660; Patchbearer6,20,6HP. No health,
inventory, position, revelation, event or simulation timing was modified.

Evidence: `Verification/Live16x24/north-motion-baseline.json`, seven actual
`north-motion-before-*.png` Game captures, earned fixtures
`north-motion-precontact224401440` and `north-motion-baseline-after224401440`.
Existing bounded ActionCapture helper exported24 frames in
`G2Action-20261027-020419-570` and5 in `G2Action-20261027-020531-687`, no export
errors; additive event observers were removed before stopping Play. The first
frame cap exhausted during approach at tick590; its later event log still
records actual Patchbearer combat, but is not a later image sequence. The second
sequence records the player's real attack and recovery while Patchbearer stays
static. Neither capture is continuous video or uninstrumented timing proof.

The actual zoomed empty-cradle encounter and approved North works contact were
fed to built-in imagegen. Exact prompt and concept are retained in
`ArtSource/Live16x24/north-patchbearer-motion`. Accept restrained leg alternation,
generic claw extension and compact recoil; reject the concept's oversized detail,
larger body compression and any implied weapon. Source RED preceded native
production. The first native8-cell contact was visually reviewed at1x and8x;
approved idles remain borrowed, and full/empty derivatives differ only in their
actual healing-slot pixels. This is approval for in-game trial, not acceptance.

Native behavior RED:38 cases,37failed/1passed at02:22:30–33UTC, retained in
`north-motion-red.xml`. Real cell takeover fails with zero animated views and
the adapter is absent; static missing-motion fallback already passes. The first
run during reload selected0tests and is explicitly not counted. Production
implementation began only after this verified RED.

Readiness corrections from independent source review: validate canonical disclosed
top ownership before stateful medicine lookup; refresh assets before existing
reference-equality retirement; rebind on both quiet and event paths. Death drops
inventory before its callback, so capture current stock after the drop before
freezing the dying view. A detached authoritative manager is valid; reject stale,
foreign or unmanaged connected graphs without tightening existing glade policy.

Baseline closure verified: Play off, pending false, normal save root restored,
clean scene. Normal save trees, gameplay preferences,34 unrelated tracked edits
and current delivered packages remain preserved. Storage inspection only.
Hourly report sent02:25UTC: East steam delivered/pushed with211native cases and
actual Mac restart; North baseline/design/native derivatives complete for trial,
integration in progress. Coming-hour goals are the gates below. Next report03:25UTC.

Next: minimal integration, native GREEN/adversarial/regressions, actual same-save
after capture, review and delivery. Whole-opening and physical Deck gates remain open.


## N3–N4 verified source — 2026-10-27 02:50 UTC

Eight native derivatives are imported. A local catalog borrows the two approved
idles, caches ten owned sprite slices over two borrowed textures and admits the
existing exact disclosed owner only. Full/empty asset refresh preserves view,
cell/facing, interpolation, elapsed reaction and the bounded pending reaction.
Other actor identity changes retain retire/reacquire behavior; the four globally
portable identities are unchanged. Missing motion art falls back to approved
static art. Death reads stock after ordinary inventory drops before freezing its
brief existing fade. There are no AI, combat, inventory or turn-cost changes.

A new portable-identity counter exposed an overly broad in-place refresh:
`north-motion-identity-red.xml` records38passed/1failed. The final refresh is
restricted to this catalog's full/empty pair. Native combined GREEN records
272passed/1skipped, and separately imported adversarial tests6/6pass: **278passed,
1skipped,0failed**. The skip is the existing allocation probe whose known-allocation
counter does not work; this is not a zero-allocation result. Earlier initial RED
was37failed/1passed. XMLs and author/independent review receipts are retained.

Ordinary Continue from the earned tick510 checkpoint produced real full-state
walk/attack/hurt, then actual healing12HP and consumed tonic at640. Empty-state
Hurt continued through recovery and subsequent attack/walk, matching exact
Examine with Antidote still carried. Both1x and2x were inspected. The final
ordinary blow at720 killed the2HP actor; sampled empty dying Hurt retired, then
normal movement crossed its cell, returned to glade740 and revisited North750.
Corpse/loot remained with no live ghost. F5 earned final17HP checkpoint.
No actor, inventory, HP, revelation, event, position or simulation timing was
changed for the camera. Baseline combat outcomes differed naturally; this is
same-checkpoint comparison, not identical RNG replay. Existing En garde text was
visible, but Guarded/Parried success was not observed and remains unaccepted here.

Five actual bounded sequences contain60frames, plus seven original1280x800 Game
stills. Full/empty body stays aligned; no duplicated body, stale healing bottle
or persistent readout found by independent review. Default-scale detail remains
subtle; displacement/outline and2x improve readability. These are sampled frames,
not continuous video. Exact route, test receipts and capture lists are in
`Verification/Live16x24/north-motion-editor-play-summary.json`.

Profile:73.0036s stationary North/reader/tool session,64,458 samples. Renderer
mean0.058324ms,p990.096ms,max3.112083ms; draw calls mean38.8068,max41. No capture
readback during sample. This is neither controlled before/after nor combat,
standalone or Deck benchmark. Recorders disposed. Existing Metal memoryless
load/store diagnostics remain; no clean-console claim.

Self-review: 🟡 fixed generic identity-refresh regression under RED; 🟡 fixed
hidden-owner authority ordering and death-after-drop state before GREEN.
🔵 original163 source/art inputs unchanged; new full/empty differences contain
only the eight healing pixels. 🧪 packagedContinue/restart still pending;
physicalDeck, Linux execution, guard/parry and sustained performance remain open.
⚪ no drinking pose or directional rows: generic SelfUse also covers Antidote.
Independent source and visual reviews found no outstanding material defect.

Files: new local motion catalog,8-frame resource and metas; scoped edits to
AnimatedEntityRenderer/FlatPixel,EnvironmentSpriteRenderer and shared idle resolver;
39motion cases,6adversarial cases and global admission counter; editable Blender,
cells/generator/concept/verification; actual evidence, earned fixtures and living
docs. Root closed Play with pendingfalse,normalroot,clean SampleScene and removed
capture observers. Normal saves/gameplay preferences,34foreign tracked edits and
previous delivered packages preserved. Three reviewed whitespace-only importer
changes restored from HEAD after backup. Storage inspection only,~411GiB free.

Next: freeze/push exact reviewed source, fresh compressed Mac/Linux builds,
ordinary MacContinue/full-empty encounter/save/restart; preserve old packages.
Then East Cindercaller movement/cast attribution beside animated Scrabbler is the
next named scene-level gap. Existing Ember Spit FX/audio works; capture the actual
caster before changing its body. Whole six-scene acceptance remains unfinished.
Last hourly report02:25UTC; next03:25UTC.
