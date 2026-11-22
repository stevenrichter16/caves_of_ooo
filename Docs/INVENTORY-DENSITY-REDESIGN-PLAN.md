# Inventory density redesign — proposed November 21, 2026

User direction: plan a redesign so many more items are visible at once.
This combines a qualified first functional layout increment and further proposals; it is not a restart
of the cancelled graphics program. Preserve the existing visual language and art.

November22 priority reaffirmed by the user: keep this on the ACTIVE hourly work
list as more machine/tool work completes. It is the next player-facing layout
increment after the current water/package checkpoint. Begin with a current
bounded source/UI critique and native baseline, then the smallest complete
layout change, relevant checks and actual play. The proposed counts below are
historical targets. The November22 first increment below qualifies44/66 entries;
additional state badges/category buttons/sorting remain proposals.

Evaluate the screen during the recurring gathering/preparation/transport journey
in [MACHINE-GAMEPLAY-LOOPS.md](MACHINE-GAMEPLAY-LOOPS.md): finding the right
reagent, charged tool, treated garment and camp kit should be faster while full
names, consequences and existing actions remain accessible.

## What currently limits the pack

The live GameBootstrap camp scenario was opened with ordinary inventory input.
Its first page shows seven item entries and five category headings: twelve rows
out of twenty-five. The four camp kits are below the first page. A small pack
already needs scrolling to reach the tools for the current task.

The later actual workshop pack contains 71 item entries and six category
headings (77 rows); its first page shows ten items. Filled flasks and treated
garments can have identical visible names, forcing extra reader visits. Include
compact, disclosed state in rows where it distinguishes otherwise identical
entries: liquid identity/volume, charges and treatment markers. Search should
match those already disclosed facts too; never reveal hidden item properties.

The active renderer is `InventoryUI.LedgerView.cs`, not the old 80×45 tilemap
renderer. `LedgerListRows = 12` is fixed. A centered 1280×800 content rectangle
is scaled, so a larger monitor does not reveal additional rows. Equipment and
stats take 312 pixels of width; the pack has only 330 pixels of height; the
selected-item panel occupies most of the space below it. Quantity appears both
in stack names and in a separate column. These are layout costs, not a need to
make all text smaller.

## Recommended browsing view

Make Inventory a dedicated, full-height pack view. Keep Equipment as its own
comparison-focused tab. Retain Tinkering, Abilities, Crafting and Loadouts.

```
PACK   weight / capacity   drams   ink                modes / close
Find: ...       All | Weapons | Armor | Food | Materials | Tools ...
-----------------------------------------------------------------
ITEM                  QTY WT | ITEM                  QTY WT | SELECTED
...                         | ...                         | full name
...                         | ...                         | condition
...                         | ...                         | contents
...                         | ...                         | comparison
...                         | ...                         | useful facts
...                         | ...                         | F1 full details
-----------------------------------------------------------------
items 1–64 of 120    sort ...    navigation / actions / drop / details
```

Use two compact text columns for the pack, with a narrower details pane on the
right. These are rows with names and useful numbers, not icon-only tiles.
Both columns share one ordering and one selection. The current selected item's
full name, liquid contents, charges, proofing and equipment comparison remain
available in details; full Examine and action popups keep their existing routes.

Remove the persistent paperdoll/stats from Inventory. The Equipment tab retains
body-slot browsing, candidate comparison and relevant actor stats, including
extra limbs. Switching back restores the pack selection and scroll position.

Category filters occupy a compact top strip instead of consuming item rows.
The default All view remains sorted by category, then the existing stable item
order; show the selected item's category in details. Filtering and sorting are
free browsing actions. Keep `/` search and its current cancel/clear behavior.
Show stack quantity once, in QTY; keep meaningful name qualifiers and distinct
item identities. Do not merge merely similar garments, flasks or damaged items.
The weight summary must read the current pack's actual `MaxWeight`, including
the −1 unlimited case, and distinguish it from the Strength-derived soft limit.
The ordinary Creature blueprint starts at150, while a sidebar can show a
Strength-derived270; scenario loadouts can expand the hard cap. Do not hard-code
150 into the screen or infer ordinary capacity from the large workshop fixture.
Explain the applicable limits without changing either game rule in this layout.
Move DMG/DEF to selected-item comparison rather than spending a wide column on
dashes for most materials and tools. Keep equipped and other important markers.

