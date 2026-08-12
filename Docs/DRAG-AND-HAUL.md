# Drag & Haul — moving what you cannot carry

> **Status: design, not built.** Drafted 2026-08-11 from the user's
> brief: *let the player drag any non-living entity; whether you can is
> a function of the thing's weight and the character's Strength.*
>
> Grounded in a read of the shipped systems rather than invented on top
> of them — the weight/Strength substrate, the handling metadata, and
> the context-verb menu all already exist. Cited file:line throughout;
> anything I did not verify is flagged in §9.

---

## 1. The one-sentence design

**Carrying is what fits in your arms. Dragging is what doesn't.** A
thing you cannot pick up can still be *hauled* — slowly, noisily, and
only over ground that will let it slide.

The mechanic is worth building because it converts the world's furniture
from scenery into equipment. A barrel is currently a thing that is
somewhere. After this it is a thing you can put somewhere.

---

## 2. What already ships (verified)

| Piece | Where | Note |
|---|---|---|
| `PhysicsPart.Weight` | `PhysicsPart.cs:20` | int, on every physical object |
| `PhysicsPart.Solid`, `.Takeable` | `:15`, `:25` | the existing can-I-interact gates |
| **`HandlingPart`** | `Gameplay/Items/HandlingPart.cs:12-23` | `Weight`, `BulkClass`, **`MinLiftStrength`**, `MinThrowStrength`, **`CarryMovePenalty`**, `Carryable`, `Throwable` |
| Carry capacity | `InventoryPart.GetMaxCarryWeight()` `:414` | `Strength.Value × 15` (`WEIGHT_PER_STRENGTH`, `:24` — Qud parity) |
| Overburden | `InventoryPart.IsOverburdened()` `:427` | player-only, per Qud |
| Context verbs | `WorldActionMenuUI.Open(actor, target, cell, actions)` `:88` | the menu the drag verb joins |
| Turn energy | `TurnManager` | ⚠️ **corrected in §11** — every action costs the same fixed `ActionThreshold`; there is no per-action cost. Being slower means a **Speed penalty** |
| Per-cell dirty | `ZoneRenderHooks.MarkCellDirty` | required for any visible move |
| **`Slippery` / `Sticky`** | `LiquidDefinition.cs` | **declared, never consumed — no consumer ships** |

Two findings worth stating plainly:

1. **`HandlingPart.MinLiftStrength` already encodes "too heavy for
   you"** — the concept exists, it simply has no verb attached to it.
2. **`Slippery` and `Sticky` are dead schema.** Every liquid can already
   declare them; nothing reads them. Dragging is the natural first
   consumer, and it is a *good* one, because ice and tar changing how a
   heavy thing moves is exactly what those words mean.

---

## 3. The three weights of a thing

> ⚠️ **This section was wrong in its first draft and is corrected here.**
> It read `Strength × 15` — the **pack capacity** — as the carry/drag
> boundary. That is not the gate the game enforces on picking a single
> thing up. `PickupCommand.cs:60` and `TakeFromContainerCommand.cs:69`
> gate on `HandlingService.GetLiftStrengthRequirement`, which is
> `max(ceil(weight/2) + 1, MinLiftStrength)` for a one-handed grip.
>
> The two numbers are wildly different: at Strength 16 the pack holds
> **240** but a single lift caps near **30**. Building the drag boundary
> on the pack number would have reported "just pick it up" for every
> object the feature exists for, while the pickup path refused them —
> **objects reachable by no verb at all.** Caught by reading the pickup
> path during D1, before the verb existed to expose it.

Two different questions, and the code must ask each of them once:

```
can you lift it?   HandlingService.CanLift(actor, target)   ← shipped, delegated to
can you haul it?   weight ≤ Strength × 8                    ← new: DRAG_PER_STRENGTH
```

| Condition | Verb | Feel |
|---|---|---|
| the shipped pickup path accepts it | **Take** (ships) | it goes in your pack |
| it refuses, but `weight ≤ dragCap` | **Drag** (new) | it comes with you, slowly |
| `weight > dragCap` | *refused* | "You set your shoulder against it. It does not care." |

