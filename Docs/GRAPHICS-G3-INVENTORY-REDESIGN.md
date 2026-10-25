# G3 — Inventory redesign and polish

2026-10-25 UTC. **Planned; one generated concept reviewed; no inventory
implementation, RED result or gameplay acceptance is claimed.** The user explicitly
requested redesigning and polishing inventory as part of graphics completion.
This takes priority over the planned Nella ground material pass. The separate
wide-popup backing correction remains a prerequisite integration check.

## Goal and preferred direction

Make opening the pack useful and comfortable: recognize what is equipped,
find an item, understand its immediate value and drawbacks, and choose its
real actions without fighting tiny text or reopening a reader for every fact.
Keep the approved dark green, restrained pixel lettering, native artwork and
ledger character. This is an original Caves of Ooo interface, inspired by the
user's desired Qud-like density; no Qud engine or artwork parity is claimed.

**Preferred composition: an equipment ledger with a broad pack list and a
persistent selected-item panel beneath that list.** Do not divide the screen
into three narrow equal columns. Keep the existing five modes: Equipment,
Inventory, Tinkering, Abilities and Crafting. They are distinct existing tasks,
not five new systems. Equipment and Inventory remain two focus states of the
same useful view. Other modes reuse its header, palette, row grammar, details,
action status and footer rather than retaining a visually unrelated old page.

The smallest cohesive scope is a new presentation over the current inventory
state and command paths. Do not rebuild inventory transactions, currency,
equipment, crafting, throwing, ownership or saves to obtain a better screen.
Do not enable BitLocker in normal play. Trade remains a separate interface.

## Actual baseline and measured design problems

The reviewed actual capture is
[sixsite-reader-inventory-return.png](Verification/Live16x24/sixsite-reader-inventory-return.png),
with its [receipt](Verification/Live16x24/sixsite-reader-inventory-return.json):
1280×800, seed64 earned kitchen checkpoint, tick7160,42HP,11 displayed items,
frost-quenched LongSword2699 and Buckler2700 equipped. This is an existing
gameplay baseline, not a fresh capture made for this redesign.

Observed in that image:

- Most of the left half is separated empty slot boxes. Small abbreviations
  and slash/parenthesis glyphs identify equipped items despite native artwork
  being available for some of them.
- The list compresses category rules and items into thin adjacent rows near
  the top, while the lower half is empty. Category separators dominate small
  inventories visually.
- The selected blade has no persistent decision panel. Only the footer says
  `Actions: [throw] [examine]`; detailed comparison/handling already exists in
  code but requires another screen.
- Tabs, weight and currencies share one top row. The current short numbers
  fit this capture; source permits longer resource text to intrude into tabs.
  This is a layout risk to test, not a claim of overlap in this particular shot.
- Controls and detail text occupy the bottom edge with little separation.
  Existing input-aware prompts need a deliberate area rather than more text
  appended to the same line.

Before production, root should capture the current owned checkpoint again
with ordinary inventory input, including a selected tonic, equipment, and an
item action popup. Preserve the old 1280×800 baseline for comparison.

## Verification sweep and corrections

