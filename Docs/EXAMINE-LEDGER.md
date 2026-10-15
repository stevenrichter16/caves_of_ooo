# Examine field ledger (design C)

Status: implemented and verified for the bounded cohort below. User selected C from `Docs/Design/ExamineDialogMockups/C-field-ledger.png` and authorized implementation, Deck testing, content/art audits, image-generated art targets followed by world-art iteration. Physical Steam Deck verification remains outstanding; native simulated-controller and Deck-resolution checks are complete.

## Design and readiness

A warm charcoal reader with bronze frame, sea-green labels, ivory prose and amber subject/important state. It remains inside the gameplay column, preserves the sidebar and returns to the originating menu. Optional subject portrait comes from the same world model; empty sections collapse. Short notices including level-up use the same shell. Controller hints must match real A/B and shoulder/D-pad paging behavior.

- 🟢 Shared modal, paging, controller routing and inventory return retain their existing flow. Structured world/inventory Examine and level-up now use the common reader.
- 🟢 Immutable subject/section metadata travels with the existing announcement queue. Exact log text and document-reading audio retain their separate authority; transient subject references are not saved.
- 🟢 The builder displays current status/repair/growth readouts, authored prose and established material/composition/weight/structure facts. It omits unknown material and expired effects rather than inferring mechanics from names or artwork.
- 🟢 A census of all 712 blueprints justified 124 authored leaf-description additions. Eight world objects now have model work guided by eight separate image-generated targets: 15 refined existing variants and one new FireClay model. The targets are design images, not replacement gameplay sprites; this is a bounded cohort, not whole-game art coverage.
- 🟢 The selected 1,072 native cases are green across the final sweep plus the corrected asset-count pin rerun. Live game captures and simulated-controller checks are complete at Deck and desktop sizes; detailed limits are below.
- ⚪ This is an original CoO interface implementing the selected concept, not a claim of copied Qud source or exact parity.

## Verification sweep before production edits

| Premise | Verified source / correction |
|---|---|
| Examine is DialogueUI | False: `InputHandler.OpenWorldExamineReader` publishes to `MessageLog`, displayed by `AnnouncementUI`; NPC dialogue is separate. |
| Level-up needs another modal | False: `LevelingSystem` already uses AddAnnouncement. Upgrade its metadata and common shell. |
| Existing popup font is the polished HUD font | False: uses narrow CP437 UI glyphs; scoped `HudPixelFont` supplies normal proportions. Do not use the world glyph bank for UI text. |
| A world object view can be cloned for its portrait | Unsafe: ground-patch batching can include adjacent objects. Resolve the object's recipe/model only. |
| Examine can expose any object at remembered coordinates | False: the existing reader revalidates current visible owner, part and cell before publication. Preserve those guards. |
| Page layout can grow freely | False: 80×45 overlay, existing lower action-row clearance, cached actual footprint clearing and reading audio must survive. |
| Every material is defined | False: composition/repair/physical material are different sources. Show authored facts and state; omit unknowns. |
| A local missing Description field means the object lacks prose | False: blueprint inheritance and the Text/Description aliases must be resolved in assignment order before selecting targets. |
| Every referenced blueprint is guaranteed to spawn | False: exact-token references include placement constraints, scenarios and inactive paths. Coverage counts are static source evidence, not discovery rates. |
| A rejected inventory snapshot can fall back to its old text | False: description callbacks can remove ownership; a null snapshot must reject before publication and success diagnostics. |
| Nonoverlapping tile coordinates prove readable text | False: at 1280×800, scale-2.1 title ink overlapped adjacent glyphs by about 1.5 pixels. Measure painted glyph bounds after tile transforms. |
| A successful one-shot portrait capture leaves other rendering untouched | False: a native RED reproduced the portrait RenderTexture remaining active. Capture must restore the previous render target, including failure paths. |

## Milestones and acceptance

1. **Structured content and queue.** Explicit immutable document, sections/facts and optional blueprint identity; current world/inventory ownership and hazard priority; legacy plain-text queue and audio retain behavior. Red tests → implementation → counterchecks.
2. **C reader shell.** Normal pixel font, bronze frame and rules, scoped colors, stable title, optional portrait, inside-frame footer and bounded pagination. Generic notices/level-up/long books use the same renderer. Test shrink/reopen/close clearing and every page's bounds.
3. **Content coverage.** Inventory concrete leaf descriptions and live material/condition sources. Surgically add specific lore to justified gaps, validate parsed object delta and no mechanics changes.
4. **Target-first art.** Save separate imagegen targets and prompts for selected reachable objects, iterate their world models, use those models for reader portraits. Audit fit, silhouette, state variants and render-resource lifetime. Publish exact coverage ledger and remaining scope.
5. **Native verification and polish.** EditMode regression sweep; isolated Play at 1280×800, 1280×720 and desktop. Exercise well/material/creature/crop/equipment, level-up, multi-page document, menu return and simulated native controller. Compare screenshots to C; fix notable review findings.
6. **Review and delivery.** Cold-eye integration, adversarial hidden/stale targets, long text, missing art, queue chaining and state changes. Update this document in the implementation commit, fetch/rebase and push main.

