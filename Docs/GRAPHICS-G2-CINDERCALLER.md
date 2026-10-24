# Cindercaller — exact native body

2026-10-24. Status: bounded static-body integration accepted. Behavioral RED2
preceded production; final334 distinct native cases pass, zero failures/skips.
Ordinary world/Look/Examine, NPC movement and F5/F6 passed in the actual seed65
eastern wilderness. Play is stopped and owned temporary settings restored.
Animation, complete G2/G3, packaged and device acceptance remain open.

## Scope and corrected premise

The remaining body is **MarlbackCindercaller**, an NPC blueprint inheriting
MarlbackScrabbler mechanics. It is not a Player class. Preserve the accepted
weapon-neutral Player body and all its motion frames. This packet supplies one
static, full-frame 16×24 Cindercaller image in the already admitted native zones,
using `Sprites/Live16x24/cindercaller`. It does not grant casting animation,
permanent flame, a held weapon, additional stock or new combat behavior.

The visual reference is the accepted flat native style, inspired by the user's
Caves of Qud direction. No Qud mechanic or source-parity claim is made. Follow
the current actual capture → imagegen concept/parts → editable native cells →
import → native tests and ordinary world/reader acceptance pipeline. The root
captures the existing actor before art authoring; concepts are not game evidence.

## Verified source sweep

| Source | Verified contract / correction |
| --- | --- |
| `Assets/Resources/Content/Blueprints/Objects.json:41592–41692` | Exact `MarlbackCindercaller`, inherited Scrabbler, canonical glyph `g`, base color `&R`; narrow cap and scorched mantle. CombatTactics grants EmberSpit; Loadout equips Cudgel and carries FireMoss. Those are mutable belongings/abilities, not body-state art predicates. |
| `Assets/Scripts/Gameplay/Bootstrap/StartingBuildService.cs:29–85`, `Assets/Resources/Content/Data/Builds/StartingBuilds.json` | Starting builds change stats, equipment and skills plus `StartingBuild`; no Cindercaller Player class or class-body mapping exists. |
| `Assets/Scripts/Presentation/Rendering/FlatPixelActorMotionCatalog.cs:61–74` | Motion accepts Player/PetDog/exact Scrabbler/exact Gleaner. Cindercaller inheritance must not borrow Scrabbler's motion or suppress the new static body. |
| `Assets/Scripts/Presentation/Rendering/FlatPixelOpeningArtCatalog.cs:587–620` | Existing cast selection is exact, so no native Cindercaller frame is currently selected. Player's existing tag-wide body is intentional and unchanged. |
| `Assets/Scripts/Presentation/Rendering/FlatPixelIncidentalActorCatalog.cs:11–93` | Existing optional Farmer/Grazer companion supplies cached centered16×24 Tile/Sprite pairs, owns those generated objects and borrows textures. Its two-argument injection is resource-free. |
| `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs:167–183,739–837` | Existing strict fallback establishes world graph/cell/owner/disclosure before local substitution. World and exact-subject routes use `HasKnownRole` and the same companion. Admission remains the six exact graphs/current Spread/no-POI policy. Portrait semantic lookup is not document authorization. |
| `Assets/Scripts/Presentation/Rendering/SpreadBiomeHumanoidLibrary.cs:69–80` | Old 3D caster appearance checks exact identity, canonical glyph/color and explicit reskins. That model is not approved native16×24 art and must not be revived in flat mode. |
| `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationWorksites.cs:40–46,65,104–109` | Forge worksite can place this exact caster and alter ranged tactics. Do not require a generation role, current tactic setting or a particular worksite to recognize its body. |
| `Assets/Tests/EditMode/Gameplay/AI/SpreadSpecialistCombatTests.cs:8–25` | Real FireMoss/cooldown/cover behavior already has gameplay tests. Body queries must not consume, refresh or modify it. |

## Smallest implementation, after native RED

Extend `FlatPixelIncidentalActorCatalog` with an independent optional third
texture and cached frame. Default construction loads all three Resources once.
Keep `(Texture2D farmer, Texture2D grazer)` deliberately equivalent to the new
three-argument injection with a null Cindercaller texture. No extra positional
opening-atlas argument, renderer, world scan or observer is necessary. Existing
Greybox routes will recognize the added exact role through their current lazy
companion seam; change them only if a concrete routing gap appears.

Eligibility reuses owned visible Render, owned non-takeable uncustodied Physics,
owned Body/Brain, Creature and non-Player/non-Item, canonical `g`, no explicit
VisualID/VisualVariant/GlyphVariants and single current-cell geometry. A neutral
body must survive low HP, changed mood/name, depleted FireMoss, unequipped Cudgel
and a cooling spell. No mandatory held item or permanently flaming body is
truthful. Existing Farmer/Grazer semantics and textures remain unchanged.

