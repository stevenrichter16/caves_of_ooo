# Barnacle Symbiotes — itemization you wear instead of carry

> **Status: BRAINSTORM / DESIGN PROPOSAL.** Nothing here is implemented.
> User pitch (2026-09-15): certain small creatures don't die when
> defeated — they can be grappled loose and clamped onto a limb of your
> choosing as living, detachable attachments, each granting a passive
> while fed and kept alive. Tradeable, stealable off a downed enemy
> mid-fight, and gross in the way this project's body horror already is.
> Seed: "barnacle colony" + "hive mind."

## 0. Verification sweep — most of the hard machinery already exists

| What the pitch needs | Actual state | Impact |
|---|---|---|
| Attach a thing to a *chosen* limb at runtime | **Fully built and shipped.** `ExtraArmPrototypeMutation` adds body parts at runtime via `body.AddPartByManager(MANAGER_ID, parent, part)` and removes them via `body.RemovePartsByManager(MANAGER_ID, evenIfDismembered: true)` + `body.UpdateBodyParts()`. The Qud **Manager pattern is live** | A symbiote is a managed attachment. Clamp = add-by-manager, pry loose = remove-by-manager. No new anatomy code |
| A body model with real per-limb structure | Full Qud-parity anatomy: `Body`, `BodyPart` with the whole flag set (`FLAG_APPENDAGE`, `FLAG_INTEGRAL`, `FLAG_EXTRINSIC`, `FLAG_DYNAMIC`, `FLAG_CONTACT`, `FLAG_FIRST_CYBERNETIC`), plus `SupportsDependent` / `DependsOn` / `RequiresType` / `Manager` fields, `Laterality`, `AnatomyFactory` | Limb choice can be mechanically real, not cosmetic |
| A body modification that is *also* an item entity | **Precedent exists in the same file**: ExtraArm generates a limb item entity carrying `RenderPart` + `PhysicsPart` + `EquippablePart` | The "it's a creature, it's an item, it's worn" triple identity is already a shipped shape |
| Granting a passive ability while attached | `MutationSourceType` already includes **`Equipment`**, alongside `MutationModifierTracker` and `MutationGeneratedEquipmentTracker` | A symbiote can grant an existing **mutation** while clamped — reusing ~30 shipped mutations as the ability pool instead of authoring a parallel passive system |
| Taking one off an enemy mid-fight | No grapple exists (`Grapple\|Grabbed\|Wrestl` — zero matches), but **`Cudgel_Disarm` is the exact precedent**: an ability that removes a thing from an enemy in combat | Follow Disarm's shape; no new architecture |
| Losing a limb with a symbiote on it | `SeveredLimbFactory` + `SeveredLimbPart` already turn severed limbs into **item entities carrying metadata**, and the docstring explicitly anticipates reattachment | A severed arm with a live symbiote still clamped to it, lying on the floor, is nearly free |
| Keeping it fed | **No hunger/satiation system** — only `FoodPart` (edible items) | The upkeep loop is the one genuinely new system. §4 keeps it small on purpose |

## 1. The lore fit — and the through-line that makes it more than a gear slot

These are **substrate-creatures**: small pieces of the Rot Choir's
network that ride bodies. That is not decoration, it is the point.

Canon: Selen, the Wedded, lay down into the fungal dark and gave herself
back, and the substrate woke. Her register is **joy**, and her doctrine
is inclusion — *"there is room in us for everyone."* The horror of the
Choir is not malice, it is welcome.

So: **clamping a symbiote to your arm is a small, reversible version of
what the Wedded did.** Each one is a tiny yes to being included. A
player who covers themselves in symbiotes is walking the Consume
ending's path in miniature, one limb at a time — and the game never has
to say so.

That gives an itemization system the project's actual moral weight:

- Symbiote count nudges **Choir standing** up and **Recension standing**
  down. The order that keeps bodies as records does not love a person
  who is becoming a habitat.
- At high counts it can brush the **ending-lean** machinery from
  `Docs/Design/BUILD-EXPRESSIVITY.md` §10 — not gating anything, just
  tilting.
- A **Driving Bloom** variant exists for the aggressive, addictive
  strain (canon: the Bloom multiplies abandonment), and the **Concord**
  will absolutely price, trade, and insure them, which covers the
  pitch's "tradeable" with an existing faction's established register.

## 2. Attachment

A symbiote is an entity with a `SymbiotePart`. Clamping registers it as
a **managed attachment on a specific `BodyPart`**, using the proven
manager pattern. Detaching removes by manager.

**Which limb you choose must matter**, or this is a slot-fill:

- On a **weapon arm**: boosts the attack, interferes with wielding.
- On the **head**: perception-flavored grants, and the worst place to
  have something with opinions (§5).
- On the **body**: defensive grants, hardest to pry off, hardest to hide.
- On a **leg**: movement grants, and it takes the hits your legs take.

**Dismemberment interaction (nearly free, and the best horror in the
design):** lose the limb and you lose the symbiote — but it survives
*on the severed limb*, which is already an item entity. A severed arm
with a live thing still clamped to it can be picked up, carried, traded,
or reattached. The existing `SeveredLimbPart` docstring already
anticipates reattachment.

## 3. What they grant

