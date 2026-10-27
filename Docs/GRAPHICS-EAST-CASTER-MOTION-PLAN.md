# East caster encounter: motion and action attribution

2026-10-27. **DEFERRED by the user after E1; no motion runtime integration.**
The latest priority is missing sprite coverage across all biomes/areas before
nuanced animations. Preserve this work for later; do not resume it ahead of
that coverage. The original plan below is historical except for the new
continuation at the end.

Original pre-implementation plan: North motion source
`2375284dd` is delivered to main; actual Mac encounter/restart and matching
Linux archive are now verified in the North delivery record. No East runtime,
art, scene, save or editor changes were made for this audit. Root retains Unity
ownership. Read the latest continuation in
[Six connected scenes](GRAPHICS-SIX-SCENE-POLISH.md) and
[North motion](GRAPHICS-NORTH-ENCOUNTER-MOTION-PLAN.md) before execution.

## One scene-level outcome

At the real East tempering shelter, the player should distinguish the
Cindercaller moving to a firing lane, spitting, closing to melee and taking a
hit while its Scrabbler partner advances. Preserve the approved native body,
actual walls/forge/forage, steam, existing spell effects and audio. This is one
connected encounter pass, not a new bestiary or an excuse to replace scenery.

The reference is the user's approved flat16×24, Qud-like readability. This is
original Caves of Ooo presentation; no Qud source, art or mechanic-parity claim.
Follow [art direction](LIVE-16X24-ART-DIRECTION.md)'s actual capture → imagegen
concept → editable native derivatives → real integration → gameplay review.
The [completion plan](GRAPHICS-COMPLETION-PLAN.md) keeps whole-scene acceptance
ahead of isolated asset counts. Do not author a new packet before E1 below.

## Evidence and corrected premises

The historical seed65 [world/movement capture](Verification/Live16x24/cindercaller-native-moved.png)
and adjacent JSON record caster4983 at78,5 after ordinary movement from78,7,
with the approved static image. That Oct24 run is not the later seed65
pre-generation checkpoint. Its stored save path is not located in the retained
record, so do not claim it is an available exact replay.

The Oct26 [six-scene32 encounter](Verification/Live16x24/six-scene-32-east-pipe-encounter.png)
is a Scrabbler fight. The newer Oct27 steam captures and Mac restart prove the
vent cue, not casting. No current close-up, time-resolved Cindercaller cast has
yet been accepted. E1 must establish the actual readability problem in motion;
absence of such a capture alone is not a broken spell-effect finding.

| Premise | Verified correction and boundary |
| --- | --- |
| The Cindercaller is a player build or an ordinary Scrabbler skin | `Objects.json:41592` defines exact `MarlbackCindercaller`, inheriting Scrabbler mechanics with12HP, sight6, EmberSpit,35% tactic chance and allied assistance. Keep its approved narrow cap/scorched mantle identity. |
| Its carried FireMoss is ammunition | The loadout equips a Cudgel and carries FireMoss, but `Pyromancy_EmberSpit`, `SpellSkillPart`, `SkillsPart` and `CombatTacticsPart` contain no FireMoss requirement or consumption for this cast. FireMoss is carried loot/reagent. No full/empty ammunition body states or gameplay redesign. |
| Every East shelter has the same pair at the same coordinates | `SpreadExplorationWorksites.cs:30–65,161–174` replaces an eligible two-actor population with the caster/melee pair; placement is fitted to current generation. Exact East ranged owners gain `RepositionForShot`. IDs/positions and even successful pair placement must be observed, not assumed from a screenshot. |
| A static body already has motion because its parent is animated | `FlatPixelIncidentalActorCatalog.cs:35–74` supplies one exact neutral Cindercaller tile. `FlatPixelActorMotionCatalog.cs:61–74` admits only Player/PetDog/exact Scrabbler/exact Gleaner. The new Patchbearer adapter does not admit a caster. |
| The defect calls for a new projectile or native3D rig | Flat mode clears the3D native surface in `ZoneRenderer.FlatPixel.cs:120–134`. `NativeSpellFxRenderer.Play` refuses without that surface; the coordinator then uses the projected `SpriteSpellFxRenderer`, followed by ASCII fallback. Existing fire sheets, geometry and Ember audio already exist. Preserve them. |
| CastCallback is a new gameplay event to add at attack time | `SpellSkillPart.OnCommand` commits one `SpellFxCapture` only after successful resolution. `WorldFxCoordinator.Play` emits CastCallback when it accepts that resolved presentation. Off/hidden/wrong-zone rejection skips it. Do not emit a duplicate event from AI or the spell. |
| Adding a cast PNG makes it display | The compact `EntityVisualAsset` constructor has no casting-art argument; flat `OnCast` deliberately selects Idle, and `TickFlatView` has no Cast branch. All three seams need bounded handling if E1 confirms a cast pose is useful. Other actors retain their existing unauthored-cast Idle behavior. |
| Existing3D gesture duration can be copied into flat mode | Current flat fallback receives `min(.24f, playbackDuration) / AnimationSpeed` from the coordinator. EmberSpit's sprite definition separately has.12s cast,.10s charge,.025s/path-cell travel,.20s impact and.18s aftermath. Follow the actual hook duration; do not change projectile/contact timing or turn waiting. |
| Cudgel ownership, spell cooldown or health define the neutral body | Existing static eligibility intentionally survives unequipping, reagent removal, mood/name changes, health changes and cooldown. Keep those semantics. No fixed held weapon, permanent flame, glow or readiness indicator in the idle. |

