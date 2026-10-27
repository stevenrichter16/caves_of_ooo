# Worldwide native sprite coverage

2026-10-27. **Active priority; initial source audit, not completed coverage.**
The user explicitly asks for sprites for everything across all biomes and areas
before nuanced animation polish. The scheduled continuation and repository
instructions now encode that order. The six opening scenes remain the accepted
style/integration anchor; they do not define the final scope.

## Work order

1. Keep normal flat16×24 launch, travel, visibility and essential interactions
   intact. Repair delivery gaps that prevent already-approved identities from
   displaying outside their original area.
2. Fill missing shared sprite families and unrepresented biome/area kits,
   starting with high-frequency terrain, flora, creatures/NPCs, equipment/items,
   architecture and functional props. Use actual generator/content evidence.
3. Complete gameplay-essential visual variants: harvested/depleted, damaged,
   opened/closed, repaired/broken and other mechanically real states. Distinguish
   these from optional action poses or decorative motion.
4. Verify coherent whole areas and transitions, world/inventory/examine identity,
   real interactions, save/revisit and packaged delivery. Keep actual screenshots
   and bounded test/profile receipts with each accepted slice.
5. Return to nuanced animation once broad native sprite coverage is filled,
   except where a demonstrated motion defect blocks gameplay understanding.

Avoid both extremes: no isolated asset accumulation without integration, and
no repeated opening polish while other regions remain unrepresented. A shared
sprite is valid only when it truthfully represents the owner's identity. A
fallback glyph keeps play possible; it does not close a native sprite gap.

## Coverage accounting

Inventory the actual registered biomes, area/generator families and renderable
content. Separate the following evidence rather than assigning a completion
percentage from PNG/frame totals:

| Dimension | Required evidence |
| --- | --- |
| Registered scope | Source key, real generator/registry and concrete area family. |
| Authored | Editable native source and mapped identity/state; legacy art recorded separately. |
| Integrated | Actual renderer/reader mapping, authority and optional-resource fallback tested. |
| Observed | Named ordinary game route/capture and relevant state transitions. |
| Delivered | Source commit and packaged observation, with hardware limits explicit. |
| Missing/fallback | Specific owner/VisualID/material/state using no native identity yet. |
| Not audited | No evidence sufficient to classify; never silently counted as complete. |

Abstract blueprints, inheritance aliases and runtime-created variants require
classification. Raw blueprint count is a discovery denominator, not a count of
unique sprites required. Procedural combinations must be represented by their
generator/material/state families and sampled in real play; no claim of every
possible seed or configuration follows from a representative route.

The initial source matrix will be kept in
`ArtSource/Live16x24/world-sprite-coverage.json`, linked from the existing release
ledger. The ledger remains planning evidence, not a runtime manifest or automatic
certification. Expand it as actual missing identities and states are observed.

## First delivery gap: approved equipment outside the opening

Read-only source inspection finds that global flat projection already works,
but `FlatPixelGreyboxCatalog.TryResolvePresentation` and
`TryGetPresentationSubjectSprite` restrict most native artwork to six exact
graphs. Only the four approved portable actor identities bypass local admission.
Consequently an unchanged accepted weapon/armor image can revert to CP437 when
travelling. More PNGs alone cannot repair that continuity gap.

The first bounded integration slice is exact **LongSword, LeatherArmor, Hatchet
and Cudgel** in current authoritative world graphs and authorized inventory/
examine views. Reuse their existing accepted native images unchanged. The
incidental-equipment catalog already recognizes the latter three exact owners;
LongSword has an existing owned semantic resolver. Apply the same policy to
both world and subject routes. Keep local ground, terrain, walls, services,
other actor identities and regional state rules scoped as before.

### Verification sweep and corrections before implementation

| Premise | Observed source / correction |
| --- | --- |
| New sprites alone solve worldwide coverage | The local gate at the two GreyboxCatalog presentation methods prevents reuse outside six graphs. Correct identity delivery is part of coverage. |
| Global native sprites require every zone to be admitted | `FlatPixelFallbackCatalog.IsCurrentGraph` and existing disclosed-owner/custody routes already validate global contexts. Do not make `IsAdmitted` universally true or paint distant terrain with Spread art. |
| Any weapon may reuse LongSword/Cudgel | Current exact blueprint and owned semantic checks exclude inherited/new/reskinned identities. Preserve them. |
| Item poses depend on damage, temper or name | Current neutral item sprites intentionally preserve base geometry through those changes. Use existing semantics; no balance, inventory or save-schema change. |
| Subject sprite selection authorizes disclosure | The public subject selector explicitly relies on its authorized document caller. Test the real reader path for custody/stale-owner rejection, not a fabricated direct-call authority guarantee. |
| Reuse needs a new concept or redraw | These four images already passed the concept/native pipeline and are accepted. Preserve them; compare actual regional before/after rather than regenerating stable art. |