## Density targets, subject to native readability checks

Measure actual item entries, excluding headers and empty cells. Derive capacity
from available height and the chosen UI text scale, rather than a fixed row count.

| Window, default text scale | Proposed two-column item capacity | Current maximum |
| --- | ---: | ---: |
| 1280×720 | at least 36 | 12 rows including headings |
| 1280×800 | at least 44 | 12 rows including headings |
| 1920×1080 | at least 64 | 12 rows including headings |
| 2560×1440 | at least 88 | 12 rows including headings |

These are original design targets. Actual first-increment800p/1080p results are44/66;
720p/1440p and other densities have layout-test evidence only. Start with roughly
26-pixel rows and 20-pixel body glyphs at default scale; reserve room for controls
and status messages. The existing readable text size is the starting point.
Honor a larger text scale by reducing capacity. If columns become too narrow,
fall back to one full-height item column, with details available on demand.
Do not squeeze names into illegible columns to meet a count. At wide sizes use
the additional space for names before considering an optional third column.
Require space for at least 32 average-width name glyphs in each column, after
quantity, weight and markers; otherwise use one column. Remove only the view's
generated stack suffix when quantity is shown separately, preserving numeric
qualifiers in the item's actual name.

## Navigation and selection contract

- Use column-major order: a shared page fills the left column top to bottom,
  then the right. Both columns share one page/scroll offset. Up/down move within
  a column; left/right move to the neighboring item column
  at the same row. At a ragged end, choose the closest valid row. Browsing never
  wraps into a different tab or invokes an action accidentally.
- Page Up/Down advance a visible page. Home/End reach the beginning/end of the
  current filtered list. Tab keeps its current mode-switching meaning; category
  filters have explicit clicks/shortcuts that do not steal item actions.
- Enter opens the selected item's existing action menu; F1 opens full details;
  Escape returns to the same item. Pointer hit areas match painted rows, wheel
  scrolling follows the hovered list, and selection is visible without color alone.
- Preserve selection by item identity through sorting, filtering, reader return,
  resizing, stack changes and commands. If the item disappears, select the nearest
  remaining item. Do not preserve a stale row index that now names another item.
  A filter that excludes it selects the first matching item and remembers the
  previous selection; clearing the filter restores that item if it still exists.
  Explicitly selecting another item replaces that remembered selection.
- Retain contextual controller prompts and nested cancel behavior. This proposal
  does not reopen deferred Steam Deck hardware qualification.

## Small implementation sequence

1. Capture the current full workshop pack, an empty pack, long machine names,
   liquids, equipment comparison and a populated action menu at 800p and 1080p.
   Record item counts and actions needed to reach a camp kit/reagent.
2. Build the full-height two-column Inventory layout over the existing screen
   model and command paths. Centralize layout, capacity, hit testing and movement
   so painting and input cannot disagree. Leave Equipment's functional layout intact.
3. Move category headings into filters, remove duplicate quantity text, and add
   stable sorting only after selection retention works. Reuse existing details
   snapshots: hovering/painting must not repeatedly invoke mutable descriptions.
4. Validate native navigation and actual gameplay, then tune spacing and names.
   Do not rebuild custody, equipment, transactions, crafting or save data for this.

Likely files: `InventoryUI.LedgerView.cs`, `InventoryUI.LedgerInput.cs`,
`InventoryUI.cs`, `InventoryUI.Clarity.cs`; retain `InventoryScreenData` as the
model unless a small presentation-only item ordering adapter is needed.

Acceptance requires the density targets at readable scale, no clipped names
without accessible full names, all real items reachable exactly once, and stable
selection across 0/1/12/44/64/200 entries, resize, search and action completion.
Native input checks must include column edges, category filtering, long stacks,
extra body slots, F1 return and popup cancellation. Actual play must cover equip,
drop, throwing cancellation, liquid/camp actions and one paid action: quantities,
custody and turn costs stay governed by the existing commands. Obtain a bounded
independent source/UI review before acceptance. The broader sequence remains subject to these gates; the first layout receipt
below supplies its own bounded implementation evidence.