## Evidence and limits

Evidence is stored under `Docs/Verification/ExamineLedger/`. Native editor tests prove observed code/render behavior; simulated gamepad input and Deck-size captures do not prove physical Steam Deck ergonomics or Linux GPU behavior. No new Deck build was requested or produced in this turn.

## Implemented behavior

`AnnouncementDocument` copies its sections and facts into immutable collections.
World reads require the exact visible, explored occupied cell and current physical
owner; inventory reads require actual carried/equipped ownership. Description
callbacks are followed by a fresh ownership/part check. Portrait access separately
revalidates the original subject, selected cell and viewer when the reader opens.
It does not refresh prose or execute actions during rendering.

Current warnings and state precede flavor text. The builder includes live effects,
repair/growth/ground information, existing item mechanics, enhancements and
directions when their providers return content. Facts report only established
material/composition, weight, stack totals, structure and valid repair supplies.
Repair supplies come from the existing recipe and compatible current repair state;
the UI invents neither composition nor a new repair action.

The reader uses normal-proportion HUD glyphs, bronze borders, charcoal fill,
sea-green labels, ivory prose and an amber title. Description prose has no redundant
DESCRIPTION heading. Facts use separate label/value columns. The 80×45 overlay
holds at most 25 body rows inside a 37-row panel, preserving the lower inventory
action area. Two title rows have extra vertical spacing; title text uses a
1.25-cell advance and a scale constrained by the actual cell width. Long names
remain fully available in the body. Footer hit regions match their keyboard and
controller labels; shrinking, reopening and closing clear the previously painted
foreground and background footprint.

The portrait resolves the subject's individual world recipe/prefab, never a
combined neighboring ground patch. It captures on demand into one point-filtered
192×192 target with owned material copies and releases its graphics resources on
clear. Supported carried/equipped items reuse their portable floor-model recipes
through the current owning viewer's presentation context, without moving the
item into a zone. Repair supplies and biome crop seeds/produce work in admitted
contexts; ordinary portable goods currently use models in Spread/Sodden contexts.
Unsupported current appearance or model/context retains the current sprite/glyph
fallback and tint. Rejected subject ownership clears the portrait. There is no
per-frame capture loop or multi-subject texture cache.

A bounded static census identifies **288 authored blueprints with declared
carried mappings** in `Objects.json` (291 identities including three special names), and 411 unique identities (407 in `Objects.json`) across the
base SpawnRing bindings, portable dispatch, repair/cultivation and biome crop
lists. These counts exclude other world recipe extensions and do not establish
spawning, current visibility or successful rendering. `SoddenFieldDressing` and
`ToastedEmberwheat` remain carried fallback examples; actor portraits omit
separately attached equipment. Exact sources, count boundaries and scope limits:
[portrait-coverage.md](Verification/ExamineLedger/portrait-coverage.md).

## Coverage ledger

### Description scope

**124 of 712 blueprints** receive new authored leaf descriptions; no existing
description or mechanical field is replaced. Parsed comparison proves 124 changed
objects and **588 unchanged objects**. Among 704 named blueprints, authored prose
coverage rises from 445 to 569; **135 named entries still lack authored prose**.
That remaining set includes NPCs, spellbooks, runtime templates and special pools
whose need for static prose must be reviewed individually. Dynamic mechanical
readouts are not counted as authored prose.

The new cohort contains 25 terrain entries, 16 workstations/containers, 27 crafting
and gathering materials, 10 plants/food, 19 weapons, 14 tonics and 13 hazards/lights.
Every addition has a concrete content source reference. Exact-token referenced
entries without prose fall from 198 to 74, but these references do not prove live
generation, acquisition frequency or an unchanged-save migration.

The prose review used the current `Lore` canon and mystery restrictions. It
narrowed steel-blade and leather-binding repair claims to supported gear families,
removed an implied manufactured origin for ChoirSpine, and kept PalimpsestBlade's
decoration ordinary and ambiguous. Sporeblade prose does not promise spore
emission. No material/composition fields were added. See the full manifest,
source references and parsed receipts in
[the description audit](Verification/ExamineDescriptions/description-audit-notes.md).

