# Object interaction & destructibility — plan

> Five issues raised from play, 2026-08-12. Two are small render bugs,
> one is an input affordance, and two are real systems that change what
> the world is made of. Ordered by blast radius, smallest first.
>
> Status marks: ✅ shipped · 🔨 in progress · 📋 planned

---

## 1. ASCII ghost trails over sprite tiles ✅ (feature removed)

**Symptom.** Walking over a walkable ground sprite — bush, copper pipe,
peat bog — leaves the player's ASCII glyph on the tile, fading over a
few ticks. Also seen with NPCs.

**Mechanism.** `GlyphGhostRenderer` is a *deliberate* motion-trail
feature (Pass 6 §6A). Each frame it captures whatever the **main
tilemap** shows at an entity's current cell, and when the entity moves
it paints that captured tile at the vacated cell, fading it out.

Its own comment says a sprite-rendered actor should produce no ghost:

> *"When the sprite pass claimed the cell (actor sprite → main is null)
> there is no glyph to ghost: Tile stays null and SpawnGhost skips."*

**That reasoning is defeated by pass ordering.** In
`ZoneRenderer.RenderZone` the ghost pass runs at ~`:902` and
`EnvironmentSpriteRenderer` at `:914`. The ghost captures the main
tilemap *before* the sprite pass nulls it. So the captured tile is the
ASCII glyph, and a sprite-rendered entity leaves an ASCII ghost —
exactly the reported smear. It is most visible over ground sprites
because there the ghost is an ASCII character sitting on a painted
tile rather than on more ASCII.

**Fix options.**

| | Approach | Cost | Risk |
|---|---|---|---|
| A | Move the ghost pass *after* the sprite pass | small | reorders a render pipeline with known ordering assumptions elsewhere |
| B | Ghost only when the entity genuinely renders as a glyph — ask the sprite layer whether it will claim this entity, rather than inferring from a tilemap that has not been written yet | small | needs a query the sprite renderer may not expose |
| **C** | **Ghost the entity's sprite when it has one, its glyph when it does not** | medium | best result: trails work for everyone |

**Shipped: B.** `EnvironmentSpriteRenderer.WillRenderAsSprite` is now
public and `GlyphGhostRenderer` asks it before spawning. The renderer
could not infer this from the tilemap — the ghost pass runs at
`ZoneRenderer:907` and the sprite pass at `:915`, so at capture time the
tilemap still holds the glyph.

### ⚠ A SECOND cause, found by survey and NOT yet fixed

`PostRender` is the ghost renderer's **only clock** — there is no
`Update`. And `ZoneRenderer` calls it **only from the full-redraw path**,
which fires once per **player move** (`MovementSystem.cs:226`
`MarkFullDirty("Move.Player")`). So a lifetime written as "6 frames ≈
100ms" is really **6 player turns**, and a fresh ghost sits at 100% alpha
until you take your next step.

That explains the reported "fades over the course of a few **ticks**" —
ticks, not frames, exactly as described. It also means NPC moves never
tick the decay at all (they use the dirty path), so an NPC ghost can sit
at a stale position reading as a duplicate monster.

### Resolution: the feature is gone

Asked whether the trail should decay properly or be dropped, the answer
was that the ghost should not appear at all. So `GlyphGhostRenderer`,
its `ZoneRenderer` wiring (5 sites) and its two test files are deleted
rather than left disabled — a fully-tested renderer that nothing calls
is dead code, and the sprite-gate from the earlier partial fix was only
ever a workaround for a feature that is no longer wanted.

The `WillRenderAsSprite` query added to `EnvironmentSpriteRenderer` for
that gate is **kept**: "will this entity draw as a sprite?" is a
generally useful question about the render pipeline, and it is the only
correct way to ask it (the tilemap cannot answer it before the sprite
pass runs).

---

## 2. Brine pool still scrolling ✅

Shipped. My first gate allowed any `LiquidPoolPart` through, and
`BrinePool`, `PeatBog`, `AcidPool` and `TarSeep` all carry it while
rendering as a bare `~` glyph. Scrolling a *letter* is the bug the gate
exists to stop.

