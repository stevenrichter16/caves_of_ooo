# Object interaction & destructibility — plan

> Five issues raised from play, 2026-08-12. Two are small render bugs,
> one is an input affordance, and two are real systems that change what
> the world is made of. Ordered by blast radius, smallest first.
>
> Status marks: ✅ shipped · 🔨 in progress · 📋 planned

---

## 1. ASCII ghost trails over sprite tiles ✅ (partial)

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

Gating on sprites removes the symptom for everything that has a sprite,
which is the player and the named roster. **Any purely-ASCII actor still
leaves a 6-turn trail.** Fixing the clock is its own slice and needs a
decision: a real per-frame decay (costs an `Update` on a renderer, see
`Docs/PERF-FOUNDATION.md`) or dropping the feature.

**Counter-check that matters:** a purely-ASCII actor must still leave a
trail, or B has silently deleted the feature instead of fixing it.

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

**Prefer a Part.** It carries more than a number — what it drops, what
it sounds like, whether it can be destroyed at all (a quest door should
not be breakable by accident) — and it keeps "has HP" from implying
"is a creature" everywhere HP is currently read.

### 4.2 Let attacks reach non-creatures

Find and relax whatever gate currently requires a creature. The bump
path is the primary entry: walking into a solid object should offer or
perform an attack rather than silently refusing.

**Risk — this is the one to be careful about.** `ApplyDamage` and the
death path are creature-shaped: corpses, XP, faction reputation, the
`Died` event with its listeners. Routing a barrel through that path
unchanged would try to give it a corpse and award XP for a fence. A
**separate destruction path** for objects is safer than widening the
creature one.

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

### 5.3 Fire spread

Ignited flammables should spread to adjacent flammables and eventually
destroy themselves (→ §4). This is the piece with the most emergent
potential *and* the most risk: a hedgerow field that burns down
entirely, or a fire that runs through crop strips into a village, is
either a great story or a bug depending on whether it was intended.
**Spread wants a rate limit and a test that a fire cannot consume a
whole zone in one turn.**

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
