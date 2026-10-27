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
In progress: bounded registered-scope matrix and test design for the four approved
equipment identities. Pending: native RED, production change, actual after
screenshots and regional sprite kits. No new runtime/gameplay result is claimed
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
