# Tinkering & Spell-Crafting — Review + Continuation (2026-07-15)

> Review of the crafting (Tinkering) and spell-crafting (Grimoire →
> Mutation) mechanics, per user direction: "review, fix, and continue."
> Fixes landed in the same ship; findings below use the §5 severity
> scale. **Honesty bound:** this session has no Unity editor — all
> touched JSON parses and all code was reviewed line-by-line, but the
> EditMode suite has NOT been run. Run it first thing next Unity
> session; the new/changed tests are listed in §IV.

---

## I. What exists (survey)

**Tinkering (crafting):** `TinkeringService` (craft / mod / disassemble
with rollback paths), `TinkerRecipeRegistry` (JSON recipes +
auto-generated build recipes for weapons/armor from blueprints),
`BitLockerPart` (bits + known recipes), `BitCost`, `TinkerItemPart`,
`TinkerModificationRegistry` + mineral-infusion/armor mods.
`Recipes_V1.json` ships 10 recipes. Test coverage was solid on happy
paths and mod-application (20+ tests).

**Spell-crafting:** `GrimoirePart` items teach mutations (spells) via
a Read inventory action; ~20 grimoire items shipped; ranked mutations
upgrade through `MutationsPart.AddMutation`; `GrimoireCopy` economy
exists in conversations. The deeper "spell economics" design
(`Docs/Design/ALCHEMY_SPELL_SYSTEM.md`: learn-cost in bits, mana,
amplification, well-transmutation) is **design-only — no mana system
exists in code**; it is a future ship, not a gap in the current one.

## II. Findings

### 🔴 Finding 1 — Craft batch failure duplicated resources (FIXED)
**File:** `TinkeringService.cs` (TryCraft loop)
For `NumberMade > 1` recipes, a mid-batch failure (blueprint failure
or inventory-full on item N) refunded the full bit cost and the
ingredient **while leaving items 1..N-1 in the inventory** and in the
`crafted` out-list. Net: partial output + full refund = duplication.
All shipped recipes are currently NumberMade 1, but generated recipes
read `TinkerItem.NumberMade` from blueprints, so the path is live for
future content.
**Fix:** `RollbackCraftOutputs` — failed crafts remove every
already-added output (with a stack-merge-aware fallback: an output
that merged into an existing stack is undone by decrementing a
matching stack) and clear `crafted` before refunding.
**Test:** `Craft_BatchFailure_LeavesNoPartialOutput_AndRefundsFully`
(RED against the old code by construction: MaxWeight admits exactly
one of two outputs).

### 🟡 Finding 2 — Disassembly refunded the full build cost (FIXED; behavior change)
**File:** `TinkeringService.cs` (TryResolveDisassemblyBits)
Disassembly yielded the **entire** build cost, making
craft→disassemble lossless (bits are not a resource if every craft is
reversible for free) — and combined with any `NumberMade > 1` recipe,
each output of a batch refunded the *whole batch's* cost: a bit
printer.
**Fix:** `ResolvePartialYield` — deterministic strict subset: the
per-item share of the cost (length ÷ NumberMade, min 1) then every
other bit of that share, min 1. `"BC"→"B"`, `"BR"→"B"`,
`"BBCC"/2 made → "B"`. Deterministic on purpose: testable without
RNG, and the loss rule is legible to players.
**Behavior change, deliberately:** two existing tests pinned the full
refund; both updated with comments explaining the new contract, plus
`CraftThenDisassemble_CycleIsLossy` (economic counter-check) and
`Disassemble_MultiMadeTinkerItem_DividesYieldPerItem` (the
bit-printer counter-check).
*(Qud note: Qud also yields partial bits on disassembly; the exact
Qud formula was NOT verified against the decompile — the full
decompile is not in this container — so this is committed as a
CoO-native rule, not claimed parity. Verify-and-align later if
desired.)*