**Why 8.** The shipped lift ceiling works out to `2 × (Strength − 1)`,
so a drag cap of `Strength × 8` is about **4.3× the lift ceiling** — and
stays there across the whole stat range (Str 10: 18 vs 80; Str 20: 38 vs
160). A person hauls substantially more than they lift; 4× is the honest
version of that intuition at this game's weight scale, where a heavy
weapon weighs 30, not 300.

**Pack capacity is deliberately not consulted.** A pebble does not
become draggable because your bag is full — that would be an infinite-
haul exploit and a nonsense sentence. Full-pack is a different problem
with a different fix.

**The gate is `MinLiftStrength`, not weight, where a blueprint says so.**
`HandlingPart.MinLiftStrength` (`:21`) already exists to say "you need
this much Strength regardless of arithmetic" — an anvil is not hard
because it is heavy, it is hard because it has nowhere to hold. To do
independent work the value must exceed the weight-implied requirement
(`ceil(weight/8)`); below that it is inert. `SmithAnvil` is the shipped
example: weight 120 implies Strength 15, `MinLiftStrength 18` overrides.

### Scenery is not cargo

A third refusal that is not about weight: some things are *attached*.
The rule is **no `HandlingPart` and not `Takeable` → rooted**, and it
partitions shipped content exactly — of the 62 blueprints marked
`Solid`, the only ones carrying a `HandlingPart` are the six authored to
be hauled. Walls, standing trees, ore veins, chests and grass all fall
out as scenery without a single new tag.

The direction matters: **haulable is opt-in.** A future author makes a
piece of furniture draggable by giving it a `HandlingPart`, and
forgetting to fails safe (immovable) rather than dangerous (the player
drags a wall).

*(The first draft invented `Rooted` and `Fixture` tags for this. Neither
existed in any blueprint or anywhere else in the codebase — the branch
protected nothing.)*

---

## 4. Dragging is a state, not an action

The verb starts it; then it just *is* until something ends it.

```
DragPart (on the actor)      DraggedPart (on the object)
  DraggedEntityId              DraggerEntityId
```

**The follow rule.** When the dragger moves A → B, the dragged object
moves to **A** — the cell the dragger just left. It trails you. This is
the classic haul feel and it needs no pathfinding: the destination is
always a cell that was passable one tick ago.

**Cost.** ⚠️ **The formula below is superseded — see §11 correction 1.**
There is no per-action cost in `TurnManager` to multiply: every action
deducts the same fixed `ActionThreshold`. Being slower is expressed as a
**Speed penalty**, the way carried weight already does it
(`InventoryPart.cs:350-362`). D4 mirrors that. The intent survives
unchanged — a light crate barely slows you; a Salt-Cured body is a
decision — only the mechanism moves.

~~```~~
~~ratio    = weight / carryCap~~
~~moveCost = baseCost × (1 + ratio)       // clamped to ≤ 3× base~~
~~```~~

**It ends when:** you let go (the verb toggles), the object can't follow
(the cell you left got occupied), you change zone, you're stunned or
knocked back, or you die. **Not** on taking damage — being shot while
hauling a body and choosing to keep hauling is a good moment.

---

## 5. The ground matters (activating dead schema)

This is the part that makes it a mechanic instead of a chore, and it
costs almost nothing because the fields already exist.

| Ground | Effect on drag | Source |
|---|---|---|
| **Slippery** (ice, oil, gel) | cost ×0.5 — heavy things *slide* | `LiquidDefinition.Slippery` |
| **Sticky** (tar, honey, resin, sundew mucilage) | cost ×2 — or refuse outright above a weight | `LiquidDefinition.Sticky` |
| Water / mire | cost ×1.5 | `bog-mire` already ships |
| Otherwise | ×1 | |

Two consequences fall out for free, and both are the kind of thing
players tell each other about:

- **Freeze a puddle to move something you otherwise couldn't.** The tile
  layer already turns water → ice via `freeze_water`
  (`Reactions.json`). A cryomancy spell becomes a *logistics* tool.