Water animation now requires the cell to resolve `GroundMaterial.Water`
— i.e. to have a real water tile to scroll. `WaterPuddle` is currently
the only blueprint that does. The rest are static until they have
sprites of their own.

---

## 3. `c` as a general interact key ✅

**Want.** Press `c`, then a direction, and the world-action popup opens
for whatever is that way — barrel, tree, hedgerow, basket, dog, sword on
the ground — offering examine / take / throw / haul / talk / whatever
that thing supports. Today `c` only talks to NPCs.

**What already exists.** Nearly all of it. `WorldInteractionSystem.GatherActions`
fires `GetInventoryActions` on a target and returns the action list;
`WorldActionMenuUI.Open` displays it; `InputHandler.OpenWorldActionMenuFor`
wires the two together. Look-mode Enter and left-click already reach it.
Chat is just one row in that menu (`ConversationPart`, hotkey `c`).

**So the work is an input mode, not a feature.** Add a pending-direction
state: `c` arms it, the next direction key resolves the adjacent cell,
`WorldInteractionSystem.ResolveTarget` picks the entity, and the
existing menu opens. Escape cancels.

**Design decisions to make:**
- **`c` on its own cell.** Pressing `c` then `.` (or `c` twice) should
  target the cell you are standing on — that is how you reach an item
  you are standing on without opening the full pile picker.
- **Nothing there.** Direction with an empty cell should say so and
  cancel, not open an empty menu.
- **Chat is NOT special-cased.** Decided: `c` always opens the menu,
  even for a talkable NPC. `ConversationPart` already contributes a
  "chat" row, so talking is one row among the rest — a deliberate extra
  keystroke, in exchange for one key that reaches everything.

**Shipped.** The mode already existed (`InputState.AwaitingTalkDirection`,
armed by `c`, message "Interact — choose a direction"); it just resolved
through a hardcoded talk-then-container ladder that could reach exactly
two verbs and ignored a sword on the floor. It now resolves through
`WorldInteractionSystem.ResolveTarget` into the same
`OpenWorldActionMenuFor` the look-mode cursor uses, with the
"<< everything here" row for cells holding several things.

**Risk.** `InputHandler` is large and modal state is scattered. The
throw-aim popup is the closest existing precedent for "suspend normal
input, wait for a targeting decision" and should be the template.

---

## 4. Destructible objects 📋

**Want.** Attack anything — wall, tree, hedgerow, barrel, chest — and
destroy it at 0 HP. Contents spill onto the ground where it stood.
Qud's behaviour: break a wall to make a shortcut or escape.

**This is the biggest of the five** and needs its own sub-milestones.

### 4.1 Give objects HP

Most objects have no `Hitpoints`. Options: a `DestructiblePart` holding
HP + material hardness, or plain `Hitpoints` stats on blueprints.

**Qud puts HP on the stat and the flags on Physics** — `Physics` owns a
packed bitfield (`Solid`, `Takeable`, `IsReal`, `Organic`, `WasAflame`,
`WasFrozen`) plus temperature thresholds, while HP lives in
`Statistics["Hitpoints"]` with a `Penalty` channel
(`GameObject.cs:1177-1208`).

**Still prefer a Part here**, because CoO's `Hitpoints` is read in
creature-shaped places and a `DestructiblePart` carries what a bare
number cannot: what it drops, what replaces it, and whether it may be
destroyed at all.

### 4.2 Let attacks reach non-creatures

Find and relax whatever gate currently requires a creature. The bump
path is the primary entry: walking into a solid object should offer or
perform an attack rather than silently refusing.

**Risk — this is the one to be careful about.** `ApplyDamage` and the
death path are creature-shaped: corpses, XP, faction reputation, the
`Died` event with its listeners. Routing a barrel through that path
unchanged would try to give it a corpse and award XP for a fence.

### ⚠ What Qud actually does — and where my plan was wrong

Read from `/Users/steven/qud-decompiled-project/`. **Qud has ONE
hitpoint-depletion path, not two**, which is the opposite of what §4.2
assumed:

```
damage → Physics.ProcessTakeDamage        (Physics.cs:3301)
       → stat.Penalty += amount
       → StatChangeEvent("Hitpoints") → Physics.CheckHP
       → hp <= 0 → GameObject.Die(...)          ← creature AND chest AND wall
                    ├ BeforeDieEvent.Check           ← veto point 1
                    ├ AfterDieEvent.Send
                    ├ KilledEvent / AwardXPTo
                    ├ BeforeDeathRemovalEvent        ← inventory + corpse drop
                    ├ DeathEvent
                    └ Destroy(...)
                         ├ IsInGraveyard() → return   (idempotent)
                         ├ BeforeDestroyObjectEvent.Check  ← veto point 2
                         ├ OnDestroyObjectEvent.Send       ← not vetoable
                         └ Physics.TeardownForDestroy()
```

`Die()` is shared; the creature/object difference is handled by
**branches inside it** and by `IsCreature` checks at the points that
matter. `Destroy()` is the lower-level **removal primitive**, callable
directly (Obliterate, ReplaceWith, Explode, ~240 sites) — and calling it
directly fires **no death events at all**.

There is no `AfterDestroyObjectEvent` and no `ObjectDestroyed` event.
Only `BeforeDestroyObjectEvent` (vetoable) and `OnDestroyObjectEvent`
(notification).

**Destructibility is OPT-IN, not opt-out.** Qud gates on a `Breakable`
tag/property:

```csharp
// XRL.World.Effects/Broken.cs:41-48
if (!Object.HasTagOrProperty("Breakable")) return false;
```

and applies a `Broken` effect at ≤25% HP, **only for non-creatures**:

```csharp
// Physics.cs:4236-4239
if (CurrentHP <= (MaxHP ?? baseHitpoints) / 4 && !ParentObject.IsCreature
    && ParentObject.HasTagOrProperty("Breakable"))
    ParentObject.ForceApplyEffect(new Broken(FromDamage: true));
```

### Reconciling this with "build a separate path"

The instruction and Qud's design are compatible, because **Qud's
`Destroy()` IS the object path**. The port should therefore be:

- **`DestroyObject(entity, cause)`** — mirrors Qud's `Destroy()`. No
  corpse, no XP, no faction rep, no `Died`. Fires a vetoable
  `BeforeDestroy` and a non-vetoable `Destroyed` notification. This is
  what objects use.
- **The existing creature death path stays untouched.** Nothing about
  NPC death changes.
- **Damage itself can be shared** — it is only the *consequence of
  reaching zero* that must fork. That is a smaller change than
  duplicating the damage pipeline, and it is what Qud does.

**Opt-in, per Qud.** A thing is breakable because its blueprint says so.
That answers the indestructible-staircase requirement from the other
direction and more safely: anything not explicitly marked is immune by
default, so a staircase is protected by omission rather than by
remembering to flag it. An explicit `Indestructible` marker is still
worth having for things that ARE breakable-shaped but must never break
(a quest door), and the register test still applies.

### 4.3 Destruction consequences

- Contents spill to the cell (`ContainerPart` → drop each item).
- The entity is removed; the cell stops blocking.
- A rubble/stump/wreck entity may replace it — a broken wall should
  read as broken, not as pristine floor.
- `MarkCellDirty` both for the visual.
- Diag: `damage/ObjectDestroyed { blueprint, by, dropped }`.

### 4.4 What must NOT become breakable

Zone-edge walls, stairs, quest-critical fixtures. A player who breaks
the only staircase has softlocked. This needs an explicit
`Indestructible` marker and a test that the world's structural pieces
carry it.

---

## §4 as shipped ✅

`DestructiblePart` (structural HP, `Hardness`, `WreckageBlueprint`,
`Indestructible`) + `DestructionSystem` (`IsBreakable` / `Damage` /
`Destroy` / `RouteDamage` / `ComputeStructuralBlow`). 47 tests.

**Two ways in.** Bumping a breakable obstacle swings at it —
that is the shortcut-making case, and it only fires on things that
were already blocking the move. The interact key (`c`) contributes a
**Break** row at priority 5, for the barrel you could simply walk
around; deliberately below `Open` (30) so smashing a chest is never
what the cursor lands on when opening it is available.

**Damage.** `ComputeStructuralBlow` is weapon dice + the wielder's stat
modifier, floored at 1. No to-hit roll, no penetration ladder, no hit
location — a barrel has no dodge value and no armour, and `Hardness`
stands in for AV. Hardness never absorbs a hit entirely, so nothing is
unbreakable by arithmetic; that is what `Indestructible` is for.

