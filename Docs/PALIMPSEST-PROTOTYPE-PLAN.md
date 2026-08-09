# Palimpsest Prototype — implementation plan

**Status:** PLAN — awaiting review. Nothing implemented.
**Date:** 2026-08-09
**Inputs:**
- `Docs/PALIMPSEST-SPEC-VS-SHIPPED.md` — the verification sweep for this
  plan. Every "already exists" claim below was checked there against
  source, not recalled.
- `Docs/SPELLCRAFT-STATUS-SYNERGY.md` — the shipped SM1–SM8 work this
  builds on, and the SM9–SM13 roadmap this **replaces in part** (§2).
- `Docs/STATUS-SYSTEM-MODULAR-LADDER.md` — the user's approved build
  principle: modular layers, each an independently testable POC.

---

## 1. What this plan is, and what it deliberately is not

**Not** "implement the Palimpsest spec." Roughly half of that spec is
already shipped, one of its pillars has been rejected by decision, and
its POC ladder starts several rungs below where the game already is.

**This plan is:** the shortest path from what exists today to the spec's
*genuinely new* ideas, built as independently testable layers.

### Binding decisions already made

| Decision | Effect on this plan |
|---|---|
| **Skills stay cooldown-based; Stamina rejected** (2026-08-09) | Spec §2's Stamina economy is **cut entirely**. Cooldowns remain the only skill gate |
| **Ink is per-book charges, not a pool** | Spec §2's regenerating Ink pool is **cut**. `GrimoireChargePart` stays as shipped. Scrape's refunds go to the **book** |
| **Modular POC layers** (user, via the ladder doc) | Every phase below must be independently shippable and independently fun-testable |
| **Rites cast cold are weak** (SM7) | Any new rite added here obeys the same rule |

### Already shipped — do not rebuild

Spec **POC 1 (Actors Only) is complete.** Statuses, skills that write
them, grimoires that consume them, resonance, ink, and four rites all
ship. Also shipped and reusable: **25 liquid definitions** (including a
full `oil`), **13 data-driven reactions**, a thermal simulation, a gas
system, push/pull primitives that run the real movement pipeline, and a
`spell` diag category already emitting the causal chain.

**The prototype therefore starts at spec POC 2, not POC 1.**

---

## 2. Reconciling the two roadmaps

`SPELLCRAFT-STATUS-SYNERGY.md` has SM9–SM13 outstanding. Several
overlap with this spec. Rather than run two contradictory roadmaps:

| Old milestone | Fate |
|---|---|
| **SM9** — channelling (hold a rite for more slots) | **Kept, deferred.** Resonance already supports the slot count. Low value until there are more statuses to spend, which P2–P3 provide |
| **SM10** — Shattered Rime + Verdigris Bloom rites | **Kept, folded into P5** — they become tile-reading rites rather than creature-only |
| **SM11** — environmental features (Rain Basin, Static Spire…) | **Superseded by P2/P4/P6.** Those features were a hand-authored version of what a real tile layer gives generically |
| **SM12** — legibility (primed marker, resonance preview) | **Kept, becomes P8** — merged with the spec's Resonance Dial / Inspect / Reaction Log |
| **SM13** — adversarial sweep + damage bench | **Kept, becomes P9**, and now also has to answer the 3-status-reachability question from the cooldown decision |

---

## 3. The keystone: a tile-state layer

**The single architectural finding from the sweep:** the shipped system
is actor-centric, the spec is tile-centric. `ResonanceSystem` is already
a `Preview`/`Spend` pair over "a thing that has statuses" and does not
care that the thing is a creature. **One new layer unlocks most of the
spec.**

### Where tile state should live — three options

`Cell` today holds `X`, `Y`, `ParentZone`, `Explored`, `IsVisible`,
`IsInterior`, `List<Entity> Objects`. There is no extension point.