### Art scope

Eight individual imagegen targets precede the corresponding art work; seven
objects initially had world refinements; native review added FireClay as the eighth:

| Object | Refinement |
|---|---|
| RepairLinedWell | Closed masonry courses and exposed clay lining; damaged gap remains distinct. |
| RepairRopeWell | Masonry courses, winding loops and distinct broken/restored rope and bucket. |
| RepairWoodenGate | Metal hinge studs across broken, closed and open states; existing leaf motion retained. |
| KnotflaxCrop | Mature pale fiber tassels and broader knot. |
| HearthbulbCrop | Stepped exposed bulb and upright split leaves at maturity. |
| SeamleafCrop | Upright leaf, pale seam and geometric cross stitches at maturity. |
| SaltCuredBody | Salt crystals and wrist tally cord; original poses and case identities preserved. |
| FireClay | Compact ochre clay chunks replace the misleading crystal silhouette; one small model uses the existing repair palette. |

The two source packs contain exactly **15 changed model variants, one added FireClay model and 42 unchanged
models**, with no palette changes; the largest model has 92 cuboids,
within the existing 100-cuboid limit. Target-image glow is not converted into
invented game light/emission. Source constraints and Blender previews are not
Unity screenshots. The final import receipts, `Verification/ExamineLedger/repair-art-final-import.json`
and `Verification/ExamineLedger/curation-art-final-import.json`, cover the complete
38-model repair/cultivation pack and 20-model Curation pack. Their source hashes
match `world-art-diff.json`; they do not mean all 58 models were redesigned.
The earlier `*-art-import.json` receipts precede FireClay and the body-occlusion
correction and are retained as intermediate evidence.
See [the art cohort and limitations](../ArtSource/ExamineLedger/README.md).

## Review findings and evidence checkpoints

| Receipt / gate | Observed result | Interpretation |
|---|---|---|
| `ExamineDescriptions/authored-red.json` | 124 intended missing texts | Content RED before surgical data edits. |
| `ExamineDescriptions/authored-green.json` | 124 authored checks pass; 124 prose-only changes, 588 unchanged | Offline parsed proof; not a native spawn census. |
| `ExamineDescriptions/tool-counterchecks.stdout.txt` | Seven unittest methods pass | Inheritance/alias/cycle controls and forbidden-change rejection. |
| `ExamineLedger/initial-red.xml` | 32 cases: 2 pass, 30 fail | Missing builder and C shell reproduced; zero-case stale-assembly attempts excluded. |
| `ExamineLedger/first-pass.xml` | 80 cases: 56 pass, 24 fail | Queue/footer RED plus content callback/ownership findings and fixture corrections. |
| `ExamineLedger/second-pass.xml` | 168 cases: 143 pass, 25 fail | Includes nine inventory publication failures with three unchanged-owner controls passing. |
| `ExamineLedger/third-pass.xml` | 38 cases: 36 pass, 2 fail | Inventory 12/12 GREEN; typography 5/6 with expected Deck collision. Portrait test also exposed an incomplete fixture before its behavioral RED. |
| `ExamineLedger/render-state-red.xml` | 7 cases: 6 pass, 1 fail | Typography 6/6 GREEN; active-render-target leak reproduced in portrait capture. |
| `ExamineLedger/broad-pass.xml` | 567 cases: 565 pass, 2 fail | Broader findings described below; both subsequently pass in `polish-red.xml`. |
| `ExamineLedger/polish-red.xml` | 341 cases: 310 pass, 31 fail | New carried portraits, level-up facts, reader spacing and FireClay expectations confirmed RED. |
| `ExamineLedger/final-sweep.xml` | 1,072 cases: 1,071 pass, 1 fail | Only the old repair-kit size pin fails: 37 expected, 38 after the intentional FireClay addition. |
| `ExamineLedger/final-pin.xml` | 1 case, 1 pass | The exact count pin now expects 38. All selected current cases are green across sweep plus rerun; the original sweep itself had one failure. |
| `ArtSource/ExamineLedger/world-art-green.txt` | 19 source tests pass | Geometry/state/source constraints; native visibility and gameplay reachability remain separate. |

The nine inventory failures were a caller bug: `BuildInventory` rejected stale
subjects but `InventoryUI` still published their legacy text and recorded success.
Both ordinary/tonic and material paths now reject null before publication, retain
the action popup, show a failure and emit the existing rejection diagnostic. All
12 cases subsequently passed natively, including unchanged ownership, no
consumption and no-turn controls.