- **Oil your route.** `Pyromancy_Oilmark` already paints oil. It is a
  fire-starter today; it becomes a sled-track too — and a fire hazard
  you are now standing in the middle of, dragging something.

---

## 6. What it's for (why the world wants this)

Not a physics toy — three shipped or planned systems need it:

1. **Body-courier contracts** (Felling W3, `FELLING-WORLD-DESIGN.md`
   §3.2). Canon has the player recovering Bog-Taken from peat and
   delivering them "sealed to Curation intake standard". A body you
   cannot pocket needs a haul verb. `IDEAS.md` is explicit that
   preserved bodies are heavy — the Yellowfoot Wayfarer tortoise is
   described as a *funeral wagon*.
2. **Furniture as tactics.** Drag a barrel into a doorway; pull a brazier
   onto oil; haul a Spore-Block panel to wall off a Bloom front (W4).
3. **The Preserved.** Canon's five preservation methods produce
   *objects that were people*. Dragging one is meant to feel like
   something. The Bower-Folk want it beautiful; the Catchers want it
   preserved; you want it out of the rain.

---

## 7. Sub-milestones (TDD, smallest blast radius first)

**D1 — the arithmetic.** `DragRules` static: `CanDrag(actor, target)`
returning an enum result (`Ok`, `TooHeavy`, `NotStrongEnough`, `Alive`,
`Rooted`, `NoTarget`). Pure function, no state, no zone. Tests: the
three weight bands, the `MinLiftStrength` gate, **the living-thing
refusal**, boundary at exactly `dragCap`, Strength 0/negative.

**D2 — grab and release.** `DragPart`/`DraggedPart`, the toggle verb,
adjacency check, `drag/Grabbed` + `drag/Refused{reason}` diag on both
branches. Tests incl. counter-checks: can't grab at range, can't grab
two things, releasing clears both sides.

**D3 — the follow rule.** Hook the dragger's move; object moves to the
vacated cell; `MarkCellDirty` both cells. Tests: follows on move,
doesn't teleport, breaks cleanly when the vacated cell is occupied,
survives a diagonal.

**D4 — cost and ground.** ~~Move-cost multiplier~~ → **a Speed penalty
while hauling** (see §11 — there is no per-action cost to multiply);
Slippery/Sticky/water lookups. This is where the dead schema wakes up.
Tests: the penalty scales with weight, applies on grab and lifts on
release, ice halves it, tar doubles it, the floor holds.

**D5 — the edges.** Zone transition, stairs, death, stun, knockback,
save/load round-trip of both parts. **Save is the risk** — a dragged
object is a cross-entity reference, and the save graph has form here
(`ISaveSerializable` for anything beyond public scalars).

**D6 — adversarial sweep.** Five taxonomy surfaces apply (cross-actor
state, atomicity, save reach, boundary inputs, anti-exploit), so the
dedicated gate is mandatory, not optional. Specifically probe: drag a
thing into a wall; drag while overburdened; two actors grab one object;
drag an object that dies/despawns mid-haul; drag onto a stairs cell;
drag into a zone edge; drag a container that is *also* a shop's stock.

---

## 8. Open decisions (mine to recommend, yours to overrule)

1. **Can NPCs drag?** Recommend yes for the rules, no for the AI in v1 —
   the check is actor-agnostic, but no goal drives it yet. Catchers
   hauling an unconscious player somewhere is obvious later content.
2. **Does dragging make noise?** Recommend yes, eventually — it fits
   the catacomb quiet-etiquette canon perfectly. Not v1.
3. **Corpses specifically.** They're non-living, so they qualify
   automatically. Recommend leaning in: it's the strongest use case.
4. **Weight rebalance.** Current content maxes at **40** and is mostly
   1–10 — so at Strength 16 (`carryCap 240`) *nothing that ships is
   too heavy to carry*. **Dragging is inert until heavy things exist.**
   D1 should land with a handful of genuinely heavy blueprints (a
   barrel, an anvil, a millstone, a Salt-Cured body) or the feature has
   nothing to bite on. This is the single most important item here.

