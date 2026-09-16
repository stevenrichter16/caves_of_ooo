# The Body Lattice — per-node condition, numbness, and armor you can see

> **Status: BRAINSTORM / DESIGN PROPOSAL.** Nothing here is implemented.
> User pitch (2026-09-16): replace flat HP with a lattice of linked
> nodes (spine, lungs, arms, legs, skull); damage lands on a node and
> bleeds along its edges; sever the spine's link to a limb and that limb
> goes **numb** rather than merely hurt; armor is bought and reinforced
> per node so a cheap breastplate that ignores your legs is a visible
> tradeoff instead of one AV number. **With the user's own hedge:**
> there will still be an HP bar, maybe with HP split across parts — and
> per-node armor should partly exist to prevent dismemberment.

## 0. Verification sweep — half of this already shipped

**The surprise:** location-based hit resolution and per-node armor are
already implemented and running.

| What the pitch wants | Actual state |
|---|---|
| Damage lands on a specific node | **Already does.** `CombatSystem.SelectHitLocation(Body, Random)` picks a body part weighted by `BodyPart.TargetWeight`, excluding abstract parts |
| Armor applies per node, not as one number | **Already does.** `CombatSystem.GetPartAV(entity, hitPart)` reads the armor equipped *on the struck part* plus natural armor. A breastplate genuinely does nothing for a leg hit **today** |
| Armor helps prevent dismemberment | **Already partly true**, indirectly: AV cuts damage, and dismember chance scales with the damage ratio. There is also a `CanBeDismembered` **veto event** (Phase H) carrying the struck `BodyPart` — a ready-made hook for armor to block severance outright |
| Limbs can be lost | **Already does.** `CheckCombatDismemberment` → `Body.Dismember(part, zone)`, with severable/mortal gating; `SeveredLimbFactory` turns the result into an item entity |
| A graph of linked nodes with edges | **Structurally present.** The anatomy is a parent-child tree (`AddPartByManager(id, parent, child)`), and `BodyPart` carries `DependsOn` / `SupportsDependent` / `RequiresType` — Qud's dependency-edge model |
| Per-node damage state | **Does not exist.** `BodyPart` has no HP, integrity, or condition field of any kind |
| Numbness (attached but non-functional) | Does not exist |

### The blast radius of a full HP replacement

| Measure | Count |
|---|---|
| `"Hitpoints"` references in code | 175, across 75 files |
| Test files touching `"Hitpoints"` | 214 |

And a concrete cascade: `CheckCombatDismemberment` computes its
threshold from **max HP** (`damage / maxHP`). Remove the pool and
dismemberment breaks too, along with `ShouldFlee`'s HP-ratio check,
healing, death, the sidebar, and save compatibility.

## 1. Recommendation: keep the pool, add the lattice, give them different jobs

The user's own hedge is the right call, and not merely as a risk-dodge.
There is a principled reason these should be two systems:

- **The pool is *life*** — blood, shock, how much fight is left. It
  answers *am I dying?*
- **The lattice is *capability*** — what still works. It answers *what
  can I still do?*

Conflating them is what makes most per-limb-HP systems feel bad. You do
not die because your arm ran out of hit points; you lose the arm's
**function**, and you die of systemic blood loss. Splitting one pool
across nodes also invites a degenerate optimum (spread damage, or focus
one node) and leaves "HP" meaning nothing coherent.

**So every hit does two things:**

1. **Drains the shared pool**, scaled by the struck node — a skull or
   spine hit drains more than a hand. This is where "vital nodes matter"
   lives, and it needs no new death logic.
2. **Degrades that node's integrity**, which costs *function* and
   propagates along the lattice edges.

This delivers everything in the pitch — location matters, damage bleeds
along edges, limbs go numb, armor is per-node, the HP bar survives —
while leaving death, fleeing, healing, saves, and 214 test files alone.

## 2. The lattice

**Nodes** are the existing `BodyPart`s. No new topology is required for
v1: the anatomy is already a tree rooted at the body, and
`DependsOn`/`SupportsDependent` already express "this part is
downstream of that one," which is exactly the edge semantics the pitch
describes.

**Integrity** is one new int per part: full → impaired → numb. It is
*not* a second HP pool to whittle down to death — it is a condition
track that gates function.

| Integrity | Effect |
|---|---|
| Full | normal |
| Impaired | that part's contribution degrades — a weapon arm loses accuracy, a leg costs movement speed, lungs cost stamina-adjacent effects |
| **Numb** | the part is attached and useless (see §3) |

## 3. Numbness — the best idea in the pitch

*"That limb goes numb rather than just hurt"* is the line worth
building the whole feature around, because a limb that is **attached
and useless** is both more horrifying and more interesting than one
that is gone.

A numb limb:
- does nothing — no wielding, no attacks, no contribution,
- **is still carried**, still hit (it keeps its `TargetWeight`), still
  bleeds,
- cannot be quickly fixed — it is a *condition*, not a wound, so
  ordinary healing does not restore it.

That last property makes numbness the demand-generator for content this
project has already designed and has no strong use for yet:

- a **fungal prosthetic** replacing a dead arm (`BUILD-EXPRESSIVITY.md`
  §9.1 D),
- a **salt-cured limb** — never fails, never heals
  (`BUILD-EXPRESSIVITY.md` §9.1 E, and the hollowing build),
- a **barnacle symbiote** clamped to a limb that no longer answers
  (`BARNACLE-SYMBIOTES.md`),
- the Choir's **Include** — replace the flesh with substrate.

Four existing designs suddenly have a problem they solve.

## 4. Propagation along edges

Damage to a node degrades integrity downstream along dependency edges:
wreck a shoulder and the arm and hand below it degrade with it. Severing
the **edge** (rather than the part) is what produces the pitch's spinal
case — the limb is intact, attached, unmarked, and numb, because nothing
reaches it any more.

Two rules keep this from becoming a cascade that deletes characters:

1. **Propagation is directional** (downstream only) and **attenuates**
   per hop.
2. **Mortal/integral nodes** (spine, skull) have much higher integrity
   thresholds, so reaching "sever the spine's link" is a rare,
   memorable, usually-fatal-adjacent event rather than a routine one.

## 5. Dismemberment, reworked (and improved)

Today dismember chance keys off `damage / maxHP` — which means you can
lose an arm because a hit was globally big, and repeatedly hacking the
*same* arm does nothing special. With integrity present, the natural fix:

**Dismemberment keys off the struck node's integrity.** Hack the same
arm until its integrity fails and it comes off. That matches player
expectation, makes the manual multi-cell targeting from
`MOMENTUM-COMBAT.md` meaningful against a single enemy, and turns
"target the leg" into a strategy.

**Armor as a dismemberment veto** is then trivial to wire: the
`CanBeDismembered` event already fires with the struck `BodyPart` and
already supports veto. Heavy armor on a node answers "no" — which is
exactly the pitch's "armor partly to help prevent being dismembered,"
built on a hook that already exists.

## 6. Armor per node — mostly built; the real gap is legibility

The mechanic is shipped (`GetPartAV`). What is missing is that **the
player almost certainly cannot see it**, and there is little per-slot
armor content to buy or reinforce. So the work here is not systems work:

1. **A paper-doll panel** — per-node armor coverage and integrity at a
   glance. This is the deliverable that makes an existing mechanic
   *felt*, and it is the feature's main investment.
2. **A per-slot armor roster** worth shopping for, plus reinforcement
   via the existing tinkering mod path
   (`ArmorTinkerModificationUtility` already exists).
3. **Hit-location feedback in the log** — "the snapjaw bites your left
   leg" — so the player learns that coverage gaps are real.

Note the coexisting legacy path: `CombatSystem` currently has both a
sum-all-equipped-AV branch and the per-part branch. That fork should be
resolved before building UI on top of it, or the panel will lie.

## 7. What full replacement would cost, and why I don't recommend it

For completeness, since the pitch's headline is "replace flat HP":

- 175 code references and 214 test files to audit and rewrite.
- New death semantics (die at skull zero? spine zero? both? what about
  bleed-out?), new flee logic, new healing semantics, new UI, and a save
  migration.
- **Total rebalance from scratch** — every creature's damage was tuned
  against a flat pool.
- Dismemberment, which currently derives from max HP, rewritten too.

Against that, the hybrid delivers the same *felt* experience — location
matters, coverage gaps hurt, limbs die while attached — incrementally,
reversibly, and without a rebalance. If the pool still feels wrong after
the lattice ships, replacing it later is strictly easier with integrity
already in place and proven.

## 8. Risks

- 🔴 **Legacy/per-part AV fork** (§6) must be resolved first; building a
  paper-doll on an ambiguous armor path ships a lying UI.
- 🟡 **State volume and save reach** — integrity per part across every
  creature. `Body` already has explicit save handling; integrity must
  join it deliberately, not by assumption.
- 🟡 **Numbness must not be a death sentence without an answer.** The
  content in §3 needs to exist, or losing an arm's function early is
  just a ruined run in a game with no run to restart.
- 🔵 **Enemy-side complexity**: every creature now tracks per-node
  integrity. Cheap creatures should opt out (integrity only on the
  player, named NPCs, and bosses) until it proves worth the cost.

## 9. Scope-prune (v1) and build order

**Cut from v1:** full HP replacement (§7); edge-severance/spinal
numbness (the dramatic case — ship simple integrity first);
propagation; integrity on rank-and-file creatures.

1. **Resolve the AV fork** (§6) and add hit-location feedback to the
   message log. Pure clarity work on shipped mechanics, zero new state
   — and it makes the existing per-node armor legible immediately.
2. **`Integrity` on `BodyPart`** (one int + thresholds), player and
   named NPCs only. Impaired/numb effects on function. Save handling.
3. **Dismemberment keyed to integrity** (§5) + the armor veto through
   the existing `CanBeDismembered` event.
4. **The paper-doll panel** — coverage and condition at a glance.
5. **Then** propagation along edges, spinal numbness, the per-slot
   armor content roster, and prosthetic/symbiote/salt answers to numb
   limbs.