| Assumption | Source-checked correction / consequence |
| --- | --- |
| Inventory needs to become full screen | It already occupies the80×45 main tilemap. `InputHandler.OpenInventory` pauses `ZoneRenderer` and calls `CameraFollow.SetUIView`; improve hierarchy and ownership, not just size. |
| Inventory has three tabs | `InventoryUI` has five panel constants, including Abilities and Crafting. Its class summary and `_panel` comment are stale. All five remain reachable. |
| Equipment is a fixed humanoid diagram | `InventoryScreenData.BuildEquipmentList` uses real body parts. `BuildPaperdollLayout` has fixed positions and clamps extra limbs; a new view must accommodate actual slots rather than draw a permanent eight-slot human. |
| Selected details must be invented | `InventoryUI.Clarity.cs`, `InventoryDecisionDetails`, `EquipmentComparisonService`, and `ExamineDocumentBuilder.BuildInventory` already supply handling, comparison, food, materials and exact-subject metadata. Reuse these with current-owner validation. |
| Every selected row identifies an item | Category headers, empty equipment, crafting results and container-transfer previews do not. `SelectedDecisionPresentation` deliberately keeps composite previews neutral. No borrowed item portrait for these. |
| A retained Canvas can simply copy the Skills screen's sorting order | `SkillsScreenUI.View.cs` is an existing pooled1280×800 Canvas precedent, but an opaque ScreenSpaceOverlay inventory would cover the camera-rendered Examine popup. Prefer verifying ScreenSpaceCamera on the main camera; an overlay fallback needs explicit suspension for BOTH F1 and queued announcements. |
| F1 and item-action Examine take the same path | F1 calls `OpenInventoryDetailsReader` → `OpenDecisionReaderWithPresentation` and hides existing tiles. Ordinary queued item Examine uses `TryOpenAnnouncement` and currently leaves inventory beneath it. Preserve both routes and queued notices. |
| Any texture draw preserves approved sprite colors | `FlatPixelPortraitDrawer` documents the earlier Linear-project double-brightening defect. New native thumbnails require actual GPU color/alpha checks; a plain IMGUI texture draw is not an approved substitute. |
| Searching is merely filtering the view | Search owns input first, preserves an entity selection, handles cancel/clear, and blocks destructive/read shortcuts. Preserve that ordering and48-character cap. |
| All actions are free while inventory is open | Successful paid actions transfer a pending turn exactly once through `InputHandler.HandleInventoryInput`; throwing transfers a precise item into free targeting, with payment at commitment. Keep these paths. |
| Rendering crafting choices is read-only | `BuildCraftingRows` normalizes duplicate exclusive marks and calculates previews. Do not call it from every hover/paint/frame merely to populate a new details panel. |
| Controller X can be advertised as Close everywhere | Menu X currently synthesizes `I`; an alphabet-shortcut popup can interpret that as its ninth action. B/Escape is the reliable nested cancel. Add a contextual regression before retaining/suppressing virtual letter shortcuts. |
| CP437 box characters are globally unavailable | `InventoryUI.Chrome.cs` preserves a historical warning, while the current generator authors frame glyphs and `DrawChar` uses `Cp437.Map`. Inspect the actual font output rather than enforcing a stale restriction or changing the global atlas. |

## Concrete composition at1280×800

Use24px outer margins, dark opaque backing, thin desaturated green rules,
warm ivory body text, restrained ochre titles/focus, cyan only for equipped or
informational values, and semantic red/green paired with words/signs. Retain
the current approved sprites byte-for-byte. Avoid ornamental paper texture,
gradients, bloom, bevelled cards, glossy icon badges or fabricated item art.

Suggested content rectangles are design targets, to be confirmed against the
actual mockup and rasterized text rather than blindly treated as constants:

| Region | Proposed rectangle | Purpose |
| --- | --- | --- |
| Header | `(24,20,1232,44)` | `INVENTORY` / character context at left; weight, drams and ink in their own right-aligned area. Numbers never share a tab's rectangle. |
| Modes | `(24,76,1232,40)` | Five named modes; active mode has a solid marker and contrasting rule, not color alone. |
| Equipment rail | `(24,132,312,572)` | Compact current defenses/vitals, actual body-slot rows, equipped name or explicit empty state, scrollable overflow for unusual anatomy. |
| Pack list | `(352,132,904,330)` | Stable name, quantity and weight columns; approximately12 comfortable display rows, including modest category headers. List scroll is explicit. |
| Selected details | `(352,478,904,226)` | Exact selected title, small native icon/glyph, useful current facts, existing decision summary and clear full-details affordance. |
| Action/status footer | `(24,720,1232,56)` | One contextual control legend and one distinct wrapped status/result area. Long errors cannot overwrite controls. |

At20px nominal text height with24px row advance, list items have readable
separation. Critical text should retain at least the actual painted size of
the accepted Skills/Examine UI at1280×800; validate rasterized ink, not only
the requested font size. Titles may use24–28px. Do not shrink item names to
fit. Ellipsize a list row with an explicit marker and show its complete name
in selected details. Long details remain available through the existing full
reader. Clear pagination/continuation must announce any inline clipping.

Keep quantity and weight distinct. Existing `ItemDisplay.Weight`, stack
count and value have specific meanings: label displayed weight consistently
and retain per-unit versus total weight in the details provider. Do not
invent an exact merchant sell price from base commerce value; label value as
such if shown. A compact resource header can wrap/expand at tested limits.

### Equipment and selected-item hierarchy

- Use actual slot labels and held item names; `left hand` and `right hand`
  need not be understood from position alone. An empty slot is quiet, not a
  large attention-grabbing box. Worn and wielded equipment must stay clear
  when browsing the pack.
- A compact optional actor glyph/sprite is contextual only; it must not
  replace real slot information or imply that equipment changes world art.