Bounded independent plan review completed November 21: four requested navigation,
filter-return, name-width and quantity contracts are incorporated above. Native
readability, rasterized layout and implementation review remain future gates.

November22 bounded recurring-use/inventory-priority critique found no material
design blocker. Its current-capacity correction is incorporated above. Pin
column boundaries, ragged pages and wheel/hover behavior in the upcoming current
source/UI critique before writing RED cases. Proposed targets remain unqualified.


## First layout increment — November22 source and native contract

Current source/UI critique permits an item-only ordinal adapter over existing
header-bearing rows. Retain exact row/owner identities for existing commands and
all other tabs. The dedicated Inventory view will use actual Canvas pixel bounds,
full height, at most two name/quantity/weight columns, a contextual details pane
when it fits, and a one-column fallback. Actual glyph advance is half glyph height;
the32-name-glyph minimum must include numeric columns and equipped markers.
Remove only the generated final stack suffix; retain genuine numeric qualifiers
and live striker charges already present in the display snapshot.

Up/down crosses a page boundary while retaining the selected column where valid;
left/right stays within the page and never changes modes. Page Up/Down preserves
row/column where valid; Home/End selects the exact first/last item. Ragged targets
choose the closest valid owner. These rules keep all pages reachable with a
D-pad. Wheel over the list uses the same vertical movement; other screen regions
do not scroll it. A modal owns pointer/wheel input over its own translated/scaled
frame and never exposes rows beneath it. Existing Tab/T and popup keys remain.

Resize must refresh geometry before pointer dispatch, preserve the selected
owner, and ignore a stationary pointer for that resize frame. Repaint and layout
changes must reuse the existing model and description snapshot: no action or
mutable-description requery on resize/scroll/hover within the same row. Rebuild,
query/clear and reader return all need identity preservation; removing a selected
item chooses its nearest remaining neighbor. Center/scale all modal children,
text and hit rectangles together, including full-window dimming and status.
No new per-frame component or inventory/custody/time rules are needed.

Actual earned1580 workshop baseline has69 combined carried/equipped owners in75
rows, not the earlier71/77 pack. Native I/Tab browsing shows10 real entries at
both1280×800 and1920×1080. Tick1580/HP400 and quantities remain unchanged. Its
fixture hard cap is5000 and soft limit1050. Screenshots and observations are in
/private/tmp/coo-inventory-{before-800,before-1080}-20261122.png and
/private/tmp/coo-inventory-density-baseline-20261122.json. External execute-code
Screen.width is Editor-context2319×1283; actual GameView Canvas/PNG dimensions
establish the target resolution. Owned keyboard/settings/callbacks and original
GameView cleanup was initially incomplete (relative-index removal was a no-op);
the corrected absolute-index cleanup and final fresh checks are in the receipt.
Empty inventory, paid actions and camp-kit use were not staged in this baseline.

This first increment may defer category-button filters, extra state badges and
new sort controls. Existing search/readers remain available. Publishing the
layout will not claim completion of the entire redesign.

Performance: compute one small geometry value on layout invalidation and the
item-ordinal map on model rebuild. Compare viewport/text-scale scalars in the
existing input loop; paint only on changed geometry/input/model. Retain label/
image pools and selection-prose cache. Do not query item actions or build lists
each idle frame. Tests cover capacity/columns,200-entry reachability, exact hits,
ragged navigation, selection/removal/query/resize, modal isolation and actual
current weight limits. Native readability and action/save gates remain required.

## First layout qualification — November22