Proposed runtime changes remain limited to the two native-art policy paths and,
if needed, a small exact LongSword resolver to avoid admitting other opening art.
Write/run meaningful native RED before runtime edits. Cover real full/dirty
rendering, shared world/reader images in local and nonlocal/other-biome/underground
contexts; counter unknown equipment, explicit visual overrides, invalid ownership,
fog/memory, stale graph, custody and missing resources. No new animation work.

Actual ordinary before/after travel, inventory/examine and save/revisit remain
required. Source tests are not gameplay acceptance. New regional artwork then
follows real target-area capture → references/imagegen → parts/state mapping →
editable native production → actual integration/playtest. Root owns Unity.

## Deferred work preserved

The East Cindercaller ordinary baseline and source-only motion study are preserved
in `Docs/GRAPHICS-EAST-CASTER-MOTION-PLAN.md`. There is no imported motion atlas,
new runtime adapter or executed native motion test. The saved draft is outside
Assets. Do not resume that optional motion ahead of worldwide sprite coverage.

## Current continuation

Completed: priority persisted in scheduled prompt and repository; source-only
motion work stopped and preserved; first global identity gating issue found.
Completed: registered-scope matrix and actual Sill before captures. In progress:
portable equipment integration,222nativeGREEN and actual LongSword travel/reader/
save-restart checks completed. Sill static envelope is next; regional coverage
and packaged delivery remain pending. No new runtime/gameplay result is claimed
by this plan. Normal saves, existing builds and unrelated work are preserved;
storage remains inspection-only.


## Chunk fullness and composition — user requirement, 2026-10-26 local time

Chunks must not feel too bare. Assess the actual whole gameplay view at normal
zoom for biome-appropriate terrain variation, coherent vegetation clusters,
environmental objects, architecture and recognizable landmarks. Balance denser
patches with quiet paths, foreground landmarks with open tactical space, and
regional character with readable hazards and interactions. Use actual owners
and truthful environmental decoration; do not imply services/loot that do not
exist or scatter collision blockers merely to fill the screen. Avoid uniformly
carpeting every cell with texture or props. Deliberately sparse places such as
Overwrit still need intentional composition and an explicit visual-quality
review; their lore is not a blanket excuse for unfinished-looking chunks.

Each regional kit requires a whole-scene fullness/composition verdict alongside
identity and state coverage. Capture ordinary entry, a useful interior route
and a transition to adjacent terrain where applicable. Name any bare stretches,
repetition or obstructed paths and address the highest-impact issue. This is a
visual acceptance requirement; no fixed prop density or untested generation
change is authorized as an automatic solution.

## Portable equipment integration record — 2026-10-27 UTC

- Baseline: ordinary world-map travel from earned seed65 glade to Sill,
  Overworld.10.10.0; player39,12 HP40. Inventory/F1 shows LongSword as a
  fallback slash. Drop, south step and F5 preserve a comparable ground checkpoint.
- Actual captures: `portable-equipment-before-01-sill.png` through
  `portable-equipment-before-04-sword-ground.png` under the verification folder.
  Finite onscreen receipt identifies120 disclosed cells: one native Player,
  fallback Grass80, StoneWall15, VineWall17 and seven other owners. This is
  observed local coverage, not the full Sill census. The receipt is later at39,13
  with a2321×1139 editor viewport; the earlier screenshot is39,12 at1280×800.
  Do not use these as an exact numerical before/after pair. Capture a fresh
  aligned receipt/frame for later comparison. Fog is not a bare terrain defect.
- Native RED:48 cases,31 passed/17 failed. Sixteen failures demonstrate the
  approved item travel/portrait identity gap. The remaining test reused a stale
  AlchemyStill tile when checking HealingTonic; explicit dirtying between changed
  owners corrects that fixture while retaining the regional exclusion invariant.
  Raw RED XML is retained.
- Fixture correction: the first import used an internal runtime API unavailable
  to the test assembly; replaced with public graph reference assertions. A
  premature run executed the previously compiled29 cases and is excluded from
  new-change acceptance. The48-case RED ran after successful compilation.
- Scope: four existing approved item images unchanged; no new regional art,
  animation, combat balance, save-schema or generation changes.

- First GREEN:133/133 (Global50, IncidentalEquipment48, LongSwordAdversarial35).
  Portrait/opening regression87/87. MCP startup watchdog timed out while the
  native runner continued; retained completed Unity XML, not failed job metadata,
  establishes these results.