- Two-handed occupation and displacement use existing service results.
  Show the actual occupied slots and proposed displacement before confirmation.
- Keep all existing stats accessible. The compact rail may promote HP, AV,
  DV and relevant capacity/handling; the existing F2 current-effects reader
  remains the complete view. Do not silently drop extra statistics or show
  a bare modifier as a computed defense/parry value.
- Selected details starts with the exact full name, relevant material and
  condition facts, then the existing decision text. For a weapon, prioritize
  the actual comparison/handling; for a material, its implemented uses; for
  food/medicine, actual effects/constraints. No predictive benefits invented
  by the visual layer.
- Native icons use the current authorized subject and atlas UV. A single
  focused preview at3× integer scale (48×72 for a16×24 frame) is sufficient.
  Unknown art uses the true glyph/color. Do not open a3D capture scene for
  each row or generate placeholder thumbnails that misidentify items.

### Other modes and nested choices

Tinkering uses a broad recipe list and a clear right-side recipe/cost result,
including a truthful unavailable/no-known-recipes state. It does not grant
BitLocker. Crafting keeps separate Forge/Brew modes, real picked components,
shared craft marks, station/batch distinctions and the existing result preview.
Abilities displays the ten actual hotbar slots with occupied/empty states,
assign/move/clear behavior and current tooltip details; it must never cast by
opening or browsing.

Nested item actions, equip candidates, manual body-part choices, displacement
confirmation, modification targets and grimoire choices use one restrained
modal treatment with visible title, selected owner and cancel affordance.
Rows remain scrollable; no action disappears because its label is long or it
is later than a letter shortcut. The visible selection drives both the detail
panel and confirmation. Destructive or paid actions use existing confirmation
and validation behavior; this redesign adds no blanket approval flow.

## Implementation boundaries and preferred technical approach

Prefer a retained presentation, following the already shipping Skills screen
pooling/text pattern, while `InventoryUI` remains the state/command owner.
This is a conscious bounded presentation replacement, not a new UI framework.
Reuse its existing fields, services and public API rather than introducing a
second inventory model that can diverge from the real entities.

Suggested additions are `InventoryUI.View.cs` and a small inventory-only
layout/hit-region helper. Names may change during implementation. Use one
set of rectangles for painting and hit testing, retaining semantic item/slot
indices and entity references. Do not leave old grid hit rectangles active
under a new Canvas. Preserve existing keyboard focus/movement semantics,
including equipment choices, while deriving any changed spatial navigation
from the actually visible slot layout.

Keep one active render owner. Once the new view paints a mode, legacy
`Tilemap.ClearAllTiles`/glyph painting must not keep running underneath merely
to satisfy old representation-specific tests. Update those tests to establish
the same opaque UI/world restoration invariant using the new real painter.
Do not replace meaningful behavior assertions with private-field snapshots.

### Canvas choice: prove camera composition first

**Preferred implementation candidate: ScreenSpaceCamera bound to the actual
current gameplay camera while it is in fullscreen UI view.** This is not yet
GPU-verified. It keeps the opaque inventory on the main camera, then lets the
existing higher-depth Popup Overlay Camera paint Examine over it. The current
`GameplayRenderLayers` in `GameplayViewportLayout.cs` admits Default(0) and
World(8) to the main camera; the popup camera admits only PopupOverlay(10).
Choose an explicit existing main-visible layer for the inventory Canvas,
not PopupOverlay, and prove it is drawn once. Do not change those global masks.
`CameraFollow.SetUIView` remains the owner of full camera rect/framing. Supply
the actual render camera to screen-to-RectTransform pointer conversion; the
null-camera conversion used by Skills applies to its overlay mode, not this
camera-bound variant. Keep the Canvas plane between that camera's near/far
planes and use the real pixel rect for resize/hit tests.

| Choice | Benefit | Required proof / cost |
| --- | --- | --- |
| ScreenSpaceCamera, main camera | Preserves the current inventory-under-queued-popup relationship and avoids blanking the inventory between view transitions. | Actual URP camera order, culling layer, opaque backdrop, material/color, resize and pointer tests. Must not repaint a second legacy inventory below it. |
| ScreenSpaceOverlay plus explicit suspension | Reuses the proven Skills pooling/layout approach with fewer camera-space assumptions. | Every camera-rendered popup must suspend it; queued and F1 routes differ. Otherwise inventory hides the reader. No old-tile flash or state reset on return. |