The broader pass found one additional real readout bug: clearing a HealingTonic's
payload still left a blueprint-only hint promising companion healing, which made
an unsupported item appear inspectable. Healing hints now require the current
owned tonic's nonempty healing payload; paired HealingTonic/KnitmossPad tests also
check that restoring the payload restores the hint. The other broad failure was
an incomplete selected-creature fixture: the frog and scenery had IDs and visible
Render parts but no Physics part. The fixture now supplies physical parts when
missing; production ownership guards and selected-owner assertions are unchanged.
Both corrections passed in `polish-red.xml`: its 31 failures are the newly requested polish/portrait expectations, not those earlier regressions.

Other review corrections include skipping duration-zero effects while retaining
indefinite effects, rejecting foreign description parts, tolerating render removal
during description evaluation without publication, and authorizing portraits
against the captured viewer/cell rather than a replacement location. Footer
previous/next/back regions were separated after explicit behavioral RED. A final
cold source review found no further actionable queue/input integration issue;
this is bounded review evidence, not a bug-free guarantee. Detailed independent
notes: [review-notes.md](Verification/ExamineLedger/review-notes.md).

### Native visual review and final polish

The first real 1280×800 screenshots exercised wells, a gate, three ripe crops, fire clay, a creature, an NPC, a chest, a sword and level-up. A separate visit to generated Marrowstye examined the actual named, receiving-index-owned preserved body. The artificial showcase body correctly retained its fallback rather than borrowing that authored case identity.

Visual review identified a wrapped 14-character fact label and cramped section-heading spacing. The label is now “Repair”; visible headings gain a blank row, while plain prose does not. Separate native RED covers both changes. The showcase now synchronizes its directly authored mature crop stage to the crop’s real glyph/color; normal gameplay already does so.

Fire clay’s old source was a crystal cluster. Its own earlier imagegen target guided a compact 19-cuboid clay replacement. The large legacy portable source pack remains intact. Two buried salt-body hand blocks were removed after an occlusion regression; they are not claimed as visible improvements.

The final polish RED run contains 341 cases: 310 passed, 31 expected failures (17 inventory portraits, 11 level-up facts, one heading-spacing, one repair-label and one FireClay route expectation). A stale-assembly run following an incidental test compile error was discarded. Level-up now shows only actual grants/current totals, with missing-stat, callback removal, death, caps and multi-level snapshot counterchecks.

## Final native and live acceptance

- [x] Final combined native regression receipt and review of any failures: 1,071/1,072 plus corrected pin 1/1.
- [x] Live 1280×800, 1280×720 and 1920×1080 captures compared with selected design C.
- [x] Native portrait/render-state verification across supported model and fallback cases.
- [x] World and inventory reader paths, multi-page reading, level-up and return-to-menu checks.
- [x] Simulated native controller paging/close behavior and no-turn observations.
- [x] Final scope report and cold source/visual review; delivery follows the authorized fetch/rebase/push workflow.

The [final native review](Verification/ExamineLedger/final-live-review.md) links each capture to its observation and separates script/render evidence from unverified Deck hardware behavior. B restored InventoryOpen, all RB/LB/A/B paging remained at tick 10, the portrait did not recapture between frames, and closing released its owned target. Private-save cleanup completed with code 0; Unity returned to Edit mode with the original GameView configuration.

## Implementation log

- Planning: verified the shared reader and selected C artwork; split content, authored-description and art audits so all can proceed without competing for Unity.

- Native initial RED: 32 cases, 30 expected failures (29 missing builder, one missing bronze/HUD-font shell), two existing behavior controls already green. Two early zero-case stale-assembly runs were discarded; evidence only counts the imported test assembly.
- First combined pass: 80 cases, 56 passed/24 failed. Six queue/level-up metadata tests and one explicit pointer-footer test intentionally red before implementation. Content tests exposed stale duration-zero effects, foreign description ownership and render removal during a description callback; guards were fixed. Several hand-built test actors were missing IDs; fixture IDs were corrected without weakening production ownership checks.
- 124 authored leaf descriptions added; parsed receipt proves exactly those Description fields changed, 588 other blueprints unchanged. Missing named prose: 259 to 135. See `Verification/ExamineDescriptions/description-audit-notes.md` and source-reference caveats there.
- Reader now uses the C palette, larger amber subject, normal-proportion HUD glyphs, explicit section labels, narrow factual columns, and an opaque in-frame footer. Capacity is 25 text rows so the expanded header and footer still clear inventory action rows on the 80×45 overlay. Old 33-row/glyph-placement test pins are updated for this intentional layout change; full-text/paging/no-turn/return invariants remain.