Primary source references above are under `Assets/Scripts/Presentation/Rendering/`
except gameplay classes under `Assets/Scripts/Gameplay/Skills/`, AI under
`Gameplay/AI/`, generation under `Gameplay/World/Generation/Builders/`, and
`Assets/Resources/Content/Blueprints/Objects.json`. The existing
[static integration record](GRAPHICS-G2-CINDERCALLER.md) and
`ArtSource/Live16x24/cindercaller/README.md` give the approved source provenance.

## Actual event and ownership route

```text
ordinary NPC turn → CombatTacticsPart.TryUseAbility → owned skill command
  → SkillsPart.TryRouteSkillCommand → SpellSkillPart.OnCommand
  → Pyromancy_EmberSpit.ResolveSpell → committed SpellFxCapture/SpellFxBus
  → WorldFxCoordinator accepts visible resolved sequence
  → existing sprite projectile/impact + Ember audio + EntityVisualHooks.EmitCast
  → optional admitted caster view pose; simulation has already resolved

ordinary movement / melee / damage / death
  → existing EntityVisualHooks → existing bounded flat actor view/recovery
```

EmberSpit is range4, cooldown8 and direct Heat damage3 before ordinary modifiers,
with first-body line contact, possible ignition and existing residue/reactions.
Walls, range, cooldown and the35% AI chance can prevent a cast. None should be
changed to make a capture convenient. The existing specialist test validates
real power, cover and cooldown; it is not a new ordinary-play result.

`EnvironmentSpriteRenderer.ResolveCell` suppresses static art only after an
observer commits to drawing that exact selected visible owner. The adapter must
stay inside the Cindercaller's existing six-graph native admission, not the
globally portable set. Require current authoritative graph, current physical
cell membership, disclosed top owner, owned visible Render/Physics/Body/Brain,
Creature and non-Player/non-Item, canonical `g`, no explicit visual override,
single-cell footprint and no inventory/equipped custody. Reuse the current
static predicate if extracting it avoids divergence; do not weaken it or require
the optional generation role. A missing or malformed motion resource leaves
the existing static body and reader intact.

## Earned start and ordinary route

Use [east-shelter-pre-generation65](Verification/Live16x24/Fixtures/east-shelter-pre-generation65/README.md),
never overwrite a normal save. Independent read-only metadata and bounded v8
prefix inspection establish:

| Field | Recorded value |
| --- | --- |
| Game / seed | `50e670c30b6d48feb82756df64bd1cbc` /65 |
| Current graph/cell | Glade `Overworld.11.10.0`,79,16 |
| Earned state | Tick440,40/40HP,50drams; ordinary Warden, LongSword/Buckler,2HealingTonic in the adjacent actual pre-generation receipt |
| Cached graphs | Glade only; East not generated |
| `Quick.json` |410bytes; SHA256 `2e7011bd50289e5d1f95e5aa370f24c26402c5b61ff17c06e2cf44f11e9ba3cd` |
| `Quick.sav.gz` |70,275bytes; SHA256 `c127bc22987239edec2a79a384602f706d0bcb5acba03e370a07972744ffab46` |

This proves an early earned continuation, not current-source load success or
an already-saved caster with unused supplies. The current generation and first
disclosure must establish the new actual owner. Root alone runs the isolated
Continue and archives earned checkpoints before stopping isolation.

1. Confirm root's editor/build/test ownership is free. Protect current saves,
   preferences, foreign changes and packages using the existing isolation flow.
   Continue the unchanged fixture with normal presentation. Record actual state.