Missing/malformed Cindercaller texture leaves Farmer/Grazer working; missing
either older texture leaves Cindercaller working. Disposal destroys generated
Tiles/Sprites only. Repeated queries return the same cached objects without
mutating any owner, AI, inventory, timing or graphics resource.

## Verification sequence

1. First two existing-API tests use an actual factory Cindercaller in an admitted
   current east graph. World must report native foreground; exact-subject
   semantic lookup must stop returning its cached glyph. Both assert the actual
   imported Cindercaller texture only after their behavioral assertion. A real
   authorized document is checked as a precondition for the subject test; full
   `ExaminePortraitRenderer`/submitted-world lifecycle coverage follows with the
   counter suite. This first fixture does not itself claim those later layers.
2. After root records native RED, implement the independent optional frame and
   add source-backed counters: missing/malformed peer groups, old two-argument
   injection, exact identity versus Scrabbler/Soursprayer/unknown, explicit
   reskins/glyph, foreign structural parts, custody, hidden/current-graph/cell
   authority, borrowed disposal, stable cache, and mutable neutral-state controls.
3. Exercise default actual resource plus submitted world and authorized examine;
   preserve accepted Player/Farmer/Grazer/Scrabbler controls. Body selection must
   never give Cindercaller any of the four existing motion identities.
4. Root ordinary Play must inspect the existing caster in world and exact reader,
   compare the source image and verify cleanup. A receipt's FOV flag alone does
   not prove that the owner lies inside the screenshot viewport. No grants,
   replacement spawns or forced AI actions are acceptance evidence.

Relevant native controls: `FlatPixelIncidentalActorTests`,
`FlatPixelActorMotionCatalogTests`, `FlatPixelGlobalPresentationTests`,
`FlatPixelFallbackCatalogTests`, `SpreadSpecialistCombatTests`, and the exact
source-contract caster/finite-supply fixtures if implementation touches any of
their authority. No gameplay change is planned.

## Separate action-feedback finding

Player hurt/guard/parry ordinary capture remains open. Existing defense words
and audio already communicate outcomes (`BladeDefense.cs:109–137`,
`CombatSystem.cs:338–343`); a full parry correctly produces no Hurt. A separate
source finding deserves a later targeted test: `LongBlades_Riposte.cs:39` calls
`PerformSingleAttack`, whereas the normal `EmitAttack` resides in outer
`PerformMeleeAttackCore` (`CombatSystem.cs:133`). Do not expand this body packet
into defense-state or direct-strike event changes.

## Initial tests-first log and self-review (historical checkpoint)

- Source sweep completed before tests. No production, art or generation edit.
- Initial fixture: `CavesOfOoo.Tests.FlatPixelCindercallerTests` (two tests).
- Initial native import found a fixture compile error: `AnnouncementDocument.Subject`
  is internal (`Gameplay/Events/AnnouncementDocument.cs:40`) and inaccessible to
  the test assembly. The source review missed that access boundary. The test now
  passes the same known `owner` used to build and validate the document, as the
  existing incidental-actor semantic tests do. The failed/zero-case invocation
  was not behavioral RED; the corrected two-case native run was still required
  at that checkpoint and is recorded below.
- 🧪 Native RED and actual resource/visual acceptance were pending. Static source
  review is not compilation or gameplay evidence.
- 🔵 Scope preserves existing body art, local admission, graph authority and
  mutable combat state. Independent fixture/code review follows the native gate.

Files in this initial step: this plan, new
`Assets/Tests/EditMode/Presentation/Rendering/FlatPixelCindercallerTests.cs` and
its fresh two-line `.meta`.

## Implementation checkpoint — 2026-10-24 22:30 UTC RED

Root recorded **2/2 actual behavioral failures**, zero passes, in
`Verification/Live16x24/cindercaller-red.xml` and its summary at22:30:52Z.
Both tests reached the intended world/subject glyph assertions before resource
checks. The earlier inaccessible internal `Subject` compile error/zero-case
invocation remains excluded from RED evidence.

The only runtime change is the existing `FlatPixelIncidentalActorCatalog`:
independent optional third resource, exact known role, cached neutral frame,
and matching owned Sprite/Tile disposal. Its older two-texture constructor
forwards a null third dependency and never loads the new resource. The default
constructor loads it once. Existing Greybox world and subject routes require
no edits because they already select this optional companion by exact known
role inside native admission. Player, motion, generation and combat are
unchanged.

