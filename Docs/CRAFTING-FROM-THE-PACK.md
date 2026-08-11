# Crafting from the pack — forge and brew without a station

> **Request (2026-08-10):** *"allow the player to forge weapons and create
> alchemical brews from their menu. instead of needing dedicated in world
> objects the player can craft from their inventory. plan out and design
> the UI first. then implement the full feature."*

Living doc per CLAUDE.md. Plan → verification sweep → UI design →
sub-milestones → implementation log.

---

## 1. What exists today (verification sweep)

Read before planning; every claim below is cited.

| Thing | Where | Shape |
|---|---|---|
| Forge gate | `ForgeWeaponCommand.cs:75` → `ForgePart.IsNearForge` | 3×3 adjacency to a `ForgePart` entity |
| Still gate | `BrewReagentsCommand.cs:109` → `AlchemyStillPart.IsNearStill` | same shape |
| Selection model | `CraftingMarkPart.cs` | per-item "set aside" mark; `CollectMarked(actor)` returns `MarkedSelection { Reagents, Components, Coatings, Weapons }` |
| Forge UI | `ForgePart.HandleEvent("GetInventoryActions")` | sectioned toggle rows on the **world-action menu**, one `>> Craft <<` button |
| Brew UI | `AlchemyStillPart.HandleEvent` | flat toggle rows + `brew the mix` |
| Forge math | `WeaponForgingService.ApplyComponentStats(weapon, bladePart, haftPart, bindingPart)` (private, :368) | derives stats from the three component parts **and nothing else** |
| Brew math | `BrewResolver.Resolve(...)` (:30) | **pure** — reagent property lists in, `BrewResult` out |
| Reagent data | `ReagentPart.GetProperties()` | `IReadOnlyList<BrewPropertyAmount>` |
| Batch paths | `TryForgeBatch`, `TryBrewBatch`, `GetMaxBatchCount` ×2 | already exist, already pure for the count |
| Inventory screen | `InventoryUI.cs` (4039 lines) | 80×45; tab bar at x=1/13/25/37; panels Equipment/Inventory/**Tinkering**/Abilities |
| Closest UI precedent | `RenderTinkeringPanel` (:2287) | divider at x=50, mode header `> Build    Mod`, scrolling list with `>` cursor, `<cost>` right-aligned, affordable=Gray / unaffordable=BrightRed |

### Three findings that shape the design

**1. Brewing already crafts without a station.** `BrewReagentsCommand.cs:109`
reads `if (!foodOnly && !AlchemyStillPart.IsNearStill(...))`. A food-only
mix is already legal anywhere. So "craft without a station" is not a new
concept being introduced — it is an existing exception being generalised.

**2. The result of a craft is predictable before you commit.**
`BrewResolver.Resolve` is pure and takes only reagent property lists.
`ApplyComponentStats` takes only the three component parts. **Both
outcomes can be computed without consuming anything** — which means the
new UI can show the player what they are about to make. Today they mix
blind and find out afterwards. This is the single biggest quality-of-life
win available here, and it falls out of the existing architecture for
free.

**3. Selection is already modelled and already shared.**
`CraftingMarkPart` marks live on the items, not on a station. A new panel
should be a better *view* onto the same marks — not a second selection
concept. Mark something at a forge, walk away, open your pack: it is
still marked.

---

## 2. Decisions (stated, not asked — veto any of these)

The request is unambiguous about the goal. These are the routine calls I
made to get there; each is cheap to reverse.

| Decision | Why |
|---|---|
| **A new 5th panel, `Crafting`** — not more Tinkering modes | Tinkering is *recipe-driven* (pick a known recipe from a list). Forging and brewing are *selection-driven* (assemble from what you carry). Merging them gives one panel with four modes and two incompatible interaction models. |
| **Stations are no longer required — but keep a real benefit: batching** | Craft-anywhere makes **one** at a time; standing at a forge or still unlocks **batch**. Every forge and still already placed in the world keeps a reason to exist, the code path already exists (`TryForgeBatch` / `TryBrewBatch`), and it costs nothing. The convenience the request asks for is fully delivered either way. |
| **Reuse `CraftingMarkPart`, add no new selection state** | One source of truth; the station menu and the pack panel stay in sync automatically. |
| **The panel previews the result** | See finding 2. This is the reason to build a panel rather than just deleting two gate checks. |
| **Keep the station menus working exactly as they do now** | Zero regression for a player who likes the current flow. |