- Independent review identified a malformed enlarged/stale LongSword footprint
  gap in the new nonlocal helper. Add two genuine RED world/carried/reader
  counters before applying the single-cell/current-registration guard. Do not
  broaden local artwork behavior while correcting this new route.

- Final native GREEN222/222 after the footprint correction. The exact changed
  behavior and existing equipment/opening/reader suites are retained in
  `portable-equipment-native-final.xml`. Independent review closed the guard
  finding; no remaining significant issue in this bounded route.
- Ordinary Play: load earned Sill490 checkpoint, actual LongSword on39,12 now
  paints native; step north and G pickup auto-equips it. Inventory/F1 shows the
  same approved portrait. Travel via world map to glade and back, F5 at590,
  restart isolated launcher with unchanged saved pair and inspect again.
  The first reload screenshot captured an incomplete UI frame; retain it as
  transition evidence, not acceptance. Fresh settled UI inspection showed the
  proper sword and text; named settled capture follows.
- Actual LongSword gameplay was exercised; LeatherArmor/Hatchet/Cudgel
  portability is covered by native tests here, not newly observed acquisitions.
  Existing packaged current builds remain unchanged; new packaged verification
  will follow the Sill envelope batch. No whole-world art completion claimed.

- Restoration verified:38pre-existing tracked differences byte-preserved, normal
  editor/standalone save trees and gameplay preferences unchanged, currentMac
  build andLinuxarchive unchanged.412GiB free; storage inspection only.
- Actual settled restart reader accepted in
  `portable-equipment-after-07-reloaded-reader-settled.png`; incomplete earlier
  capture06 is explicitly not an acceptance image.


### Sill material integration — 2026-10-27 04:43 UTC

24 new native frames cover woven VineWall connectivity, two-axis ordinary doors
(open/closed) and quiet Grass. Actual StoneFloor, StoneWall and AlchemyStill reuse
approved pixels. Exact current Sill only; all other area rows remain open.
The public renderer, dirty neighbor repaint and examine path are integrated.
Independent review found hidden competing owners suppressing the native ground;
52-case RED had50pass2fail, then the saved guard passed final136/136.
Related regression224/224 also passed (counts overlap; do not sum as360unique).

Actual Unity Play used the unchanged earned seed65 save, ordinary controls and
real actors. Both door axes closed, occupied doorway refused closure, bump opened
an east door before moving, player crossed onto actual stone, and the north door
reader used the native portrait. Natural gremlin/NPC combat left the player38HP.
F5 checkpoint1070 at67,18 was reloaded unchanged and the matched2x view retained
338visible selected cells, including120WaterPuddle glyph cells. Actual glade and
cellar counterchecks remained flat. Detailed capture corrections and source/test
records are in `Docs/GRAPHICS-SILL-ENVELOPE.md`.

Fullness is **not accepted**: coherent shells and readable passages are improved,
but grass lanes remain repetitive/underfurnished and river/services/furniture/
residents need their actual sprites. Hidden fog is not counted as empty content.
No random clutter or generation changes were made. Next is the source-supported
Sodden crossing16,7 and shelter15,7: mire, boards, peat banks and dead-tree families,
then the remaining regions. Current packages still need this frozen source batch.

## Current continuation — 2026-10-27 05:48 UTC

Static sprite coverage across all biomes/areas and non-bare chunk composition
remain the priority. New subtle animation remains deferred. Sill and portable
equipment are packaged and on main through cc130817a. The new Sodden travel kit
covers the actual shelter, crossing and peat works only: 28 editable frames plus
unchanged borrowed materials, 304 native passes, 3 inconclusive allocation probes,
13 final actual Game captures and ordinary F5/Play-restart Continue at works39,16.
Bright repeated mire and black dry-overlay backing were corrected through actual
comparison. Fullness is still incomplete: regular grass and remaining real
service/plant/actor glyphs are listed in `GRAPHICS-SODDEN-TRAVEL.md`.

Next extend the same material kit to six ordinary Sodden formations with actual
current map/POI/saved-lair authority, before adding unrelated art packets. Preserve
Copse plain water. Follow the saved ordinary plan, real baseline/after captures,
RED→GREEN and independent review. Then cover missing Sodden services and Beating,
continuing all-biome, settlement, underground and special-site inventory. Current
packages are Sill; Sodden package refresh is pending. No Linux/Deck claim. Normal
saves and unrelated work stay protected; storage is inspection-only.
Last hourly report05:25UTC;next06:25UTC.