November22 [inventory layout qualification](Verification/MachineSystems/InventoryDensity20261122/README.md):
dedicated full-height Inventory shows44 actual entries at1280×800 and66 at1080p
versus10 before, with measured final10×20px body glyphs at both targets.125% text gives30 at800p.
Stable exact owners/search/resize, fitted modals and current hard/soft capacities;
all existing command/time/custody rules remain. Valid RED27/26fail, reviewRED5/
4fail, projectionRED3/2fail; final160+4 with3 overlapping strengthened cases =
161 distinct passes (41new/120existing). Native arrows/pages/search/two-page
reader/cancel/free wear/unwear, paid food2→1 and F5 verified. Separate earned
1580→1590 minimal branch archived BEFORE Stop and fresh Continue preserves exact
actor/food/flasks/treatments/69IDs; first1600 save was not archived and has no
fresh claim. Misrouted follow-up made one real south move, retained/disclosed.
Final corrected camera projection fills both targets. Profiler windows yielded
no marker events, so no timing claim. Main stopped/rootnull/profilerfalse,
original7GameView presets restored;84foreign/861normal unchanged. Final
independent artifact/gameplay-evidence review accompanies the receipt. No new
drop/liquid/camp actual-play or physical controller acceptance. Full redesign
category filters/state badges/sort and all twelve full-machine gates remain open.
NEXT: publish this layout and a current Mac candidate, then make disclosed flask/
charge/treatment state easier to find and compare during recurring gathering,
processing, preparation and freight trips. Preserve choices/services/lasting
upgrades; no compulsory upkeep. Chronology/protective NPC work still precedes
exposure enrollment; graphics cancelled, capstan/cultures parked, Deck deferred.


The receipt supersedes earlier future-tense descriptions only for the bounded
first layout. Extra category/sort/state UI and wider gameplay coverage remain open.

## Publication and next discovery increment

Runtime fd4a638b4 is pushed/remotely verified. The current universal Mac candidate
is recorded in Verification/MachineSystems/MacCandidateFD4A638B4/README.md; boot
only, with packaged actions/physical input still open. No need to repeat accepted
layout tests or the earned native branch.

Source foothold for next work: InventoryScreenData.ItemDisplay currently snapshots
name/category/weight/value/quantity/equipped/actions, not explicit fluid state.
IToolMenuState is already used by WorldInteractionSystem to distinguish flask
contents and waterskin charges in world menus; current implementations are
LiquidVesselPart and WaterskinPart. Audit that public information contract and
privacy before reusing it in cached rows/search. Striker charges already occur
in its display name. Treatment markers need their own disclosed-information
audit. Keep row geometry/count, exact item identity, actions and stack rules.
This is a source selection and next proposal, not implemented extra badges.

## Second increment under critique — public item state

Player problem: twelve identically named flasks and similarly named treated
cloaks need repeated reader visits to choose the right finite input/receiver or
expedition garment. Preserve44/66 entries and existing glyph/row geometry. Show a
compact, explicitly disclosed state beside each name, retain full name/state in
selected details/F1, and search the same cached public fields. Existing actions,
quantity/custody/stack identity and clocks remain authoritative.

Source correction: IToolMenuState currently serves flask/skin world-menu labels;
InventoryScreenData does not snapshot it and pack search re-reads live names.
ExaminablePart currently has no identification gate and publishes enhancement
GetEffectDescription, but that does not authorize searching arbitrary Part names,
blueprint IDs, unknown liquid IDs or protected lore. Use only opt-in public state.
Existing striker charge counters are already in display names and must survive.

Bounded independent design/source critique found no remaining blocker after
these corrections. Contract for RED-first implementation: snapshot existing IToolMenuState
once per model rebuild plus an optional default-empty inventory-state hook on
IItemEnhancement, initially only lacquer, lasting proofing and temporary coat.
Use short public treatment labels, with no new numeric/duration claim (remaining wear time needs the existing wearer/garment policy rather than a new
badge-derived duration). Exhausted coats
show spent; malformed amounts/remaining state show unavailable. Preserve complete
reader facts through the existing reader. Do not alter world-menu grouping by
adding these treatments to IToolMenuState. Nonowned/stale custody/provider Parts
must not be queried for extra state. Unknown enhancements default to no badge.
Invalid vessel state must say unavailable rather than expose raw unknown IDs;
a shared provider correction requires its own world-menu compatibility pins.

Use cached strings for row paint/search/selection; no full-description or extra
action queries from paint/hover/resize. Query editing rebuilds only retained rows,
not the model or its action/name/state providers. While editing, hide the selected
pane without discarding its same-owner document. Open/commands still refresh the
model; commit paths revalidate current state. Keep literal case-insensitive name search
and stable exact-owner selection. A separate snapshot match method avoids
ambiguating the existing reflection-tested Filter(Entity) API. Reserve bounded
space for compact state without hiding the whole name; independently mark
truncation and retain both full values in selected/F1. Multiple public states
must remain distinct and stable. Existing320px name cell is a total text budget,
not a new license to shrink body glyphs or halve item capacity.