The fixture is now **45 source-counted cases**, including the initial two.
Additional counters characterize the new slice after the initial behavioral
RED; they are not claimed as separately observed failures. They include
independent/malformed dependencies, cached object identity/borrowed lifetime,
exact role and reskin refusal, foreign/missing parts/custody/footprints,
outside/hidden/stale ownership, submitted world and actual authorized reader
retirement, and real movement restoring the existing Bed without full repaint.
Real reagent removal and Cudgel unequip preserve the neutral body; repeated
queries preserve a deliberately active EmberSpit cooldown. The default import
check covers Cindercaller and both earlier peers.

No allocation-counter claim is added: the prior kitchen calibration established
that this Unity runtime's `GC.GetAllocatedBytesForCurrentThread` can return zero
for retained allocations. This slice uses existing bounded cached selection,
no new scan/closure/list in the hot path, and tests object reuse/state purity;
that source evidence is not a measured zero-allocation result.

Root has the new native art source and performs import/visual review separately.
This checkpoint does not claim native GREEN, actual renderer pixels, ordinary
NPC movement, casting animation, save persistence or screenshot acceptance.

## Focused native GREEN — 2026-10-24 22:35 UTC

Root ran the expanded fixture from22:35:31Z to22:35:34Z:
**45 passed, zero failed, zero skipped**. Raw evidence:
[`cindercaller-green.xml`](Verification/Live16x24/cindercaller-green.xml) and
[`cindercaller-green-summary.json`](Verification/Live16x24/cindercaller-green-summary.json).
The initial two failures are behavioral RED; the subsequent43 counter cases
are additional GREEN coverage, not independently demonstrated REDs.

Independent source reviewer
`/root/projection_cold_review/look_focus_draft_review` found no actionable
P1/P2 in the final runtime/fixture. The review explicitly rechecked public API
accessibility after the initial missed internal `Subject` access. Optional
resource isolation, exact identity, borrowed lifetime, real move/unequip/stock
operations, cooldown purity, world/reader authority and fixture cleanup were
reviewed. See the bounded
[runtime review record](Verification/Live16x24/cindercaller-runtime-review.md).

🧪 This proves the native script-observable frame binding, submitted Tilemap
and authorized-reader contracts exercised by the fixture. It does not prove
ordinary combat/movement readability, actual screenshot appearance, save/load
continuity, a cast animation or performance/hardware targets. Root is running
the affected regressions and owns the next ordinary checkpoint replay.


## Actual integration and final review — 2026-10-24 22:43 UTC

The actual baseline is `Verification/Live16x24/cindercaller-baseline-world.png`
(seed65, east graph, tick5250). Imagegen used that capture plus the approved cast
contact. Exact prompt, concept and critique live in
`ArtSource/Live16x24/cindercaller/`. The concept's narrow cap, scorched mantle and
open limb gaps informed original native Blender cell geometry; soft shading,
inconsistent pixels, oversized placement and duplicate-looking legs were rejected.
The production frame is16×24,119 opaque cells,7 approved family colors; all384
source pixels and native contact blocks verify. All65 preserved art hashes match.
Editable `.blend`, source-cells JSON, generator and manifest are included.

The unchanged ordinary checkpoint was continued with no forced art mode, no
spawn replacement, teleport, grants, AI edits or forced actions. Player2696 began
at55,23 with40HP; real Cindercaller4983 was78,7 with12HP. Keyboard Look moved the
cursor to78,7, Return opened its actual action menu, and X opened Examine. The
actual full1280×800 `cindercaller-native-world.png` and `-reader.png` show the same
new body in the world and portrait, including selection outline and real prose.
Reading did not advance tick5250 or change any recorded owner/inventory field.
The Editor panel was displayed at2× and cropped its own edges; the stored native
Game captures are full1280×800. No game-camera zoom was changed for this pass.

After closing the menus, ordinary held-Right input moved Player six cells to61,23,
tick5310. The Scrabbler approached62,23 and dealt11 damage (40→29HP); the
Cindercaller naturally changed78,7→78,5 and retained its native body, leaving no
body at its old cell. `cindercaller-native-moved.png` records that actual state.
F5 saved and F6 reloaded it. `cindercaller-native-saved/reloaded` PNG/JSON pairs
and `cindercaller-save-comparison.json` agree on all6 recorded actor rows and18
inventory rows, plus seed,zone,tick,Player position/HP and flat presentation.
This is a bounded recorded-field comparison, not a complete save-format audit.
The actual save/checkpoints are retained separately outside the repository.