2. Ordinary Right previously crossed to East0,16 at tick450. The historical
   entry receipt placed forge65,4, caster4991 at67,3 and Scrabbler4994 at64,5.
   These are navigation context, not required new IDs or a guaranteed layout.
3. Prefer the upper approach in short observed segments. Prior lower approach
   through the shelter produced Burning and, on another attempt, player death;
   do not blindly replay a long south/east key batch. Use visible current paths,
   stop before the pair's sight range, and F5/archive a safe pre-contact point.
   Historical upper-route evidence reaches the row3 approach; verify each current
   endpoint and stop well before closing to the caster (around47,3 only if that
   geometry is actually present). Do not use a timed-out requested destination
   as proof the player reached it. The intermediate waypoints are prose in
   [East meadow's ordinary journal](GRAPHICS-G3-EAST-MEADOW-COMPOSITION.md);
   `east-meadow-after65-entry`, `-approach` and `-forge-reader` PNG/JSON files
   retain the start/end evidence, not a separate receipt for every waypoint.
4. Begin bounded time-resolved Game capture before short approach steps. Let the
   real AI move/reposition and cast while the player uses ordinary cover, wait,
   existing skills or carried healing. Record caster identity, actual path/target,
   command success/cooldown and log alongside the images. Check readable actor
   attribution at1× and2×, including its partner. A live callback observer may
   record events additively; it must not dispatch them or modify simulation.
5. Capture actual melee/hurt/recovery when it occurs. Obtain an actual cast
   before claiming casting acceptance; if it never happens, retain the result
   honestly and use the preserved pre-contact state for another ordinary attempt.
   Never force RNG, HP, hostility, position, spell choice or cooldown, grant stock,
   disable the partner, reveal cells or slow simulation to manufacture evidence.
6. After integration, replay the same earned pre-contact checkpoint, compare
   disclosed owners at both scales, then normal F5/restart and border return.
   If the caster dies naturally, observe fade/removal, loot and former-cell
   traversal. Different combat outcomes are not an identical RNG/pixel replay.

## Smallest implementation, conditional on E1

Retain the existing immutable16×24 idle and reader. Its source/runtime PNG SHA256
is `ab7fb7010cd20d3b9edab00cd48747aade51c124f8916eff6df0a5534ef72394`.
It uses7 approved family colors,119 opaque cells, a row22 baseline and empty
row23. New derivatives must keep that scale and anatomy; do not enlarge the
body to fit concept detail. No directional-row expansion is required.

Expected maximum initial parts list: **walkA, walkB, short generic melee action,
compact hurt, and one distinct spit pose**, borrowing the existing idle for
recovery. Five derivatives would fit an80×24 atlas, with cached bottom-anchored
sprites; the source shape/pose count remains subject to actual E1 critique.
The spit pose moves the head/upper body without a permanent fireball or weapon.
No ammo states, generic idle bouncing, grazer/Farmer motion or scenery changes.

The minimum runtime design is a local optional Cindercaller motion catalog
alongside the Patchbearer adapter, plus the existing cell-observer admission.
It owns generated Sprite slices and borrows textures. Preserve all four portable
identities and Patchbearer full/empty refresh semantics. This single-state caster
does not need a new mutable asset-refresh rule.

If a cast pose is authored, add a backwards-compatible compact asset overload
that accepts that pose and recovers to its borrowed idle. Existing compact
callers still have `HasCastingArt == false`. Gate use of the new spit pose to
this catalog's asset and exact `Pyromancy_EmberSpit`; a mutated/new skill must not
borrow a misleading spit gesture. Extend only the flat OnCast/flat tick path:

```csharp
// Proposed behavior, not implemented by this plan.
// Keep existing current-view validation, cell anchoring, facing and queue reset.
if (exactCasterAssetSupports(spellID) && view.Asset.HasCastingArt)
    BeginState(view, EntityVisualState.Cast, sanitizedExistingHookDuration);
else
    BeginState(view, EntityVisualState.Idle, 0f); // existing unauthored behavior
```

Keep the actor at its real cell; no3D bob, root motion or duplicate projectile.
The Cast tick chooses the authored pose then idle, bounded by the hook's existing
duration. Define cast→hurt/melee interruption under tests using the existing
minimum-exposure/coalesced-reaction approach; never add an unbounded queue or a
second simulation/FX wait. Movement, death, disclosure loss, view hide and zone
change must retain their current cancellation/retirement behavior. FX Off means
the coordinator emits no new cast hook; no new global cancellation bus is needed.

Preserve `east-steam.png` SHA256
`02703f7b4db68f73c7b50e6cc48fba7a92eb3ee115d50dc8178fe52d89c50aa5`, both
existing fire FX sheets, all Ember audio, approved base art and their generators.
No generation, AI, combat balance, inventory, turn-cost, save-schema or legacy-save
work is planned. Record any evidence-driven scope change before implementation.

## Milestones and gates

| Milestone | Required result |
| --- | --- |
| E1 — current encounter baseline | Isolated ordinary Continue, safe earned checkpoint, actual movement/close combat and genuine cast captured at1280×800. Name the demonstrated readability issue and exact current owners. Reuse coherent existing geometry. |
| E2 — bounded visual design | Feed the actual encounter plus approved caster idle/family art to imagegen; retain prompt, inputs, critique. If derivatives are warranted, source-verifier RED before original editable native output, direct native/contact renders and independent visual review. Source-only approval is not gameplay acceptance. |
| E3 — integrated behavior | Native behavioral RED before production edits, then the minimum local adapter and cast support. GREEN plus paired counters/adversarial review below; imported defaults and actual submitted body verified. |
| E4 — actual after and delivery | Same earned encounter through normal input, genuine cast with existing FX/audio,1×/2× review, recovery/removal/restart/travel and bounded profile. Review/fix, update living docs with exact limits, root fetch/rebase/push/builds and actual Mac smoke. Keep older packages. Physical Deck remains separate. |

### Required native checks and counterchecks

- **World takeover:** real factory caster, current admitted East, actual
  EnvironmentSpriteRenderer path produces one motion body and no duplicate static
  body. Flip hidden/unexplored/covered/stale graph/custody/override/footprint to
  refuse motion. Current static fallback survives missing/malformed resources.
- **Identity and state purity:** no inherited Scrabbler/Soursprayer/grazer/Farmer
  alias, no outside-scope portable expansion, stable approved reader. Health,
  mood, FireMoss removal, Cudgel unequip and cooldown preserve neutral identity;
  repeated queries do not alter them or advance gameplay. Keep earlier injected
  resource constructors resource-free and disposal limited to owned slices.
- **Actual event wiring:** native unit fixtures use real movement and combat,
  plus the real successful EmberSpit command/capture→coordinator path for casting.
  Do not make EmitCast-only tests the integration proof. Assert exactly one
  presentation event, unchanged spell result/stock/cooldown after presentation,
  and existing FX playback completion. Pair with cooldown/blocked/refused command,
  wrong zone, hidden source with visible contact, and FX Off. UI/runtime input is
  reserved for ordinary Play; detached tests may construct controlled fixtures.
- **Cast is optional and exact:** default imported caster art has a cast pose;
  null cast art, original four actors, Patchbearer and an unrelated spell retain
  their existing Idle cast fallback. Preserve projectile/audio resources and
  selected backend; do not call the3D native mesh path flat gameplay evidence.
- **Ordering and lifecycle:** movement→cast anchors correctly; cast→hurt/melee
  retains bounded readable exposure; repeated/overlapping events cannot prolong
  state indefinitely. A new cast pose must not warp an actor or play at a stale
  source after displacement between resolution and presentation; pair the normal
  source-cell match with this mismatch. Test death during cast, owner replacement with equal ID,
  visibility loss, UI hide/resume, disposal, zone replacement and return to idle.
  Inspect real selected-owner/submitted sprites, not only selector return values.
- **Isolation and regression:** restore modified visual callbacks, FX settings,
  static content/manager bindings and transient queues in fixtures; retain borrowed
  textures. Run affected existing `FlatPixelCindercallerTests`, flat motion and
  Patchbearer suites, global/fallback authority, rectangular spell projection,
  relevant coordinator/Ember audio and `SpreadSpecialistCombatTests`. Classify
  observed RED, already-passing pins and additional GREEN counters honestly.

Follow `ADVERSARIAL_TESTING.md` and `CLAUDE.md`: cold-eye symmetry/cross-feature/
counter-completeness/doc-drift review, then bounded player-flow hypotheses for
cross-system gaps. Fix concrete Yellow+ before commit. Do not inflate case counts
with duplicate runs or treat an unsupported allocation probe as zero allocation.

## Readiness, self-review and continuation

🟢 Existing encounter mechanics, neutral art, projected spell FX/audio and earned
pre-generation fixture are present. Existing capture records establish the static
body and movement, not a new current cast or animation result.

🟡 **Integration seam:** authoring frames alone cannot activate flat casting.
Test compact-asset support, current owner admission and flat OnCast/tick together.
Preserve unauthored callers and do not accidentally extend Patchbearer refresh.

🧪 **E1 is pending:** no Unity load, new route, ordinary cast, current-code image,
native test, timing measurement or package action occurred during this audit.
The old close caster save is unlocated; the retained pre-generation fixture is
the honest starting point. Native test/art success cannot close this gate.

⚪ **Scope deliberately pruned:** no ammunition depletion, spell damage changes,
new3D rig, projectile/audio redo, terrain redo, permanent fire effect or additional
species motion. Physical Deck/Linux execution, whole six-scene acceptance and
remaining regions remain distinct later gates.

Can verify now: reviewed source contracts, current static/motion mismatch,
approved PNG identity, historical actual captures and bounded earned-save prefix.
Cannot verify now: new animation readability, ordinary current caster behavior,
audio feel, performance improvement, packages or hardware acceptance.

Implementation log: source and actual-record sweep complete; independent earned
checkpoint audit and route-section review found no material false claim; root
review pending, then E1. Files changed: this
document only. No runtime/art/save/editor/Git action performed by this planning
task. Update this document in the same commit as later implementation.


## 2026-10-27 03:40 UTC — baseline established; motion deferred by user

**E1 completed in a bounded ordinary Editor encounter.** Root continued the
unchanged earned pre-generation65 fixture through the isolated launcher and
ordinary controls. Current generation disclosed Cindercaller4920 and
Scrabbler4923. The upper route reached55,3 at tick1130 before contact. Ordinary
melee defeated the partner at1220; player HP40→30. Pursuing the actual caster
showed it chasing Magpie4884. At1630, the existing Lunge dealt3 to the caster
at67,20; the player remained69,18. The same NPC turn produced genuine AI
`Pyromancy_EmberSpit` from67,20 to65,20, dealing3 to the Magpie and Burning.
The existing projected fire/contact effects were working. The caster's neutral
body remained unchanged during movement, damage and the cast; the existing
hurt color flash still occurred. No RNG, position, HP, cooldown, hostility,
AI choice, visibility, animation timing or gameplay events were forced.

Seven bounded action capture folders preserve131 actual1280×800 frame receipts.
The genuine cast is in `Verification/Live16x24/G2Action-20261027-032836-072/`.
This is selective observation, not continuous video, normal performance proof
or audio-feel acceptance. An independent reviewer inspected13 original PNGs,
all frame dimensions/receipts and the four earned checkpoint prefixes/hashes:
[baseline review](Verification/Live16x24/east-caster-baseline-independent-review.json).
The03 reader still is the target/action menu, not full F1 Examine.

Earned fixtures are preserved under `Verification/Live16x24/Fixtures/`:
`east-caster-approach65`, `east-caster-post-partner65`, `east-caster-close65`,
`east-caster-post-cast65`. Their READMEs contain the exact state and hashes.
The isolated session was exported and stopped; normal saves, gameplay
preferences,38 pre-existing tracked modifications and prior current packages
were verified unchanged in [restoration receipt](Verification/Live16x24/east-caster-baseline-restoration.json).
No storage cleanup occurred.

**E2 partially complete, then deliberately deferred.** Built-in imagegen used
the actual cast and post-cast screenshots plus the unchanged approved caster.
Prompt/concept are preserved in `ArtSource/Live16x24/concepts/` under
`east-caster-motion-*`. The gesture ideas were retained, but enlarged anatomy,
faux grid, palette drift and a replacement idle were rejected. A source-only
five-frame80×24 derivative packet and editable Blender source are preserved in
`ArtSource/Live16x24/east-cindercaller-motion/`. The author records missing-output
source RED→first-build GREEN with8 mutation counterchecks and232 preserved
inputs. Root has not approved the contact sheet; repeat-build determinism and
saved-Blender inspection remain undone. These are source-authoring results,
not imported art or gameplay acceptance.

**E3/E4 not started.** The independently reviewed39-case test draft was briefly
copied into Assets, but no native RED run occurred and no runtime implementation
was made. At the user's priority change, those exact new files were moved to
`Tools/Live16x24/DeferredTests/`, outside Unity's active test tree. No motion
atlas was imported. Current delivered game remains North source2375284dd.
Do not count this packet as completed sprite coverage or an animation milestone.

One plan correction from E1/source review: a hidden source with visible contact
can still have its resolved FX accepted and produce the existing CastCallback.
The future actor adapter must refuse disclosing that body without suppressing
valid target-side FX. The earlier table's generic hidden-rejection shorthand
must not be used as a whole-sequence visibility rule.

Next action: follow the worldwide sprite-coverage priority in the completion
plan; retain this bounded plan and source work for a later animation pass.