RED/countercases must cover empty/full/multiple fluids, partial water charges,
all three treatment kinds and combined treatments, numeric striker names,
invalid/unknown IDs, default-empty hidden enhancement metadata, foreign/stale
custody, multiple/throwing providers, bounded single-line text, exact same-name
owner selection, same-ID state refresh after action/reload, zero extra provider
reads on paint/resize/search matching, full-name/F1 fallback and unchanged density.
Actual use should find an empty receiver/raw feedstock/treated garment by public
state, cancel freely, execute one existing finite action, observe the right owner
and world consequence, F5/archive BEFORE Stop, and freshly Continue the same stock.
This was the pre-implementation contract; the following qualification records
the shipped bounded badge/search slice. Complete-machine acceptance is separate.

## Public-state qualification — November22

November22 [public inventory state](Verification/MachineSystems/InventoryPublicState20261122/README.md)
adds cached public flask contents/water charges and opt-in garment treatments to
rows, selected/F1 details and literal search, preserving44/66 entries and exact
owners. Shared IToolMenuState validators hide malformed/private liquid IDs;
default-empty enhancement summaries avoid arbitrary type/description scans.
Queries now rebuild retained rows only; same-owner search/cancel retains hidden
private prose while exposing no details during typing. Valid RED34/33fail and
reviewRED10/2fail -> final299/299 (44new255existing), noCS. One old private-cache
assertion was retargeted to retained-private/null-public/no-hit behavior.
Actual workshop1590->1620: findheatcoat/empty/mire;
freecancel;2231pour12->11 createspool1; ordinary step applies mire-soaked/HP400->399;
Cmenu distinguishes11/12 via2231 fromempty via2235; emptyfill gains1 and retires
pool, countedflask+pool units48throughout. Fresh1620 retains exact measured
world/69IDs/coat97/badges. Separate carried-water60->70:skin2254 2->1/debt1->0,
Str/Agi17->18,HP40, row2/3->1/3; fresh70 exact. Archives copied BEFORE Stop.
Initial enabled-but-uninitialized Play recovered by reopening same clean saved
scene then ordinary GameBootstrap; no manualbootstrap/grants/teleports.
Main stopped/rootnull/clean/profilerfalse, original7presets restored,84foreign/
861normal unchanged. Independent source/artifact/gameplay-evidence review
accompanies receipt; no hardware/fullredesign/complete-machine acceptance.
NEXT: current Mac candidate, then ordinary regional mire supply->existing
camp/community processing->useful output source/design/native baseline. Existing
fixture still/oil/cold chains and finite-poured-fuel correction stay accepted;
do not reopen the closed oil-film P2. No new recipe implied. Category/sort remain
separate; chronology/protective NPC prerequisites still precede exposure.


The second slice is bounded; category controls/sorting and broader redesign gates
remain open. The detailed receipt preserves field/charge/treatment scope and limits.

## Current package

November22 [current public-state Mac candidate](Verification/MachineSystems/MacCandidate910F86DAE/README.md)
packages pushed/remotely verified runtime910f86dae. Universal6000.3.4f1/nondev,
56.487838s/0errors/5warnings exactly matchingFD4. Own74682/window70681 actual
chooser/world/HUD boot; generation2667->2668final/7creatures. Fresh nativeinput
preflightfalse, NO input posted, so packagedinventory/actions/save/controller
remain open.32005source/32008effective/592external/186app pins;83changed0missing/
3added exactoverlay, runtimeC# unchanged on import. Independent final artifact
review0P1/P2; fresh full logicalprefs/Savesabsent and all5priorapps186files each
independently checked. Main stopped/rootnull/clean/profilerfalse,84foreign861normal
fresh17:37unchanged. All copies/builds retained; no new delivery/full12 acceptance.
NEXT: ordinary regional mire supply->existing processing->useful output
source/design/native baseline. Public inventory state and44/66 layout are
published; category/sort and physical-input gates remain separate. Reuse accepted
fixture oil/cold and finite-poured-fuel evidence; do not reopen closed P2.
Chronology/protective NPC prerequisites remain before shared exposure enrollment.