---

## 3. UI design

80×45 grid, same conventions as every other panel: `>` cursor, `[x]`
marks, DarkGray chrome, White selection, BrightRed for "you can't".

### 3.1 Tab bar

```
Equipment  Inventory  Tinkering  Abilities  Crafting            Wt:98/240 $85 i7
```

`Crafting` sits at x=49. The existing header info block is right-aligned
and unaffected.

### 3.2 Forge mode — incomplete selection

```
Equipment  Inventory  Tinkering  Abilities  Crafting            Wt:98/240 $85 i7
────────────────────────────────────────────────────────────────────────────────
 > Forge    Brew                                 │  ON THE ANVIL
                                                 │
 ══ Blades ══════════════════════════            │   Blade    iron blade
 > [x] iron blade                     x2         │   Haft     oak haft
   [ ] obsidian shard                 x1         │   Binding  —
                                                 │   Quench   —
 ══ Hafts ═══════════════════════════            │
   [x] oak haft                       x3         │  ┌ RESULT ─────────────────┐
                                                 │  │                         │
 ══ Bindings ════════════════════════            │  │  Pick a binding to      │
   (none carried)                                │  │  finish the weapon.     │
                                                 │  │                         │
 ══ Quench (optional) ═══════════════            │  └─────────────────────────┘
   [ ] flask of burning oil           x1         │
                                                 │
                                                 │
────────────────────────────────────────────────────────────────────────────────
 space pick   enter craft   C clear   F/B mode   tab panel
```

### 3.3 Forge mode — complete, with a live preview

```
 > Forge    Brew                                 │  ON THE ANVIL
                                                 │
 ══ Blades ══════════════════════════            │   Blade    iron blade
 > [x] iron blade                     x2         │   Haft     oak haft
   [ ] obsidian shard                 x1         │   Binding  leather cord
                                                 │   Quench   flask of burning oil
 ══ Hafts ═══════════════════════════            │
   [x] oak haft                       x3         │  ┌ RESULT ─────────────────┐
                                                 │  │  iron sword             │
 ══ Bindings ════════════════════════            │  │                         │
   [x] leather cord                   x1         │  │  Damage    1d6+2        │
                                                 │  │  Pen       3            │
 ══ Quench (optional) ═══════════════            │  │  Weight    8            │
   [x] flask of burning oil           x1         │  │  Quench    burning      │
                                                 │  └─────────────────────────┘
                                                 │
                                                 │   At a forge: batch up to 2
────────────────────────────────────────────────────────────────────────────────
 space pick   enter craft   C clear   F/B mode   tab panel
```

The `At a forge:` line is the station's advertisement. Standing at one it
reads `Batch up to 2  [shift+enter]` in BrightYellow.

### 3.4 Brew mode — the mix resolves as you pick

```
   Forge  > Brew                                 │  IN THE MIX
                                                 │
 ══ Reagents ════════════════════════            │   bloodroot
 > [x] bloodroot                      x3         │   ashcap
   [x] ashcap                         x2         │
   [ ] glowmoss                       x1         │  ┌ RESULT ─────────────────┐
   [ ] rot cap                        x4         │  │  tonic of healing       │
                                                 │  │                         │
                                                 │  │  Healing        6       │
                                                 │  │  Poison         2       │
                                                 │  │                         │
                                                 │  │  drink  heal 6          │
                                                 │  │  throw  splash, poison  │
                                                 │  └─────────────────────────┘
                                                 │
                                                 │   At a still: batch up to 2
────────────────────────────────────────────────────────────────────────────────
 space pick   enter brew   C clear   F/B mode   tab panel
```

**Picking a third reagent updates the RESULT box before you commit.** That
is the feature. Alchemy stops being a slot machine.

### 3.5 Keys