---

## 9. Implementation log

### D1 — the arithmetic ✅ shipped

`Assets/Scripts/Gameplay/World/DragRules.cs` + 19 tests in
`Assets/Tests/EditMode/Gameplay/World/DragRulesTests.cs`, plus the six
heavy blueprints §8.4 said the slice could not ship without.

**Divergences from the §7 sketch (all deliberate):**

| Planned | Shipped | Why |
|---|---|---|
| verdict `Alive` | `Living` | reads better in the refusal string, and `Alive` invites confusion with an HP check |
| — | `NoActor` / `NoTarget` split | a null actor and a null target are different bugs upstream; one enum value would have hidden which |
| — | `CarryInstead` | "too light to drag" is not a refusal — the right response is to offer *take*. Without this branch the only honest reply to a pebble would have been "you cannot drag that", which is true and useless |
| `dragCap = Strength × 45` | **× 8** | see §3 — 45 was scaled against the wrong carry number |
| `Rooted`/`Fixture` tags | `HandlingPart`-absent + `!Takeable` | the tags did not exist in any blueprint; the shipped markers partition the content exactly |

**Corrections table (the sweep, run mid-slice rather than before it):**

| Believed | Actually | Consequence |
|---|---|---|
| carry gate is `Strength × 15` | that is *pack capacity*; the per-item gate is `ceil(w/2)+1` (`PickupCommand.cs:60`) | 🔴 would have made every heavy object unreachable by any verb — see §3 |
| "34 blueprints author `Handling`, none author both weights" | **0** author `Handling.Weight` alone, 119 author `Physics.Weight` alone, and 7 author both (`Bone` + the six new) | the "which weight wins" question is real but nearly untested by content; `Bone` is the only pre-existing case and both its values agree |
| no shipped weight resolver | `HandlingService.GetWeight` already exists and is used by `InventoryPart` and `ThrowItemCommand` | `WeightOf` had been a verbatim reimplementation; now delegates |
| `Rooted`/`Fixture` are meaningful tags | neither appears in any blueprint or anywhere in `Assets/Scripts` | the refusal branch protected nothing |

**The six heavy things** (`Objects.json`), rescaled to this game's actual
weight scale (a heavy weapon is ~30, not ~300):

| Blueprint | Weight | MinLift | Needs Strength |
|---|---:|---:|---:|
| FallenBeam | 60 | — | 8 |
| HaulBarrel | 75 | — | 10 |
| SaltCuredBody | 90 | — | 12 |
| StoneCoffer | 110 | — | 14 |
| SmithAnvil | 120 | **18** | 18 *(grip, not weight)* |
| MillStone | 150 | — | 19 |

At the shipped Strength 16 that is four haulable, two aspirational, and
the two refusals arrive for visibly different reasons. All six are
`Carryable: false`, so they are never mistaken for pack contents.
**They are placed in no worldgen yet** — D2 or later; until then they
exist for tests and the console only.

**Self-review (§5):**