**The register** — `DestructibleRegisterTests` asserts it against the
real blueprint file, in both directions:

| | HP | Hardness | Leaves |
|---|---|---|---|
| `Wall` (+ inheriting stone walls) | 28-70 | 2-6 | `Rubble` |
| `Pillar` | 50 | 4 | `Rubble` |
| `Hedge` / `Bush` / `BerryBush` | 6-10 | 0 | — |
| `Tree` | 30 | 1 | — |
| `IceWall` / `VineWall` | 14-18 | 0 | — |
| `WoodenBarrel` / `HaulBarrel` / `Crate` | 8-10 | 0 | — |
| `Chest` / `LockedChest` / `LockedDoor` | 14-22 | 1-2 | — |

`Rubble` is walkable, so breaking a wall genuinely opens the path.
**`Tree` leaves nothing** — the obvious `HollowStump` is `Solid`, so
using it as wreckage would keep the cell blocked and make felling the
tree pointless. There is a test for that specifically.

**Staircases are safe by omission**, which is the direction that
survives someone forgetting. `StairsDown`/`StairsUp` have no
`DestructiblePart`, and `IsBreakable` returns false for anything
lacking one. `Indestructible` exists on top of that for things which
need the Part's other behaviour but must never break.

**`MimicChest` is excluded** because it inherits `Creature` — it looks
like the most breakable thing in the game and must not be, or killing
one would skip its XP and loot.

### §4/§5 self-review (Methodology Template §5)

Three findings, all fixed before commit.

**🟡 1 — the Break row could reach across the whole map.**
`InputHandler.ExecuteWorldActionSelection`. The row is contributed by
`DestructiblePart` and dispatched without any distance test. The bump
path is adjacent by construction so this was invisible there, but the
same menu opens from **look mode**, whose cursor is unbounded — so the
player could point at a wall on the far side of the zone and demolish
it a swing at a time.
**Fixed:** `DestructionSystem.IsWithinStrikeReach` (adjacent, diagonals
count), checked in the Break branch, with an "out of reach" message
rather than a silent no-op. Two tests.

**🟡 2 — the burning message reported the wrong number on objects.**
`BurningEffect.OnTurnStart` read `fireDmg.Amount` back after the call,
which is correct on the creature path because `ApplyResistances`
mutates it in place — but the object path subtracts `Hardness` inside
`DestructionSystem` and never touches the `Damage` object. A burning
stone-adjacent object would report the full pre-hardness roll. This is
the same lying-message bug class the surrounding comment block already
exists to document a fix for.
**Fixed:** report `RouteDamage`'s return value.

**🟡 3 — "You strike the hedgerow." once per turn while it burned.**
`DestructionSystem.Damage` narrated unconditionally, but it is also the
per-turn damage path for anything on fire, and for NPC-caused damage.
**Fixed:** narrate only when the source is the player.

**🔵 Deferred — `PickupCommand` has no reach check either.** Found while
checking finding 1: the "Take" row has the same unbounded-from-look-mode
property, and predates this work. Not fixed here because it is a
different feature's contract and changing it needs its own tests.

### Divergence from §4.2 — damage is NOT shared

§4.2's reconciliation proposed sharing the damage pipeline and forking
only at the "reached zero" point, on the grounds that this is what Qud
does. As built, the fork is one step earlier: `RouteDamage` picks the
pool up front. The reason is that CoO's `ApplyDamage` reads and writes
a `Hitpoints` **stat** throughout — resistances, the `Penalty` channel,
death checks — and objects have no such stat. Sharing it would have
meant giving every barrel a `Hitpoints` stat, which is precisely what
`DestructiblePart`'s docstring argues against, since that stat is what
opts an entity into the creature death path.

---

## 5. Status effects and skills on objects 📋

**Want.** Fire spells ignite flammable things — hedgerows, trees. And
by extension, every status effect should do to an object whatever is
logical for that object's material.

**Good news: the substrate exists.** `MaterialPart` and `ThermalPart`
are already on terrain including every pool and seep, and there is
already liquid/gas fire interaction. This is largely a matter of
letting existing effects reach non-creature targets and deciding what
each means.