Choose the camera-bound candidate if those focused tests and a real capture
pass. Use overlay+suspension only if a concrete camera composition problem
makes it the safer bounded implementation. Record the decision and evidence;
neither option warrants replacing all other menus.

If using ScreenSpaceOverlay as in Skills, explicitly hide/suspend the entire
inventory presentation on **both** decision-reader and queued-announcement
entry. Keep it suspended through a chained queue; restore its original mode,
selected entity, filter, scroll and nested choice only when returning to that
same inventory actor/zone. A stale context must close safely. The existing
camera and pause authority remains with InputHandler. This is a required
integration step, not a later polish item. The camera-bound candidate should
retain the existing F1 semantic hide/restore contract while allowing queued
Examine to layer above the still-painted pack. Do not switch all popup
rendering to Canvas as part of this task. A temporary legacy view fallback
may ease integration, but exactly one mode renders per frame; do not ship
duplicate drawing or silently leave the other tabs unpolished.

Use cached labels, images and row pools. Rebuild semantic data on the existing
open/action/selection/refresh points; repaints do not recompute forge/brew
previews, describe every carried item, query the whole world or capture models.
The selected detail snapshot must be validated before and after provider
callbacks, as the existing full reader already does. There is no verified
general inventory revision counter to assume; use known event/refresh paths
and revalidate ownership on activation. Warm idle frames should cause no
row rebuild, tile clear or new capture. Measure allocations only with a
working calibrated counter; this Unity Mono has previously returned zero
even for known allocations.

## Controls and behavioral contracts

Preserve keyboard I, arrows/hjkl, Enter/E, D, Tab, slash search, F1 details and
F2 current effects where their existing context permits. Preserve Menu-context
controller A=confirm, B=back, Y=Tab, LB/RB=left/right, directional navigation,
and LT+RT=F1 details. The footer must describe the current context's actual
mapping; LB/RB are not independent tab keys in a submenu. Preserve held-input
quarantine on device/context transitions. A parked pointer must not replace
keyboard/controller focus. Touch/mouse clicks use the same visible rectangles.
Current `Open()` starts in Equipment and clears search; preserve that default
unless an explicit, separately tested design decision changes it. Returning
from a reader or nested choice must preserve its prior state; a new opening
after ordinary world play is not the same transition and must not inherit a
stale item/menu merely to satisfy a generic "remember selection" rule.

Do not advertise controller X as a universal close. Test physical keyboard
letter shortcuts independently from controller-synthesized letters so pressing
X cannot accidentally execute the ninth nested item action. Fix that narrow
context leak if reproduced; do not remap every game's controls.

Search is optional for controller-only browsing. Do not claim a Deck virtual
keyboard integration unless one is actually implemented and tested; every
item/action remains reachable through scrolling and confirm/back. Maintain
the existing search cancel/selection semantics, inert category headings and
an informative no-results state.

Preserve exact-item identity through equipment changes, stack split/separate,
container transfer, throw targeting and canceled throws. A successful everyday
action charges once; rejected/stale actions charge zero and retain useful
feedback. Compare, browse, inspect, change modes and assign hotbar slots are
free. No new operation executes merely from selecting a row.

## Test-first milestones

1. **Capture and concept.** Current actual inventory, nested action and craft
   baseline; imagegen brief below; critique a single preferred full-screen
   design. Record prompts/output and extract practical rectangles/colors/art
   uses. Generated text is not evidence of supported mechanics.
2. **Geometry and presentation ownership RED.** Real painter tests at1280×800,
   1920×1080, narrow1280×720 and the observed2321×1139 window: opaque backdrop,
   title/resource/tab separation, finite row capacity, actual text ink bounds,
   unclipped controls, synchronized hit regions, resize while open, no hidden
   legacy paint, close restores correct flat world projection. Then implement
   the shared shell and Equipment/Inventory view.
3. **Selections and all existing modes.** RED for selected full-name/details,
   current icon authority, empty/unusual anatomy, long names/large counts,
   scrolling and filtering; port the remaining three modes and nested choices
   into the same visual language without changing commands. Cover every
   visible control; do not ship only the pretty default tab.
4. **Connected input and transaction review.** Actual keyboard/gamepad tests
   for focus, search, nested cancel, letter shortcut leak, displacement,
   separate-one, failed action retention, throw handoff/payment, paid actions,
   hotbar assignments, craft previews and unavailable tinkering. Exercise
   both reader routes, queued announcements and changed-context rejection.