**Reuse mutations.** `MutationSourceType.Equipment` exists, so a
clamped symbiote grants a mutation for as long as it lives and is fed.
That turns ~30 already-shipped, already-tested mutations into the
symbiote ability pool on day one: a fire-gland barnacle grants FireBolt,
a healing polyp grants Regeneration, a listening one grants Telepathy.

Authoring a symbiote becomes a blueprint plus a mutation reference, not
a new passive-effect system. This is the single biggest cost saving
available in the feature.

## 4. Feeding — small on purpose

No hunger system exists, and **this feature should not add one.** A
global satiation bar would be a survival-game system nobody asked for.
Instead: **per-symbiote upkeep**, a self-contained Part with a turn
tick.

- Each symbiote has a satiety counter that decays slowly.
- **Corpses feed them passively.** The corpse economy from
  `BUILD-EXPRESSIVITY.md` §6 ("every kill is a resource three factions
  want differently") gains a fourth claimant: the thing on your arm
  eats first. A player who fights regularly never thinks about upkeep —
  which is correct, and keeps the loop from being a chore.
- A player who *doesn't* fight has to feed it something else:
  substrate, spores, meat, or **their own vitality**. That is the
  classic parasite bargain, and it means a peaceful playthrough pays a
  price an aggressive one doesn't.
- Starved: the passive stops first (a warning tier), then it detaches
  and crawls off — or, for the nastier strains, starts feeding on you.

## 5. The hive-mind half (the part the seed is really about)

If symbiotes are Choir-substrate, **the ones on your body can hear each
other.** This is where the mechanic earns the second half of its seed.

- **Quorum bonuses, arrived at diegetically.** Two or more of the same
  brood begin coordinating and produce an emergent effect neither has
  alone. Set bonuses that come from the fiction rather than from a
  spreadsheet.
- **And the cost: a colony that reaches quorum starts having
  opinions.** At high count they act on their own — refusing to detach,
  redirecting food, and eventually **singing**. The Choir sings. A
  player wearing five symbiotes has a small chorus attached to them,
  and the Choir knows exactly where that chorus is.
- That is a **soft cap with teeth** — far better than "you have four
  slots." The limit is not a number, it is the point at which the
  things you are wearing become a constituency.

It also explains the pitch's stealing rule in fiction: a symbiote has no
loyalty, only appetite. It does not care whose arm it is on. The hard
part is prying it loose before it re-clamps.

## 6. Taking one off a downed enemy

Follow `Cudgel_Disarm`'s shape: an ability, contested, able to fail,
costing your action. Mid-fight theft of a live attachment should be a
real risk — a failed pry means it gets a bite in, or clamps harder.

A downed-but-not-dead enemy is the ideal target, which quietly rewards
non-lethal play and gives the "grappled loose" verb from the pitch a
home without building a grapple system.

## 7. Salt-cured symbiotes (the upkeep-free lesser option)

A cross-system interaction worth taking, because it costs almost
nothing: the Salting practice (`BUILD-EXPRESSIVITY.md` §3.3) preserves
things in arrested states. **A salt-cured symbiote is dead but
preserved** — it grants a diminished version of its passive, forever,
with no feeding.

That gives players who don't want an upkeep loop a legitimate way to
opt out at a power cost, and it is exactly the kind of "kindness that is
also a taxidermy" the Salted represent. It also means the Cure front
from `Docs/Design/GLACIER-CLOCK.md` kills unprotected symbiotes on
contact, which is a good reason to fear it.

## 8. Risks

- 🟡 **Spreadsheet drift.** With enough symbiotes and enough slots this
  becomes gear optimization. Mitigation: keep the roster **small and
  each entry weird**. Ten memorable symbiotes beat forty statistical
  ones.
- 🟡 **The "always wear the best four" problem.** Mitigation: some
  symbiotes must be actively costly to carry socially (Recension
  hostility, the colony talking, a Bloom strain's addiction), so the
  optimum is contextual rather than absolute.
- 🔵 **Upkeep tedium**, addressed in §4 by making combat feed them
  passively — but it needs playtesting, because an upkeep loop that
  nags is worse than no upkeep at all.
- 🔵 **Save/load reach**: attachment state, satiety, and colony quorum
  all need to round-trip. `SaveSystem`'s public-field reflection path
  covers simple fields; the manager-attachment linkage needs checking
  specifically, since it spans two entities.

## 9. Scope-prune (v1) and build order

**Cut from v1:** the hive-mind colony layer (§5); salt-curing (§7);
mid-fight theft (§6 — ship attaching from a corpse or inventory first);
Bloom strains; faction-standing effects.

1. **`SymbiotePart` + attach/detach** on a chosen limb via the existing
   manager pattern, granting a mutation with source `Equipment`. This
   alone is a complete, playable feature with no new systems.
2. **Per-symbiote upkeep** (§4): satiety tick, passive feeding from
   corpses, the starve-and-detach path.
3. **Dismemberment interaction** (§2) — the symbiote survives on the
   severed limb, reusing `SeveredLimbPart`.
4. **A first content slice**: 6–8 symbiotes, each strange, each mapped
   to an existing mutation, with a reachability test in the shape of
   `GrimoireDistribution`'s totality pin.
5. **Then** mid-fight theft, the colony/quorum layer, salt-curing,
   faction standing, and the Consume-lean tilt.