> 🔴 **Finding 1 — the carry rung asked the wrong question.** Detailed in
> §3. `CanDrag` compared weight against pack capacity, so at Strength 16
> a 150-weight beam returned `CarryInstead` while `PickupCommand`
> refused it at `ceil(150/2)+1 = 76`. Net effect: **the objects the
> feature exists for were reachable by no verb at all.** Fixed by
> delegating to `HandlingService.CanLift` — the same call the pickup
> path makes — and pinned by
> `CarryInstead_TracksTheRealPickupGate_NotPackCapacity`, which asserts
> both preconditions explicitly so it cannot pass vacuously.
>
> Worth naming as a pattern: this bug lived *between* two correct units.
> `InventoryPart` is right about packs, `HandlingService` is right about
> lifts, and the defect was reading one and meaning the other. Both
> per-file reviews would have passed it.
>
> 🟡 **Finding 2 — a test asserted a branch it never reached.**
> `MinLiftStrength_GatesIndependentlyOfWeight` first used a 200-weight
> anvil, which the stronger actor could simply *carry* — so the passing
> assertion hit `CarryInstead` and never evaluated the gate. Caught by
> the run, not by reading. **Same vacuous-precondition class as the W0.2
> counter-check**, twice in this stream, so: *a fixture chosen for the
> fiction rather than the arithmetic can pass through a branch it did
> not mean to test.* Every boundary fixture in the file now states its
> arithmetic in a comment.
>
> 🟡 **Finding 3 — `WeightOf` was a copy of `HandlingService.GetWeight`.**
> Identical logic, independently written, which is exactly how hauling
> and carrying end up disagreeing about how heavy a thing is after a
> future edit to one of them. Now a delegation, with a test asserting
> the two agree.
>
> 🔵 **Finding 4 — refusal precedence is unstated design.** A thing both
> too heavy and too awkward reports `TooHeavy`. That is deliberate — it
> is the refusal the player can answer by levelling — but it was
> implicit in branch order until `WeightRefusalOutranksTheGripRefusal`
> pinned it.
>
> 🧪 **Finding 5 — no diag records.** D1 is a pure function with no call
> site, so there is no gate to instrument yet. The observability rule
> lands in D2, where `drag/Grabbed` + `drag/Refused{reason}` carry the
> verdict enum as the reason field — which is why the enum is
> per-branch rather than a bool.

---

## 10. Honesty bounds

- **Only D1 is built.** D2–D6 below the arithmetic are unwritten; the
  rest of this document is a design read against the code, not a
  measured feature.
- **D1 is unit-verified only.** 6496/6496 EditMode green. No live Play
  run — there is nothing to play yet, since no verb calls into
  `DragRules` and the heavy blueprints are not placed in any zone.
- **Verified:** the weight/Strength formula and constant, `HandlingPart`'s
  fields, `PhysicsPart`'s fields, the world-action menu signature, and
  that `Slippery`/`Sticky` have no consumer. Each is cited above.
- **NOT verified:** exactly how move cost is computed in `TurnManager`
  (I know actions cost energy; I have not read the per-move accounting),
  and whether `WorldActionMenuUI`'s `InventoryAction` enum can carry a
  new verb without a UI change. Both are D2/D4 verification-sweep items
  — the kind of assumption this project's sweep step exists to catch.
- **The 3× drag multiplier and the ground multipliers are invented**
  and unplayed. They are legible starting numbers, not tuned ones.

---

## 11. Verification sweep for D2–D4 (run before writing D2)

Five subsystems read in parallel, each report then re-read by a second
pass told to falsify it. The falsification pass earned its keep: it
overturned claims in four of the five reports, including two that would
have sent D2 down the wrong path.

### 🔴 Corrections that change the design

