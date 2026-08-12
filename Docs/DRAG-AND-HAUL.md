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
| Turn energy | `TurnManager` | actions cost energy; slow actions cost more |
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

One rule, read off Strength, producing three different verbs:

```
carryCap = Strength × 15          (shipped: WEIGHT_PER_STRENGTH)
dragCap  = Strength × 45          (proposed: DRAG_PER_STRENGTH = 45)
```

| Condition | Verb | Feel |
|---|---|---|
| `weight ≤ remaining carry capacity` | **Take** (ships) | it goes in your pack |
| `weight > capacity` but `≤ dragCap` | **Drag** (new) | it comes with you, slowly |
| `weight > dragCap` | *refused* | "You set your shoulder against it. It does not care." |

**Why 3×.** A person can drag substantially more than they can carry —
that is the entire physical intuition the feature trades on. 3× keeps
the boundary legible: if you can carry a thing at Strength 10, you can
drag it at Strength 4.

**The gate is `MinLiftStrength`, not weight, where a blueprint says so.**
`HandlingPart.MinLiftStrength` (`:21`) already exists to say "you need
this much Strength regardless of arithmetic" — an anvil is not hard
because it is heavy, it is hard because it has nowhere to hold. Drag
checks weight; a blueprint may additionally require Strength.

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

**Cost.** Dragging multiplies move cost by how overmatched you are:

```
ratio    = weight / carryCap            // 1.0 = a full pack's worth
moveCost = baseCost × (1 + ratio)       // clamped to ≤ 3× base
```

So a light crate barely slows you; a Salt-Cured body is a decision.

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

**D4 — cost and ground.** Move-cost multiplier; Slippery/Sticky/water
lookups. This is where the dead schema wakes up. Tests: cost scales
with ratio, ice halves it, tar doubles it, cap holds at 3×.

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

## 9. Honesty bounds

- **Nothing is built.** This is a design read against the code, not a
  measured feature.
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