Independent [source review](Verification/Live16x24/cindercaller-source-review.md),
[runtime review](Verification/Live16x24/cindercaller-runtime-review.md) and
[gameplay-image review](Verification/Live16x24/cindercaller-gameplay-review.md)
found no significant defect in this scope. The compact observation source is
preserved as `cindercaller-observer.cs.txt`; it reads actual submitted Tilemap
frames, not a selector return presented as gameplay. Player/Scrabbler use separate
motion bodies, so null overlay entries for those two are expected.

### Native verification and cost

Final45 focused +289 affected regression cases are **334 distinct passes**,0fail,
0skip, confirmed by unique full test names. Native XML/job/summary evidence and
`cindercaller-final-test-summary.json` are included. The initial inaccessible
internal Subject fixture compile/zero-case attempt is retained and excluded
from behavioral RED. The corrected original2 cases failed as intended before
production. The additional43 counters are final GREEN coverage, not separately
observed REDs. This is a selected native sweep, not the entire project suite.

Actual Editor profile (`cindercaller-profile.json`) covers250.934 seconds and
205844frames, including idle, tools, reader, movement and save/load. All five
131072-sample rings wrapped. Retained renderer samples: mean0.05664ms,
p99 0.11225ms,max41.66725ms; Main Thread max1041.51183ms; max45 draw calls/batches.
These are retained-window Editor samples, not a full-window timing, zero-GC
measurement, controlled before/after speedup, or Steam Deck result.

`cindercaller-native-cleanup.json` confirms stopped Play, isolation false,
view/launch flags false and null SaveRootOverride. Prior scene, presentation
preference, seed and Play-start setting were restored by the owned helper.
`cindercaller-console.json` contains only the two existing Metal memoryless-depth
load/store diagnostics. The MCP labels them Error although the visible Editor
console showed0 red errors/2 warnings; preserve the raw discrepancy rather than
reporting an empty console. No new script/exception diagnostic was found.
Twenty-one importer files changed trailing whitespace during Unity import;
byte comparisons proved no semantic change and restored their exact HEAD bytes.

### Final self-review and boundaries

- 🟡 Resolved: confusing this NPC with a Player class would redesign already
  approved art. The source sweep corrected that before production.
- 🟡 Resolved: inherited Scrabbler motion would suppress this body. Exact role
  tests preserve the static identity without fabricating an animation.
- 🟡 Resolved: the initial fixture accessed an internal Subject. Public owner
  handling compiled and the actual corrected2-test RED was recorded.
- 🔵 Body is deliberately neutral to mutable equipment/reagents/cooldown/HP.
  Existing visibility, custody, graph and explicit reskin authority are retained.
- 🧪 This does not add Cindercaller movement/cast/hurt/death animations, measure
  physical Deck performance, complete all owner coverage or ship an updated build.
- 🧪 Main G2/G3, full crop/forge lifecycle, six-zone journey and packaged/device
  gates remain open. Current October11 Deck archive is preserved and still old.

**Can verify:** actual world/reader native appearance, selected natural movement,
selection, bounded save/reload continuity, native contract counters and source
preservation. **Cannot verify from this pass:** complete combat feel, every save
field, whole-map native art, new animation, physical controller/Deck behavior or
release-wide visual acceptance.

**Next:** use the [Riposte action-source audit](Verification/Live16x24/riposte-action-source-audit.md)
to close a concrete action-feedback seam: direct counterstrikes currently bypass
normal Attack visuals. Keep that behavioral fix separately tested/reviewable;
preserve existing Player body and full-parry/no-Hurt semantics. Continue the
complete crop/forge journey and package gates afterward. The graphics heartbeat
remains active; this static actor is one completed gap, not the completed overhaul.

Files: optional catalog connection;45-case fixture+fresh meta; native PNG+meta;
editable native art/generator/manifest/prompt/concept/source checks; actual before/
after screenshots, raw native evidence, bounded observations/reviews/profile;
this plan, main completion plan, live handoff and release ledger. Gameplay,
blueprints, admission and old presentation are unchanged.


Post-push/storage: `c76654faa17c0162bb6390007951ff37b14a657f` was verified on
origin/main with all59owned blobs matching. The independent
[post-push audit](Verification/Live16x24/cindercaller-storage-audit.md) confirms
source/saves/current archive and both unrelated local Notice variants preserved.
About1.71GiB is free; no worthwhile confirmed stale cache/build candidate remains,
so **0bytes were deleted**. The active Editor's useful caches are retained. The
current delivered archive still predates this art. Continue the next bounded
action-feedback slice; no whole-release or packaged acceptance is implied.