5. **Actual Play, GPU and polish.** Ordinary input from the earned checkpoint:
   open/browse, inspect blade/tonic/material, return from both reader paths,
   equip/unequip, cancel a throw, one safe real action, abilities and craft
   preview, close/reopen after world movement. Compare actual1280×800 before/
   after, then wide window and controller input. Verify icon palette, sharp
   native pixels, actual focus and control hints. Fix material problems;
   document device/package and untested gameplay bounds honestly.

Initial native RED must precede inventory runtime edits. Subsequent counters
are labeled as such. Independent source review must examine state ownership,
view lifecycle, exact-item identity, controller behavior and work per idle
frame, not just whether the mockup looks attractive.

## Existing regression anchors

Run targeted selections based on changed seams, preserving their actual
behavior assertions. These fixture names/paths were verified on disk:

- `Presentation/Input/FullscreenUiCanvasOwnershipTests.cs` and
  `CenteredModalUIViewTests.cs`: fullscreen/split layout, real world layer
  suppression/restoration and popup camera transitions.
- `Presentation/Input/InventoryDetailsPresentationTests.cs`,
  `FiftyClarityReaderInputTests.cs`, plus
  `Presentation/UI/ExamineLedgerInventoryTests.cs` and
  `FiftyClarityDetailsTests.cs`: precise subjects, free reads, queue retention,
  search and stale callback rejection.
- `Presentation/UI/EquipmentComparisonInventoryTests.cs`,
  `Presentation/Input/GameAuditActionFeedbackTests.cs` and its adversarial
  fixture: actual body targets, comparisons, displacement and retained reasons.
- `Presentation/Input/GameAuditSeparateOneUiTests.cs`,
  `ConnectedInventoryInputTests.cs`, `PaidSelfConsumptionInputTests.cs`,
  `ThrowCursorInputTests.cs`, `ThrowPaymentBoundaryTests.cs`: entity/stack
  identity, action completion, payment and targeting.
- `Presentation/Input/GameAuditCraftingFlowTests.cs` and adversarial/bench
  companions: real selected components, shared marks, previews, scroll/click
  agreement, parked pointer and no repeated pointer-frame work.
- `Presentation/Input/NativeGamepadInputAdversarialTests.cs`: physical press
  semantics and quarantine; add inventory-specific visible-control coverage.
- `Presentation/UI/CombatInventoryPresentationTests.cs`,
  `CombatSupplyInventoryTests.cs`, `ItemUtility35InventoryTests.cs`,
  `MaterialGuidanceInventoryTests.cs`, `DensityItemExamineInventoryTests.cs`:
  authored implemented actions must remain available and truthful.
- `Presentation/Rendering/ExamineInventoryPortraitTests.cs` and relevant
  flat portrait GPU tests: authority and real texture output. Reusing a
  texture selector does not prove a new Canvas draw has correct colors.

Paths above are relative to `Assets/Tests/EditMode/`. The new wide-popup suite
tests shared popup backing, not this proposed inventory view. Never aggregate
overlapping suite totals into a fictitious distinct test count.

## Actual imagegen concept and extraction

Root generated and saved
[concept.png](../ArtSource/Live16x24/concepts/inventory-ledger/concept.png),
with the exact [prompt](../ArtSource/Live16x24/concepts/inventory-ledger/prompt.txt),
using the actual sixsite inventory screenshot as input. The saved concept is
**1586×992**, despite the requested1280×800; it is a concept image, not a
native screenshot or a text-size acceptance result. No generated pixels are
approved runtime sprites.

Useful extracted decisions:

- Separate `PACK & EQUIPMENT` title/resources and the five-tab row.
- A quieter named equipment rail, wide pack list, stable quantity/weight
  alignment and a distinct full-name selected-item panel.
- Ochre selection marker plus restrained row fill; warm ivory text against
  pine black, subtle sage rules and compact cyan stat accents.
- A small item icon alongside words, subordinate to readable identity.

Reject or correct these concrete concept errors before implementation:

- The footer invents `A Drop`, `X Examine`, `LT Details`, and `RT Tinkering`.
  Actual A is contextual confirm/actions; B cancels; Y is Tab; details is
  LT+RT in Menu context. The concept is not an input specification.
- The bright blue sword and other generated item images are not the accepted
  native assets. Reuse the exact actual selected owner art/UV, including its
  tested temper presentation; do not replace the base blade to match a mockup.
