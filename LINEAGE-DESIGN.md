# LINEAGE-DESIGN.md

**A data model for lineage and a single NPC's position within it.**

This document specifies the data structures for the lineage system. It deliberately excludes Houses — they will be layered on top of this substrate in a separate document. What follows is the pure bloodline graph, the NPC's relationship to that graph, and the query surface the rest of the game's systems (Dialogue Beats, Witness System, Mycelial Communion, Memorial Acts, revenge quests) read from.

---

## Design goals

- **Graph-native.** Lineage is fundamentally a graph problem. Model it as one; don't flatten it into lists.
- **Bidirectional pointers.** Traversals are read-hot in dialogue and skill queries. Pay the write-time cost of maintaining both sides of every kin link.
- **Genetic vs. social separation.** Adoptive family is real family socially, not genetically. The same data supports both without conflating them.
- **Cacheable.** Per-NPC summaries exist as optional denormalized caches, invalidated on write and rebuilt lazily.
- **Query-first API surface.** The rest of the game calls a small set of functions; the internal representation is free to evolve as long as the query surface stays stable.
- **Lineage as its own entity.** Bloodlines can carry tags, mythic deeds, and cached indices independent of any single living member.

---

## Primitives

```
type NpcId       = string
type LineageId   = string   // a persistent identity for a bloodline
type Year        = int      // in-world year

enum Sex        { Male, Female, Other, Unknown }
enum LifeState  { Alive, Dead, Missing, Unknown }
enum BondType   { Blood, Marriage, Adoption, Fostering }
```

**Notes**

`NpcId` and `LineageId` are stable strings — never reused, never reissued after an entity's death or a lineage's extinction. This matters for the Witness System: rumors and deeds reference these IDs long after the entity is gone.

`Year` is an integer in-world year. Precision below year-level is unnecessary for genealogy; births and deaths are recorded to the year.

`LifeState.Missing` is distinct from `Dead` — a missing NPC has no confirmed death record, which matters for succession rules, inheritance, and certain revenge/search quests. `Unknown` covers cases where the network has not yet observed this NPC's status.

---

## The NPC

```
struct Npc {
  id:            NpcId
  name:          string
  sex:           Sex
  life_state:    LifeState
  birth_year:    Year?          // null if unknown
  death_year:    Year?          // null if alive or unknown
  death_cause:   string?        // "plague", "killed_by:<npcId>", "old_age", ...

  // Lineage pointers — all optional, all by reference
  parents:       NpcId[]        // typically 0–2; multiple allowed for cultural variance
  children:      NpcId[]        // the flip side of `parents`; redundant but fast to read
  spouses:       SpouseRef[]    // can be multiple across time; marked current/former
  bonds:         BondRef[]      // non-blood family (adoption, fostering, sworn-kin)

  lineage_id:    LineageId      // the bloodline this NPC belongs to (inherited)
  ancestry_hint: AncestryHint?  // cached summary for fast queries, rebuildable
}
```

### Field notes

**`parents` / `children`** are bidirectional. When you add a child to an NPC, both sides update atomically. This redundancy is intentional — the alternative (a single "child→parent" pointer and reconstructing the reverse per query) is far too slow for read-heavy dialogue and skill code.