### 🟡 Finding 3 — No in-world recipe learn flow (CONTINUED: SchematicPart)
`LearnRecipe` was called from exactly three places: **bootstrap
(auto-learns every recipe at startup)**, a debug key (F9), and one
scenario. The plan's Phase 8 ("Data Disks + Learn Flow") was
unimplemented — recipes were not content, they were a login bonus.
**Continuation shipped:** `SchematicPart` — the tinkering twin of
`GrimoirePart`: a readable item with a Study action that teaches
`RecipeID` into the reader's BitLocker. Persistent by default
(Qud-disk-like); `ConsumeOnStudy` param for single-use pattern-slips.
Emits diag per the observability rule: `category=event`,
`kind=RecipeLearned` / `RecipeLearnRejected` with
`reason=NoBitLocker|UnknownRecipe|AlreadyKnown`. Zero factory wiring
needed (parts resolve reflectively by `Name`). Three shipped items:
`SchematicHonedEdge` (mod_sharp_melee), `SchematicReinforcedPlating`
(mod_reinforced_plating_armor), `SchematicDuelistCut`
(mod_duelist_cut_armor) — priced for the trade system, tagged
`Schematic` for stock/loot filtering (the player-shop design's
Family-3 goods slot straight in).
**Deliberately NOT changed:** the bootstrap grant-all. Flipping it
off is the activation step once schematics are seeded into loot
tables and merchant stock — a content decision for a Unity session,
flagged here so it isn't forgotten. Until then schematics are
redundant-but-harmless in the world.

### 🔵 Finding 4 — Grimoire re-read vs ranked spells (design question, deferred)
`GrimoirePart` blocks re-reading a known mutation
(`AlreadyKnownMessage`) even though `MutationsPart.AddMutation`
supports rank-up for `Ranked` mutations. Allowing re-read-to-rank-up
would make one persistent grimoire an infinite rank fountain; the
right shape is probably *consumable* higher-rank grimoires or a
study-cost gate. Deferred as a design decision — note that
`SchematicPart.ConsumeOnStudy` now provides the pattern to copy.

### 🔵 Finding 5 — Exception path in TryApplyModification consumes resources
If `modification.Apply` throws (not returns-false), bits and
ingredient stay consumed. The thread-static crafter context is
correctly cleaned in `finally`, so this is consistency-of-degree, not
a leak. Low priority: modifications are first-party code; noted for
a future defensive pass rather than fixed (wrapping Apply in
try/catch would swallow real bugs).

### ⚪ Finding 6 — `BitCost.Normalize` is case- and order-preserving
Fine as is (bits are case-meaningful chars), noted only because a
future "sorted display" requirement should sort at the UI layer, not
in Normalize (save-compat: normalized strings round-trip through
saves).

## III. Files changed

- MOD `Assets/Scripts/Gameplay/Tinkering/TinkeringService.cs` —
  batch rollback + partial disassembly yield (+`using System.Text`)
- NEW `Assets/Scripts/Gameplay/Tinkering/SchematicPart.cs` (+meta)
- MOD `Assets/Tests/EditMode/Gameplay/Tinkering/TinkeringServiceTests.cs`
  — 2 pins updated, 4 tests added, 1 recipe + 1 blueprint added to
  the test fixtures
- NEW `Assets/Tests/EditMode/Gameplay/Tinkering/SchematicPartTests.cs`
  (+meta) — 7 tests (learn, already-known-no-consume counter-check,
  no-BitLocker, unknown-recipe counter-check, consume-on-study,
  persistent-default counter-check, action registration)
- MOD `Assets/Resources/Content/Blueprints/Objects.json` — 3
  schematic items (75-line surgical insertion; JSON validated)

## IV. Next-Unity-session checklist

1. `refresh_unity` → `read_console types=[error]` must be empty.
2. Run EditMode: expect the tinkering suite green, including the 11
   new/updated tests above. `Craft_BatchFailure_…` and
   `Disassemble_MultiMade…` are the two that pin the fixed bugs.
3. Playmode sanity: buy/find a schematic → Study → recipe appears in
   the crafting list; `diag_query category=event kind=RecipeLearned`.
4. Decide the bootstrap flip (Finding 3) once schematics reach loot.

## V. Continuation roadmap (beyond this ship)

1. **Seed schematics** into TradeStockBuilder merchant stock and
   loot tables; then flip bootstrap to grant only a starter subset.
2. **Reverse-engineering** (plan Phase 6 back-half): disassembling an
   item with an unknown build recipe grants a chance-of-learning —
   now trivial to add on `TryDisassemble` + `LearnRecipe` + diag.
3. **Spell economics** (`ALCHEMY_SPELL_SYSTEM.md`): needs a mana/
   reserve system first; grimoire learn-costs in bits could ship
   before mana as a cheap first step (GrimoirePart + BitLockerPart
   already coexist on the reader).
4. **Charm crafting** (player-shop Family 1): a `Recipes_V2.json`
   Build-recipe family over gatherables, reusing everything fixed
   here — the batch path especially (charms are natural
   NumberMade>1 recipes, which is why Finding 1 mattered now).