- A single `Arms` row loses real left/right anatomy. Head/body/arms/hands/back/
  feet and extra or missing parts must reflect current slots and remain
  individually selectable. Do not fake an empty slot that is absent.
- The mockup omits category headings, shrinks the lower details area, and
  wraps an equipped blade name tightly in the rail. Test actual long names,
  category scroll, multiple hands and full details at1280×800 before choosing
  final sizes. Keep source truth above any generated labels or numbers.
- The concept uses a very small right-edge `EQUIPPED` label; verify its real
  rendered size and keep an unambiguous selected/equipped distinction.

These parts can be built from original native UI primitives and borrowed
sprites. There is no need to extract or regenerate a raster UI background,
draw an illustrated bag, or create replacement actor art.

## Imagegen design prompt

> Create a production-minded1280×800 concept for the actual Caves of Ooo
> inventory, using the supplied real inventory screenshot as the content
> reference and the accepted skill screen/examine UI as style references.
> Preserve the original dark green flat pixel-art aesthetic, warm ivory
> readable pixel text, quiet green rules and restrained ochre focus. Design
> an equipment ledger: a compact dynamic equipment rail on the left, a broad
> categorized pack list on the right, and a generous selected-item detail
> panel below the pack list. Keep separate header resources and the five
> real modes Equipment, Inventory, Tinkering, Abilities, Crafting. Show the
> real frost-quenched long sword selected, real carried item names from the
> screenshot, stable quantity/weight columns, its existing useful facts,
> and a small crisp native sprite or true glyph. Do not invent stats, new
> items, rarity tiers, unsupported actions or equipment shown on the player.
> Make long names and controller focus readable at Steam Deck size. Reserve
> a clear footer for contextual confirm/back/details controls and a separate
> action outcome. Use the proposed layout proportions; avoid a narrow equal
> three-column dashboard, ornamental paper, blur, glow, glossy cards or3D
> portraits. The output is a design concept, not gameplay proof. Include no
> external branding or claim of exact pixel-grid production readiness.

Follow with a second concept for a nested item-action modal and Crafting only
if needed to resolve real layout ambiguity; do not generate dozens of looks
instead of implementing the chosen cohesive direction.

## Implementation prompt

> Read this plan, CLAUDE.md, the current live art direction and the actual
> inventory captures. Recheck every API you use. Implement the selected
> cohesive inventory presentation milestone by milestone with real failing
> regressions first. Keep InventoryUI's existing commands, owner validation,
> exact-item identities, five modes, paid-turn handoff and controller context.
> Produce one view owner with shared paint/hit geometry, readable selected
> information, truthful native thumbnails and explicit popup suspension.
> Test both F1 and queued Examine; never let an overlay hide those popups.
> Preserve approved sprites and normal-play BitLocker restrictions. Avoid
> per-frame preview computation, whole-inventory descriptions and texture
> captures. Run relevant native tests, independent review and ordinary
> keyboard/controller gameplay at1280×800 and wide aspect; compare actual
> screenshots and correct the largest concrete issues. Update this living
> record with RED/GREEN, counter-checks, files, limitations and actual capture
> receipts in the same commit. Do not claim physical Steam Deck or packaged
> performance until measured there. Finish all existing modes and transitions
> before calling the inventory redesign complete.

## Self-review and continuation

- 🟡 **Known integration risk:** Canvas versus camera-rendered popups;
  camera-bound layer/depth proof or explicit overlay suspend/restore and
  two-route tests are mandatory.
- 🟡 **Known input risk:** virtual controller `I` can reach alphabet popup
  shortcuts; verify and close this contextual leak while preserving keyboard.
- 🔵 **Design choice:** replace sparse fixed paperdoll boxes with actual named
  slot rows. Do not introduce a decorative avatar/customization feature.
- 🔵 **Implementation size control:** retain semantic state and transactions;
  do not generalize a UI framework or extend crafting/content while polishing.
- 🧪 **Acceptance open:** the generated concept is reviewed, but no new
  inventory implementation/tests/captures or device/performance result exists
  yet. Actual screenshots establish the old screen and its contents only.
- ⚪ **Deferred:** Nella material hierarchy remains planned. Resume it after
  this newly requested graphics slice and its verified integration.

Current files changed by this planning pass: **this document only**. Root
owns Unity, imagegen, native tests and implementation assignment. Initial
source review used the actual inventory baseline and existing interfaces;
independent controls/transaction audit confirmed the preservation contracts
above. Native execution, GPU color, visual polish and performance remain
separate gates.