**`parents.length`** is typically 0–2 but deliberately unbounded. Some cultures recognize multiple parental roles (a biological parent and a ritual parent, a clan's collective maternal role, etc.). Accepting an array here costs nothing and avoids a schema migration later if the world's cultures turn out to need it.

**`spouses` as an array of `SpouseRef`** rather than a single pointer allows modeling widowhood, divorce, and serial monogamy without losing history. Status is marked per reference.

**`bonds` is separate from `parents`/`spouses`** to preserve the genetic/social distinction. A person adopted as an infant has `bonds` pointing at their adoptive parents but empty or unknown `parents`. Mycelial Communion reads `parents`; community dialogue reads `bonds` *and* `parents` as "family."

**`lineage_id` is inherited** at birth. The default rule is matrilineal or patrilineal based on the culture's convention; edge cases (unknown parents, adopted infants with no genetic record, newly-founded lineages) get either their adoptive lineage or a fresh generated one.

**`ancestry_hint`** is a denormalized cache, nullable, rebuilt lazily on first read after any write that touches this NPC's kin graph.

---

## Spouse and bond references

```
struct SpouseRef {
  partner:       NpcId
  status:        "current" | "former" | "deceased"
  bond_year:     Year?
  end_year:      Year?          // divorce, death, dissolution
}

struct BondRef {
  other:         NpcId
  type:          BondType
  bond_year:     Year?
  end_year:      Year?
  notes:         string?        // "adopted as infant", "fostered during drought"
}
```

### Field notes

**`SpouseRef.status`** is a string enum expressed as union type for clarity. `"deceased"` is distinct from `"former"` — a widowed NPC often retains reputational and inheritance ties that a divorced NPC does not.

**`BondRef.notes`** is a free-form string intended for flavor and occasional dialogue slot-fill (a character introduced as "fostered during the drought" carries narrative weight). Not parsed by game logic.

**`BondRef.type = Marriage`** is valid but generally not used, because spouses are represented in `spouses` for access speed. The one case where it appears here is retroactive or mythic marriages with no living spouse to reference.

---

## The lineage

```
struct Lineage {
  id:                LineageId
  name:              string?            // colloquial bloodline name, may be anonymous
  founding_ancestor: NpcId?             // oldest known ancestor in this line
  founding_year:     Year?
  culture_tag:       string?            // informs naming, inheritance rules

  members:           Set<NpcId>         // everyone in this bloodline, living and dead
  tags:              LineageTag[]       // mythic deeds, curses, reputations

  // Derived/cached for query speed; rebuildable from members[].parents links
  generations:       GenerationIndex?
}
```

### Field notes

**`Lineage` is a first-class entity**, not a purely derived view over parent pointers. This is what allows lineage-scoped data — mythic deeds, curses, collective reputation — to exist independent of any single living member. When the last living member of a lineage dies, the Lineage object persists; its members set still contains the dead, and queries against it still work.

**`name`** is nullable because many lineages are anonymous — a peasant bloodline in a small village may have no colloquial name until a notable member emerges. The system generates `name` lazily when a naming event occurs (a House is founded, a legendary deed is done, a migration happens).

**`founding_ancestor`** is the oldest known ancestor. May be null in very young lineages (a founding-generation ancestor is their own founder — degenerate case) or in fragmentary records where the oldest known member is known to have had parents who are not themselves recorded.

**`culture_tag`** is the bridge to cultural rules — naming conventions, matrilineal vs. patrilineal inheritance, polygamy support, burial customs. The lineage carries this tag because bloodlines persist across community migration; a Kethken family that moves to a new town retains their naming conventions.

**`members`** is a Set of all members living and dead, maintained as members are born into or removed from the lineage. Lineages grow monotonically in membership count (the dead stay in the set); they "shrink" in living membership only through deaths and lineage transfers (marriage-in, adoption-out).

---

## LineageTag

```
struct LineageTag {
  key:           string         // e.g. "curse:drowning", "deed:crossed_flats"
  origin_npc:    NpcId?         // whose act created this tag
  origin_year:   Year?
  decays:        bool           // some reputations fade across generations
}
```

### Field notes

**`key`** is a colon-prefixed tag key. The prefix categorizes the tag type: `deed:`, `curse:`, `honor:`, `oath:`, `shame:`, `gift:`. The suffix is a short identifier that the Dialogue Beat system's slot resolvers can read.

**`origin_npc` / `origin_year`** record provenance. A curse acquired by an ancestor 120 years ago reads differently in dialogue than one acquired by a living grandparent.

**`decays`** is a behavioral flag read by the tag aging system. Some tags fade across generations (most honors and shames); some are permanent (founding deeds, active curses, blood-debts). The specific decay curve is owned by a separate subsystem; this flag just indicates eligibility.

---

## GenerationIndex

```
struct GenerationIndex {
  // Maps each NPC to their generation depth from the founding ancestor.
  // Founder = 0, children = 1, grandchildren = 2, etc.
  depth_by_npc:  Map<NpcId, int>
}
```

### Field notes

**Pure cache**, rebuildable from `members[].parents` traversal. Exists to make "how many generations deep is this NPC" a constant-time read, which matters for Mycelial Communion Rank 4/5 queries and for any UI that displays a family tree.

Invalidated on any write that changes parent pointers within the lineage, or when new members are born or added. Rebuild cost is linear in lineage membership.

---

## AncestryHint (per-NPC cache)

```
struct AncestryHint {
  // Shallow cache so dialogue and skill queries don't walk the graph every time.
  // Invalidated when any pointer in this NPC's kin graph changes.

  living_siblings:     NpcId[]
  living_parents:      NpcId[]
  deceased_parents:    NpcId[]
  living_children:     NpcId[]
  deceased_children:   NpcId[]
  extended_living:     NpcId[]   // aunts/uncles/cousins via parent graph walk
  known_ancestors:     NpcId[]   // up to N generations back; N set by world config
  depth_from_founder:  int?
  last_rebuilt:        Year
}
```

### Field notes

**Entirely optional.** The rest of the system works without it; it exists purely for dialogue and skill-query performance when the same NPC is inspected many times per game-day.

**Rebuild policy** is lazy on first read after invalidation. Invalidation triggers: any change to this NPC's `parents`, `children`, or `spouses`; any change to the living/dead state of any NPC within graph-distance 3; any change to the lineage membership.

**`last_rebuilt`** is compared against a world-level change counter to detect stale caches without walking the graph.

**`extended_living`** is populated via a graph walk — parents' siblings, parents' siblings' children, etc. The graph-distance cap is set by world config (default 3 degrees, which covers first cousins).

**`known_ancestors`** depth is set by world config and typically matches the world's cultural norms — 3–4 generations for common folk, deeper for aristocratic or priestly lineages. This field's depth cap is distinct from the graph-distance cap above because ancestors cost is linear in generation depth, not combinatorial.

---

## Core query surface

```
fn siblings_of(npc: NpcId) -> NpcId[]
fn parents_of(npc: NpcId) -> NpcId[]
fn children_of(npc: NpcId) -> NpcId[]
fn ancestors_of(npc: NpcId, max_generations: int) -> NpcId[]
fn descendants_of(npc: NpcId, max_generations: int) -> NpcId[]
fn kin_within(npc: NpcId, degrees: int) -> NpcId[]   // graph-distance query
fn are_kin(a: NpcId, b: NpcId, max_degrees: int) -> bool
fn closest_common_ancestor(a: NpcId, b: NpcId) -> NpcId?
fn living_members_of(lineage: LineageId) -> NpcId[]
fn lineage_of(npc: NpcId) -> LineageId
fn is_founder(npc: NpcId) -> bool
fn is_lineage_extinct(lineage: LineageId) -> bool
```

### Notes

**This is the full public API.** The rest of the game should not read `Npc.parents` directly; it should call `parents_of(npc)`. This indirection lets the internal representation evolve — caches, indices, procedural generation-on-demand — without touching call sites.

**`kin_within(npc, degrees)`** is the workhorse query for social systems. "Who are the kin of this person within 2 degrees" is what the Witness System calls when propagating rumors of a death; what the Dialogue system calls when filling `{kin_name}` slots; what Memorial Acts calls to determine who would appreciate a shrine.

**`closest_common_ancestor`** is important for feud and honor mechanics — when two Houses clash, the narrative often traces to a shared ancestor several generations back.

**`is_lineage_extinct`** is a simple predicate over `living_members_of(lineage).isEmpty()` but given its game-event significance (lineage extinction is a narrative milestone), it deserves a named predicate.

---

## What this does not cover

By design:

- **Houses.** Layered on top of Lineages in a separate document. One Lineage may belong to zero, one, or multiple Houses across its lifetime; Houses may merge, split, adopt sub-lineages, or be founded from branches of existing lineages. That complexity belongs to the House layer, not here.
- **Cultural inheritance rules.** Which parent's lineage a child inherits, whether marriage transfers lineage membership, how adoption affects lineage — these are driven by `culture_tag` and implemented as pluggable rules. The data model here supports any such rule; it doesn't encode a specific one.
- **Mythic or contested lineages.** Claimed descent from a god, a legendary founder whose historicity is disputed, a Palimpsest-rewritten ancestor — these are modeled as normal lineages with specific tag flags. No structural change required.
- **Reincarnation, cloning, soul-transfer.** These can be modeled as separate relationships (a `BondType` extension) without changing the genetic graph. The genetic `parents` field remains grounded in biological parentage even when a character's "identity" includes reincarnated aspects.
- **Runtime procedural generation.** The data model is agnostic to whether a lineage was authored, pregenerated at world-creation, or generated on-demand when the player first inspects an NPC. All three workflows produce the same shape of data.

---

## Design tradeoffs to know about

**Memory cost of bidirectional pointers.** An average NPC with 3 siblings, 2 parents, 4 children, 1 spouse, and 1 bond has ~11 NpcId references. For 10,000 NPCs: ~110,000 references, roughly 2-3 MB depending on string representation. Negligible. The `AncestryHint` cache can double this in the worst case but is bounded and optional.

**Write consistency.** Any mutation of a kin link must update both sides atomically. This is enforced by a repository layer — `LineageRepo.addChild(parent, child)` updates both `parent.children` and `child.parents` in a single transaction. Direct struct mutation is not permitted; the structs are treated as read-only outside the repository.

**Cache invalidation.** The hard problem. `AncestryHint` invalidation cascades one degree on writes — a death invalidates the dead NPC's hint plus all direct kin's hints. Two-degree kin (cousins, etc.) pick up the change on their next hint rebuild via the world change counter. This is correct for the common case; it can show one-frame staleness for extended kin queries immediately after a relevant death, which is acceptable for a turn-based game.

**Serialization.** The entire structure serializes cleanly to JSON. Circular references (NPC → parent → child → back to NPC) are resolved by ID rather than by embedded objects. Saves are compact and diffable.

---

## Implementation priority

Build order:

1. `Npc`, `Lineage`, and their primitive fields. Skip `AncestryHint`, `GenerationIndex`, all caches.
2. Repository layer with bidirectional-write enforcement.
3. Core query functions — `siblings_of`, `parents_of`, `children_of`, `ancestors_of`, `kin_within`. Unit-test each against hand-authored small lineage fixtures.
4. `LineageTag` and tag decay system.
5. Integration with Witness System (kin-propagation on death deeds).
6. Integration with Dialogue Beats (kin-slot resolvers).
7. `AncestryHint` and `GenerationIndex` caches — only after profiling proves they're needed.

Nothing in this module depends on Unity. All of it lives in the pure-C# Core asmdef, fully EditMode-testable, with 80%+ coverage as a hard target. This is probably the most test-friendly subsystem in the whole game — pure data, deterministic queries, no rendering, no async. Ship it tight.