| # | Believed | Actually | Consequence |
|---|---|---|---|
| 1 | Drag costs "more turn energy" — multiply the move cost | **There is no per-action cost.** `TurnManager.EndTurn` takes no cost parameter (`:361`) and `SpendEnergy` always deducts a fixed `ActionThreshold = 1000` (`:422-427`). Nothing anywhere carries an action cost — a repo-wide grep for `EnergyCost\|TurnCost\|ActionCost\|MoveCost` finds only TurnManager's own private members | **§4/§5's `baseCost × (1 + weight/carryCap)` is unimplementable as written.** The shipped way to be slower is a **Speed penalty**, and there is already a precedent doing exactly this for weight: `InventoryPart.cs:350-362` does `speed.Penalty += delta` from carried weight. D4 mirrors it. Armor (`EquipBonusUtility.cs:44-54`) and a tinker mod do the same |
| 2 | The six haulables were fine as `Terrain` children | `WorldInteractionSystem.ResolveTarget` (`:45-63`) returns the highest-layer **non-terrain** entity and only falls back to terrain when the cell holds nothing else; `IsTerrain` is tag-based (`:237-243`) and `Terrain` carries that tag | **The drag verb would have been unreachable** on a millstone sharing a cell with any loose item. Also: `Terrain` sets `RenderLayer 0` (the floor band), and `VillageBuilder.cs:300` reads the Terrain tag as *"a floor exists here"* — a barrel would have counted as flooring. Fixed: all six now inherit `PhysicalObject` at layer 1, matching `Chest`/`Crate`/`Urn`/`Pillar`/`Well`/`Bookshelf`/`Tree`. Pinned by `HaulableContentTests` |
| 3 | D3 must hand-roll the follow rule | **`SkillCombatHelpers.DragAlong` already exists** (`:278`), used by `TryPush` (`:203`) and `TryPull` (`:237`), routing through `MovementSystem.ForceMoveTo` and already implementing the destination guards (solid / other creature / zone edge / stop-before) | D3 should reuse or mirror this rather than write a fifth copy. `ForceMoveTo`'s own docstring points at it (`MovementSystem.cs:175`) |
| 4 | `Part.Initialize()` runs on load | It does **not**. `Entity.AddPart` calls it (`Entity.cs:48`), but `LoadEntityBody` bypasses `AddPart` entirely — `SaveSystem.cs:772-773` does `part.ParentEntity = entity; entity.Parts.Add(part);` | Anything D2's parts set up in `Initialize()` silently does not happen for a loaded game. Put it in `OnAfterLoad`/`FinalizeLoad`, or call it from both |

### 🟡 Constraints to design around

- **No adjacency gate exists on the world action menu.** `OpenWorldActionMenu`
  (`InputHandler.cs:2197-2232`) checks only that the cell and target are
  non-null. Verbs that need proximity self-gate — `ForgePart.IsNearForge`
  (`:287-319`) is the template. D2's grab must do its own adjacency check.
- **`GetInventoryActions` carries `Actions` and `Actor` — not `Zone`**
  (`WorldInteractionSystem.cs:96-98`). A declaration-time gate can only
  see the actor; `Zone` arrives later on the `InventoryAction` event
  (`InputHandler.cs:2435`). `SeedPart.cs:33-44` is the worked example.
- **Routing is on the `command` string, not the action name.**
  `ExecuteWorldActionSelection` is an if-chain of special cases followed
  by a generic dispatch of an `InventoryAction` event onto the target
  (`InputHandler.cs:2426-2436`). A Part-declared verb needs no
  InputHandler change **unless** it needs UI (Throw is special-cased at
  `:2411` precisely because it needs an aiming popup). Grab does not.
- **`InventoryAction.fireOnActor` is dead on the world path** —
  `ExecuteWorldActionSelection` never reads it and always fires on the
  target. Do not rely on it.
- **Free hotkeys: `g`, `i`, `l`, `m`, `n`, `v`, `w`, `y`, `z`.** Note `j`
  and `k` are swallowed by the menu's own cursor nav before the hotkey
  scan (`WorldActionMenuUI.cs:161,172`), and the scan lowercases
  (`:242`), so `F` is unreachable behind `f` — a live collision already
  hides `ForgePart`'s ForgeBatch. **D2 takes `g`.**
- **Entity-typed fields DO round-trip**, by ID, via `WriteEntityReference`;
  identity across fields is pinned in `SharedReferenceIdentityTests.cs`.
  So `DragPart` may hold a real `Entity`. But `CanSerializeType` does
  **not** recurse into generic collection element types
  (`SaveSystem.cs:1790-1793`), so a `List<Entity>` is a trap — keep it
  to a single reference.
- **`FireCellEnteredEvents` has a cell-change-only guard**
  (`MovementSystem.cs:279-282`): a move whose source and target coords
  match fires nothing. Relevant to any "re-place in the same cell" path.
- **`MovementSystem.ForceMoveTo` has zero test coverage** — `grep -rn
  "ForceMoveTo" Assets/Tests` returns 0 hits. D3 will be the first thing
  pinning it.

### Honesty bound on this sweep