### 5.1 Targeting

`LineTargeting.GetFirstTargetableObject` currently **skips** anything
tagged `Creature`, `Wall` or `Terrain` — so a spell passes straight
through a hedgerow today. That filter is the first thing to change, and
it must stay deliberate: a fireball should be stopped by a wall but not
by grass.

### ⚠ §5.1 was wrong — targeting was never the problem

The pre-implementation sweep read the filter against the actual
blueprint tags and it does not do what the paragraph above says.
`Hedge`, `Tree` and `Bush` carry **no** `Wall` or `Terrain` tag
(`Tree` has only `Solid`; `Hedge` has none at all), and the filter
admits anything with a `PhysicsPart`. So spells were already hitting
hedgerows and trees. `Wall` **is** tagged `Wall`, is skipped, and then
stops the trace via `cell.IsSolid()` — which is exactly the
"stopped by a wall, not by grass" behaviour §5.1 wanted built. **No
change to `LineTargeting` was needed or made.**

The real reason a fire bolt did nothing to a hedgerow is two gates
further downstream, both invisible:

1. **`CombatSystem.ApplyDamage` early-returns on any target with no
   `Hitpoints` stat** (`CombatSystem.cs:832-840` — deliberate, so props
   and statues are not damageable creatures). Scenery has structural HP
   on a `DestructiblePart`, not a `Hitpoints` stat, so every point of
   spell damage aimed at it was silently discarded.
2. **`DirectionalProjectileMutationBase` gated the on-hit effect on
   `target.GetStatValue("Hitpoints", 0) > 0`** (line ~139). For a
   non-creature that is always 0, so the gate closed on every object
   unconditionally — `BurningEffect` could never be applied to anything
   that was not alive.

`BurningEffect` had the same bug in its own tick: it dealt its
per-turn fire damage through `ApplyDamage`, so scenery that *was*
somehow set alight would burn indefinitely and never be consumed.

**Fix:** `DestructionSystem.RouteDamage(target, damage, source, zone)`
— one helper that sends damage to whichever pool the target actually
has, used by `MutationDamageHelpers.ApplySpellDamage` and
`BurningEffect` alike. Plus widening the on-hit gate to admit a
breakable object that survived the hit.

**The lesson, repeated:** "a spell passes through a hedgerow" is a
symptom. The first plausible mechanism found near it (a targeting
filter that mentions `Terrain`) was not the mechanism. Read the tags
the filter actually tests against the content that actually exists.

### 5.2 An effect-to-material matrix

The honest way to scope this is a table, authored explicitly rather
than emergent:

| Effect | Flammable (hedge, tree, crop) | Stone / metal | Liquid | Ice |
|---|---|---|---|---|
| Burning | ignites, spreads, destroys | nothing | boils off / ignites if oil | melts |
| Freezing | brittle (damage bonus) | nothing | freezes to solid | nothing |
| Acid | damages | etches slowly | dilutes | — |
| Shock | nothing | conducts to adjacent metal | conducts, shocks anything in it | — |
| Poison / Bleed / Stun / Fear | **meaningless — must be refused** | | | |

**The last row is the important one.** Applying Bleeding to a barrel
must be a no-op with a sensible message, not a silent success that puts
a bleed timer on furniture. A rule of "effects apply to objects unless
listed" fails open; the matrix must be opt-in per effect.

### 5.2 as shipped — `ObjectStatusMatrix`

`Assets/Scripts/Gameplay/World/ObjectStatusMatrix.cs`. A
`Dictionary<Type, Func<Entity, bool>>`: the key is the effect type, the
value asks whether THIS object is made of the right stuff. Absent from
the table ⇒ `Meaningless`; present but the material gate fails ⇒
`WrongMaterial`. Both refusals emit
`effect/ObjectEffectRefused` with the reason, because "nothing
happened" is the hardest bug class to chase and the two refusals have
different fixes (author a material tag vs. add a row).

| Effect | Gate |
|---|---|
| `Burning`, `Smoldering`, `Charred` | Flammable, Organic, Wood, Plant, Cloth, Paper, Fungal |
| `Electrified` | Conductor, Metal |
| `Frozen` | Wet, Water, Liquid, Ice, Organic |
| `Wet`, `Acidic`, `Broken` | anything |
| everything else | refused |