| Option | Pros | Cons |
|---|---|---|
| **A. Fields on `Cell`** | simplest access | 80×25 = 2000 cells × N layers allocated per zone whether used or not; bloats save |
| **B. One entity per layer** (today's `WaterPuddle` pattern) | reuses existing machinery and rendering | entity churn; a tile with Water+Oil+Embers+Charge is 4 entities; iteration cost |
| **C. Sparse per-zone store** — `Dictionary<int, TileState>` keyed `y*Width+x` ⭐ | only written tiles cost anything; one lookup; trivially serialisable; matches the fact that most tiles are blank | needs an adapter so existing pool entities stay consistent |

**Recommendation: C.** Most tiles never carry state, so a sparse store
is both the cheapest and the most honest representation. `WaterPuddle`
and friends get a small adapter that mirrors into the layer on spawn, so
there is exactly one source of truth rather than two water systems.

### `TileState` shape

```
Coatings  : small list of liquid ids   (water, oil, spores — reuse LiquidDefinitions)
Residues  : small list of residue ids  (embers, ash)
Heat/Cold/Charge : byte 0..2           (the spec's discrete channels)
Cloud     : one gas id + turns         (steam, smoke)
```

**Note the deliberate divergence:** the spec's discrete Heat/Cold/Charge
0–2 is *not* the same as `ThermalPart`'s continuous temperature. Both
will exist. The tile layer uses the coarse, legible model the spec asks
for; entities keep the simulation. They meet only at explicit reaction
boundaries. This is worth doing on purpose — the coarse model is what
makes the system readable — but it **must** be documented or a future
reader will think one of them is a bug.

---

## 4. Phases

Each is independently shippable and independently testable. Any phase
can be the last one without leaving the game broken.

### P1 — TileState layer (the bridge) 🔑

Sparse per-zone store; read/write API; save/load round-trip; per-turn
decay of durations; `ZoneRenderHooks.MarkCellDirty` on every write.
Adapter so existing pool entities mirror in.

*No gameplay yet.* **POC test:** write a coating, read it back, save,
load, confirm it decayed on schedule.

### P2 — Abilities write to tiles (spec POC 2)

The spec's "Environmental Technique". Existing skills gain the ability
to leave a mark:

- `Hydromancy_JetBlast` → writes **Water** where the target lands
- `Pyromancy_EmberSpit` → writes **Embers**
- `Galvanism_GroundSurge` → writes **Charge**
- **NEW skill: `Oilmark`** — the clearest gap in the sweep. `oil`
  already exists as a liquid with Combustibility 90; nothing writes it.

**POC test:** does leaving marks change where the player chooses to
fight? Ship it and play it before building P3.

### P3 — Tile reactions (spec POC 3)

Data-driven, in the shape `MaterialReactions/` already uses. Five to
start: **Electrify** (Water+Charge), **Steam** (Water+Heat), **Freeze**
(Water+Cold), **Melt** (Ice+Heat), **Ignite Oil** (Oil+Heat/Embers).

Also from spec §12/§26/§27, and non-negotiable: **energy cancellation**
(Heat vs Cold), the **fixed resolution order**, and the **loop guards**
(ReactionID+ActionID+TileID, max depth 4, max 8 generations). Without
the guards a chain reaction can hang the turn.

**POC test:** five rules producing situations players set up on purpose.

### P4 — Propagation (spec POC 4)

Wet conduction (3 tiles), metal conduction (4 tiles), fire spread across
flammable structures. Requires **Substrate** on tiles and the
`MetalGrate` structure.

**POC test:** one local action visibly changes another part of the room.

### P5 — Rites read tiles

The payoff. Extend `ResonanceSystem` to accept a tile as a resonance
source, then rewrite rites in the spec's idiom: a rite **writes Charge 2
and walks away**; the world resolves whether that means electrified
water, a charged grate, or nothing. Folds in old SM10's two rites.

**POC test:** does one rite produce different outcomes in different
rooms without the rite knowing why?

### P6 — Terrain creation (spec POC 5)

`Vine` (non-blocking, roots), `StonePillar`, `MetalGrate`, `Barrel`
(water/oil, ruptures onto tiles). Plus the **Bloom** reaction
(Spores+Water → Vine), which matters because it proves a reaction can
create *terrain* rather than damage. `GlacialWall` (SM6) already
demonstrates temporary terrain, so the pattern exists.

### P7 — Scrape and the harvest economy (spec POC 6) ⭐

**The spec's best and most novel idea, and the reason to do all of the
above.** Scrape removes every temporary layer from a tile and refunds
ink to the book by layer count (1 layer → net 0; 3 → net +1; 5+ → net
+2). Write → React → Transform → **Harvest** → Rewrite.

**POC test:** do players build complicated tiles *in order to* harvest
them? If not, cut it — that is the whole hypothesis.

### P8 — The reading layer (spec POC 8 = old SM12)

Resonance Dial, Inspect Mode, Reaction Log, plus SM12's primed marker
and resonance preview. **The `spell` diag category already emits the
causal chain**, so the Reaction Log has its data source today.
Accessibility (symbol + motion per resonance, not colour alone) is part
of this phase, not a follow-up.

**POC test:** the *minimum* information that lets a player predict the
world without being handed solutions.

### P9 — Progression, sweep, and the bench

Spec §17–18 upgrades that **modify existing abilities** rather than
granting new ones (`Undertext`, `Marginalia`, Environmental Technique
as a purchasable gate). Then the mandatory gates: adversarial sweep
(this feature hits state atomicity, parser, propagation, save/load and
anti-exploit surfaces — well over the two-surface threshold), plus a
**deterministic self-auditing bench** that must answer the open
question from the cooldown decision: *is the 3-status resonance tier
reachable in a real fight, or aspirational?*

---

## 5. Performance — mandatory reading before P1

Four of CLAUDE.md's five triggers apply.

- **Sparse store, never a dense array.** 2000 cells per zone; the
  overwhelming majority carry no state. A dense per-cell struct would
  cost memory and save size for nothing.
- **Per-turn decay must not scan every tile.** Iterate the sparse
  dictionary's *written* entries only.
- **No allocation in the reaction loop.** Reaction resolution runs
  per-write and can cascade. Scratch lists per
  `Docs/PERF-FOUNDATION.md §Pattern 1`; no LINQ.
- **Every write marks its cell dirty** via
  `ZoneRenderHooks.MarkCellDirty(x, y, source)` — never full-zone
  `MarkDirty`, which is for FOV/lightmap changes only.
- **The propagation caps are a perf feature, not just a design one.**
  Depth 4 / 8 generations bounds the worst case a player can construct.

---

## 6. Observability — decided before code

Extend the existing `spell` category (registered in SM7) and add a
`tile` category:

| Kind | When | Payload |
|---|---|---|
| `TileWritten` | any layer written | x, y, layer, id, duration, source ability |
| `ReactionFired` | a tile reaction resolves | reaction id, inputs consumed, outputs, tile |
| `ReactionSuppressed` | guard blocked it | reason: `already_fired_this_action` / `depth_cap` / `generation_cap` |
| `PropagationStep` | charge/fire moves | from, to, medium, step index |
| `ScrapeHarvested` | Scrape resolves | layers removed, ink refunded, net |

`ReactionSuppressed` matters most: a chain that silently stops is
otherwise indistinguishable from one that never started.

---

## 7. Honest risks

1. **Two energy models.** Coarse 0–2 on tiles, continuous on entities.
   Justified, but it is the most likely source of future confusion.
   Must be documented at both ends.
2. **Two water representations** until the P1 adapter lands. Get the
   adapter right in P1 or this metastasises.
3. **P7 is the payoff and it is last.** If P1–P6 are not fun on their
   own, the plan has spent a lot before testing its best hypothesis.
   *Mitigation:* P2 and P3 each have a play-test gate; do not continue
   past a failed one.
4. **Scope.** This is comfortably larger than SM1–SM8 combined. It is
   structured so stopping after any phase leaves a working game — that
   property is the whole point, and it should be used.

---

## 8. Open questions

1. **How far does the tile layer go?** Prototype-room only, or every
   zone in the game? Room-only is far cheaper to test; game-wide is the
   real thing. *Recommendation: build the layer game-wide (it is sparse,
   so idle zones cost nothing) but only author content for the
   prototype room until P2's play-test passes.*
2. **Do existing pool entities migrate, or coexist?** *Recommendation:
   adapter in P1, full migration deferred* — migrating 5 pool blueprints
   and their tests is real work with no player-visible payoff.
3. **Should Scrape refund to the book or to a new pool?** The ink
   decision says per-book. Refunding the *cast* book is consistent but
   means Scrape cannot exceed that book's max. *Recommendation: refund
   the book, cap at max, and let the overflow be lost* — the loss is
   itself a decision about when to harvest.
4. **Is the prototype room (§29) a scenario or a real zone?** A
   `Scenarios/Custom/` bench is faster and self-auditing;
   a real zone is what actually proves it. *Recommendation: both —
   bench first for the matrix, then a hand-authored ruined greenhouse.*