Every 🔴 above was verified by me directly against the cited source, not
taken on the agents' word — the Terrain/ResolveTarget chain and the
`EndTurn` signature especially. The 🟡 list is reported as the verifiers
left it: each item carries a citation, but I re-read only the ones D2
depends on immediately (adjacency, hotkeys, command routing). The
save/load and turn-cost details matter to D4/D5 and should be
re-confirmed at the top of those slices rather than trusted from here.

---

## 12. D2 — grab and release ✅ shipped

`DragSystem` + `DragPart`/`DraggedPart` + `DragMessages`, the haul verb on
`HandlingPart`, and 23 tests in `DragGrabTests.cs`.

**The link is two-sided.** `DragPart` on the hauler names the load;
`DraggedPart` on the load names the hauler. Both, because the game asks
from both ends — *"what am I dragging?"* when the hauler moves (D3), and
*"is anyone holding this?"* when something happens to the load (D5). A
one-sided link turns half of those into a zone scan.

**Presence of the Part IS the state.** No `IsDragging` bool to drift out
of sync; `Release` removes the Parts rather than nulling their fields.

**Atomic by construction.** `Evaluate` runs every gate before a single
Part is attached, so a refused grab leaves nothing on either entity —
pinned by `ReachingAcrossTheRoom_IsRefused_AndLeavesNothingBehind`. The
alternative (attach, validate, unwind) is how a hauler ends up linked to
a millstone three rooms away after an edge case nobody tested.

**Gate order is a design decision, not an accident.** What the thing IS
(the D1 verdict) is checked before where it is, and where it is before
who is busy. So a creature across the room reports `Living`, not
`NotAdjacent` — because walking closer will not help. The verdict the
player sees is always the one they can act on.

**Adjacency is enforced at execution, not declaration.** The
`GetInventoryActions` event carries `Actions` and `Actor` but no `Zone`
(§11), so reach cannot be evaluated when the menu row is built.
`DragSystem.TryGrab` has the zone and enforces it — the same split
`SeedPart` uses for "is this seed carried?".

**Verdicts added:** `NotAdjacent`, `HandsFull`, `TakenByAnother`. They
share `DragVerdict` with D1's answers so the message log and the diag
payload have one vocabulary for "why not", but `DragRules.CanDrag` never
returns them — they are properties of the world at a moment.

**Observability:** `drag/Grabbed`, `drag/Refused{reason}`,
`drag/Released`. `drag` added to `Diag.DefaultOnCategories`. A no-op
release emits nothing, pinned by `ReleasingNothing_EmitsNothing` — a
trace full of phantom records stops meaning anything.

### Self-review (§5)

> 🟡 **1 — `Refused` payload omitted the blueprint name.** Found by the
> cold-eye Q2 pass (cross-feature payload consistency): `Grabbed`
> carried `blueprintName`, `Refused` did not. "Why did hauling the
> millstone fail?" is exactly the query this record exists for, and it
> could not name the millstone. Added.
>
> 🟡 **2 — an unreachable branch had no test.** `Release` deliberately
> refuses to clear the far side when the load's `DraggedPart` names
> somebody else. Nothing can reach that today. Pinned anyway by
> `ReleasingDoesNotStealBackALoadSomeoneElseNowHolds`, because the day
> a transfer path exists, that test states what the contract was.
>
> 🔵 **3 — `Released` carries a thinner payload** than the other two
> (blueprint name only). Deliberate: weight and capacity answer "could
> this happen", which is not a question letting go asks.
>
> 🧪 **4 — save/load is untested for both Parts.** They hold `Entity`
> fields, which §11 established do round-trip by ID via
> `WriteEntityReference`. Not verified here. **D5 owns this**, and it
> is the slice's stated risk.

### Honesty bounds

- **Unit-verified only.** 6531/6531 EditMode green. No live Play run —
  the verb is reachable in the menu but nothing has hauled anything in
  a running game yet, and **the load does not follow the hauler until
  D3.** Grabbing something today links it and then it sits there.
- The six heavy blueprints are still **placed in no worldgen**, so the
  verb is unreachable in normal play. D3/D5 or a later content pass.