**Divergence from the proposed table:** the "Liquid" and "Ice" columns
are not implemented as separate behaviours — a liquid pool either
accepts an effect or does not. Boiling-off, dilution and melting are
existing `ThermalPart` / `MaterialReactionResolver` behaviour and were
left where they are rather than duplicated into the matrix.

**Divergence — the gates read TAGS, not the numeric fields.**
`MaterialPart.Combustibility` and `Conductivity` are authored on two
different scales in the same content file: 84 blueprints use 0-1
(steel is `Conductivity 0.8`) and a handful use 0-100 (`OldWorldPipe`
is 100), despite `MaterialPart`'s own docstring declaring 0-100
canonical and warning consumers to compare against a threshold of 50.
Any threshold chosen here would be correct for one group and silently
wrong for the other. `MaterialTagsRaw` is authored consistently, so the
gates read that. **This is pre-existing content drift, not something
this slice introduced** — spun off as its own task.

**Content added:** `Hedge`, `Tree`, `Bush`, `BerryBush`, `VineWall`,
`HaulBarrel`, `Chest`, `LockedChest`, `LockedDoor`, `IceWall`, `Wall`
and `Pillar` had no `MaterialPart` at all, so nothing about them was
flammable, conductive or freezable. Each now carries one.

### 5.3 Fire spread

Ignited flammables should spread to adjacent flammables and eventually
destroy themselves (→ §4). This is the piece with the most emergent
potential *and* the most risk: a hedgerow field that burns down
entirely, or a fire that runs through crop strips into a village, is
either a great story or a bug depending on whether it was intended.
**Spread wants a rate limit and a test that a fire cannot consume a
whole zone in one turn.**

### ⚠ §5.3 — spread already exists; nothing new was built

`BurningEffect.OnTurnStart` step 5 already calls
`MaterialSimSystem.EmitHeatToAdjacent(target, zone, Intensity * 30f)`,
and `ThermalPart`'s ignition pipeline (FlameTemperature check →
`TryIgnite` → `MaterialPart` veto → `WetEffect` suppression →
`BurningEffect`) is what decides whether a neighbour catches. That is a
physical propagation model with its own rate limit — heat has to
accumulate past a threshold — rather than a graph flood, so the
"consumes a whole zone in one turn" failure mode the plan feared is
not reachable by construction.

**What changed instead is that fire can now finish the job.** Before
this slice, scenery could be heated and lit but never consumed
(`BurningEffect` dealt its damage through `ApplyDamage`, which
early-returns on objects). Now a burning hedgerow loses structural HP
each turn and is destroyed at zero, which is what makes spread
*terminate*: the fuel goes away.

**Deliberately deferred:** no zone-scale burn budget, because none is
needed yet given the thermal gate. If playtesting shows a hedgerow
field going up wholesale and that reads as a bug rather than a story,
the rate limit belongs in `MaterialSimSystem.EmitHeatToAdjacent`, not
in the matrix.

---

## Build order

1. **§1 ghost trails** (option B) — small, isolated, visible immediately.
2. **§3 interact key** — bounded; unlocks reaching objects at all, which
   §4 and §5 both assume.
3. **§4.1–4.2 objects take damage** — the foundation.
4. **§4.3–4.4 destruction and its consequences** — including the
   indestructible register.
5. **§5.1–5.2 effects reach objects**, matrix-driven.
6. **§5.3 fire spread** — last, because it is the one that can eat a
   zone.

§2 is done.

## Honesty bounds

- §1's mechanism is read from the pass ordering in `ZoneRenderer` and
  the capture in `GlyphGhostRenderer`; it is **not** yet confirmed by
  seeing the fix work. Rendering claims need a live look.
- §4 and §5 are sketched from a survey of what exists, not from a
  verification sweep of every call site. Each gets its own sweep at the
  top of its slice, per `CLAUDE.md`.
- The effect matrix in §5.2 is a design proposal. It is authored by
  reasoning about materials, not derived from the existing effect
  implementations, and every row needs checking against what the effect
  actually does before it is built.