| Key | Does |
|---|---|
| `F` / `B` | Forge mode / Brew mode (mirrors Tinkering's `B`/`M`) |
| `↑ ↓` / `K J` | move the cursor, skipping inert section headers |
| `space` | pick / unpick the item under the cursor |
| `enter` | craft one |
| `shift+enter` | craft a batch — **only at a station** |
| `C` | clear every pick |
| `tab`, `← →` | panel navigation (existing convention) |

### 3.6 Layout constants

Mirrors the Tinkering panel so the two read as siblings.

```
CRAFT_DIVIDER_X   = 49     // vertical rule
CRAFT_LIST_START_Y = 4
CRAFT_LIST_END_Y   = 40
CRAFT_ANVIL_X      = 51
CRAFT_RESULT_Y     = 12
```

---

## 4. Sub-milestones (smallest blast radius first)

Each ships one complete testable behaviour and commits independently.

### C1 — Pure preview services 🔑
Extract `WeaponForgingService.PreviewForge(bladePart, haftPart, bindingPart)`
returning a `ForgePreview { DisplayName, Damage, Penetration, Weight, … }`
by refactoring `ApplyComponentStats` to write *from* that struct — so the
preview and the real forge cannot drift, because the forge computes the
preview and then applies it. Add `BrewingService.PreviewBrew(reagents)`
wrapping the already-pure `BrewResolver.Resolve`.

**Invariant:** what the panel shows is what the craft produces. Tested by
forging and comparing the produced weapon's stats to the preview.

### C2 — Ungate the commands
`ForgeWeaponCommand.Validate` and `BrewReagentsCommand.Validate` stop
requiring a station for a single craft. Batch keeps the station
requirement. Diag records the station-vs-pack provenance so
`diag_query category=craft` can answer "where was this made?".

**Counter-check:** batch without a station still refuses, with a message
naming the station.

### C3 — The Crafting panel: navigation + selection
New `PANEL_CRAFTING`, tab-bar entry, mode switch, sectioned list built
from the carried items, cursor that skips headers, `space` toggling
through `CraftingMarkPart.Toggle` (the same marks the station menu uses).
No crafting yet.

### C4 — The RESULT box
Wire C1's previews into the anvil panel. Incomplete selections explain
what is missing rather than showing nothing.

### C5 — Craft from the panel
`enter` executes through the existing commands and `InventorySystem.ExecuteCommand`
— no bypassing the pipeline. `shift+enter` batches when at a station.
Rebuild + re-render after, so the list reflects consumed items.

### C6 — Adversarial sweep + cold-eye + both audit angles
Surfaces this feature touches: state atomicity (partial craft), stacking
(picking one of a stack of 3), cross-system (marks shared with the
station menu), boundary inputs (empty pack, cursor past end), anti-exploit
(craft with items dropped mid-selection). Well over the two-surface gate.

### C7 — Live PlayMode verification
The panel is visual; EditMode cannot prove it renders. Per CLAUDE.md this
is where the honesty bounds get written.

---

## 5. Performance

Two of CLAUDE.md's triggers apply.

- **The panel rebuilds only on change.** Selection toggles and mode
  switches call `Rebuild()`; render is otherwise a pure redraw of cached
  rows. No per-frame inventory scan.
- **No allocation in the render path.** Row list is a member, cleared and
  refilled in `Rebuild`, never `new`-ed in `Render` — the pattern the
  Tinkering panel already follows.
- **Preview is computed in `Rebuild`, not `Render`.** `BrewResolver.Resolve`
  allocates a result; calling it per-frame would be the exact
  cache-miss-is-expensive anti-pattern `Docs/PERF-FOUNDATION.md` warns
  about.

---

## 6. Observability

| Gate | Record |
|---|---|
| Craft succeeds from the pack | `craft` / `Crafted` — `{ kind, atStation:false, inputs, output }` |
| Craft succeeds at a station | same, `atStation:true` |
| Batch refused away from a station | `craft` / `CraftRejected` — `{ reason:"batch_needs_station" }` |
| Selection incomplete on enter | `craft` / `CraftRejected` — `{ reason:"incomplete_selection", missing }` |

`craft` must be added to `Diag.DefaultOnCategories` or every record is
silently dropped — the exact trap the `spell` category hit in SM7.

---

## 7. Implementation log

### C1 — pure preview services ✅

`WeaponForgingService.PreviewForge(blade, haft, binding)` → `ForgePreview`
and `BrewingService.PreviewBrew(reagents)` → `BrewPreview`. Both consume
nothing, create nothing and log nothing.

**The anti-drift design.** The real forge does not compute stats
separately any more: `ApplyComponentStats` now calls `PreviewForge` and
copies the answer onto the weapon. Brew naming routes through a shared
`ComposeBrewName`. So a preview that disagrees with the product is not a
bug to be fixed later — it is unreachable. A panel that promises 1d6+2
and forges a 1d4 would cost more player trust than the whole feature is
worth, and that is the one failure mode a preview feature has.

Pinned by `PreviewForge_MatchesTheWeaponForgingActuallyProduces` (forges
for real, compares seven fields) and the brew equivalent, plus a purity
counter-check that previewing five times touches nothing in the pack.

**RED confirmed as a compile error** before any production code — and it
paid immediately: it surfaced that `TryBrew` takes six parameters, not
the five I had written.

**Two fixture corrections from the first run**, both mine, neither a
product bug:
- `BrewRuleRegistry` must be initialised in `SetUp` or every mix resolves
  to nothing.
- Reagents carry *input* properties in lowercase (`heat:2, combustible:3`)
  which rules then map to *effects*. I had invented `Healing:4`, which is
  an output, not an input. Fixture rewritten against real reagent data.
- `CollectionAssert.AreEquivalent` on `BrewPropertyAmount` compares
  references — the type is a class with no value equality, so that
  assertion could never have passed. Now compared by value.

### C2 — stations stop being required ✅

A single craft works anywhere; a **batch** still needs the forge or the
still. The convenience the request asked for is fully delivered, and
every station already placed in the world keeps a reason to exist.

Brewing already had the shape of this exception —
`BrewReagentsCommand.cs:109` let food-only mixes brew in the field — so
this generalises an existing rule rather than inventing one.

**Four existing tests failed, exactly as they should have**, because
they encoded the old contract. Rewritten to pin the new one, each paired
with a batch counter-check that keeps C2 honest:
`ForgeBatch_AwayFromForge_StillRejected_AndNothingConsumed` and
`NoStill_BatchBrew_StillRejected_NothingConsumed`. Without those, ungating
one craft would have quietly ungated all of them.

`Forge_NullZone_RejectedLegiblyNotCrash` became
`Forge_NullZone_ForgesFromThePackWithoutCrashing` — a pack craft never
consults the zone, so a null one is irrelevant rather than a rejection.
The no-crash guarantee is the part that still matters and is still
asserted.

### C3–C5 — the Crafting panel ✅

Fifth panel, `PANEL_CRAFTING`, in `InventoryUI.Crafting.cs` (partial
class, so the 4000-line `InventoryUI.cs` does not grow). Sectioned
selection list on the left, the anvil and RESULT box on the right,
`F`/`B` to switch mode, `space` to pick, `enter` to craft,
`shift+enter` to batch, `C` to clear.

Selection routes through `CraftingMarkPart.Toggle` — the **same** marks
the forge and still menus use, so a kit set aside at an anvil is still
set aside when you open your pack a zone later. No second selection
model.

Previews are computed in `Rebuild`, never in `Render`: `PreviewBrew`
allocates, and calling it per redraw is exactly the
cache-miss-is-expensive anti-pattern `PERF-FOUNDATION.md` warns about.

### C8 — the whole screen, restyled ✅

*(User request mid-implementation: "i want it to look like this mockup" —
the entire screen, not just the new panel.)*

`InventoryUI.Chrome.cs` holds the shared visual language as panel-agnostic
helpers — `DrawSectionRule` (`══ Title ═══════`), `DrawTitledBox`
(`┌ TITLE ─────┐`), `DrawPickRow` (`> [x] name      x3`), `DrawLabelled`,
`DrawKeyHints`. Every rule about how the screen looks lives there once.

Applied outward from Crafting:
- **Inventory** — category headers became section rules instead of bare
  yellow text.
- **Tinkering** — `│` divider matching Crafting's, the same two-tone mode
  header, a section rule over the recipe list, and the bit locker in a
  titled box.
- **Equipment / Inventory / Abilities** — key-hint legends on the footer
  row, so every panel tells you what it can do.

One layout bug caught while writing it: the bit-locker box was fixed at
height 8 while its contents are 12 bit rows, so the bottom edge drew
through the last four. Sized to content.

**Honesty bound:** EditMode proves this compiles and that nothing
regressed. It cannot prove the screen *looks* right — that needs a Play
session and eyes.
