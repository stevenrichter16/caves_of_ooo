# Caves of Ooo — Roadmap

**Last updated:** 2026-04-25 (post-M6 audit-cadence completion, post-SaveSystem landing)

A holistic, prioritized recommendation set for what to do next. Updated
when the project state shifts meaningfully. The §3.9 audit-cadence
empirical data (gap-coverage 0% bug-find on M-series, 12.5% on
unaudited code) is the empirical backbone for these priorities.

Companion to `Docs/QUD-PARITY.md` (which is the parity & methodology
deep-dive); this doc is the strategic "what next."

---

## Where the project actually is (2026-04-25)

**Architecture & systems** — rock-solid:

- Entity-Part-Event architecture (Qud-shape)
- Anatomy/Body system (humanoid/quadruped/insectoid)
- Materials simulation (acid, fire, cold reactions)
- Combat system (with 2 recently-fixed bugs from cross-cut adversarial)
- Status effects (post-frozen-bug-saga)
- Tinkering (BitLocker, recipes)
- Conversations (NoFight, dialogue actions)
- Inventory (commands, equipment, weight)
- Field of View / lighting
- AI: Phases 0-5 + M1-M6 milestones shipped
- 1965 EditMode tests; audit cadence proven on 6 milestones

**Just landed**:

- `SaveSystem.cs` (1876 LOC) — largest single system in the repo, with
  **one** round-trip integration test. **Per §3.9, this is now the
  highest-EV bug surface in the codebase.**
- 16×24 sprite atlas assets (PNG files in repo, code switch pending)

**Documented but unfinished**:

- Phase 7 — AIShopper, AIPilgrim, AIShoreLounging (infrastructure
  shipped, content empty)
- Phase 8 — Party / Follower
- Phase 12 — Calendar / world time
- Phase 13 — Zone lifecycle / NPC zone transitions (prerequisite for M8)
- Phase 14 — AI combat intelligence (prerequisite for M9)
- M7 — Turret system
- M8 — Zone transition goals
- M9 — Damage type system

**The honest tension**: engineering methodology has outpaced game
content. The codebase is 95th-percentile clean for a one-person
roguelike, but a player today can't tell you what the game *is* yet.
Tiers below sequence the path from "great engine" to "playable game"
without sacrificing the engineering bar.

---

## Tier 1 — Do now (ship-blockers + highest EV)

### 1. Audit SaveSystem before it's load-bearing

**Why first**: 1876 LOC merged with 1 test. The `§3.9` priority backlog
puts SaveSystem at EV-maximum. Once players have saved games on disk,
the format becomes a compatibility commitment — audit *now*, before
that commitment lands.

**What to do**:

- Run §3.9 cadence (gap-coverage + adversarial) against `SaveSystem.cs`
  and the 18 ISaveSerializable wirings.
- Specific edge candidates (per `QUD-PARITY.md §3.9 Tier S backlog`):
  - Round-trip identity: `entity == reload(save(entity))` for every part type
  - Goal-stack restoration with custom fields (`DisposeOfCorpseGoal.GoToCorpseTries`,
    `LayRuneGoal.MoveTries`, etc.)
  - Static factory re-wiring: `CorpsePart.Factory`, `LayRuneGoal.Factory`,
    `MaterialReactionResolver.Factory` after a "cold" load
  - `InventoryPart.EquippedItems[slot]` back-references same-instance after reload
  - `MutationsPart` manager IDs map to correct dynamic body parts
  - `StatusEffectsPart` non-zero stacks restored
  - `Entity.ID` collision on load
- Add a SAVE FORMAT VERSION test that fails loudly when the format
  changes — protects future-self from quietly breaking old saves.

**Estimated payoff**: based on the M-series 0% vs cross-cut 12.5%
pattern, expect 1-3 real bugs in 2500 LOC of unaudited code.

### 2. Save/load UI

**Why now**: SaveSystem is the most player-facing change in months but
a player can't *use* it without a UI. Without this, the feature is
invisible and the SaveSystem code rots untested in actual play.

**What to do**:

- Press-key-to-save (one-slot autosave is fine for v1)
- Boot menu: "Continue / New Game"
- Death screen: "Continue from autosave / Start over"

The UI work cross-pollinates with #1 — building the UI generates real
save/load traffic that finds bugs the EditMode tests can't, while the
EditMode tests catch corruption-class bugs the UI can't surface fast
enough.

### 3. CombatSystem deeper sweep

**Why**: The ONLY adversarial pass that found bugs in the entire
project found 2 of them in `CombatSystem` (item-dup + auto-kill on
no-HP, commit `65df19c`). The cross-cut probe was 16 tests and
stopped after 2 bugs — there's more dirt under that rock.

**Specific edges to probe**:

- `HandleDeath` ordering (equipment-drop → corpse-drop → Died event → zone-removal)
- Multi-effect re-entrancy: `ApplyEffect(A)` whose OnApply triggers `ApplyEffect(B)`
- `killer == null` paths (rune DoT, poison tick, environmental damage)
- `actor.GetDisplayName()` returning empty during message formatting
- `ApplyDamage(damage <= 0)` paths
- `ApplyDamage(damage > Hitpoints.Max)` clamp / underflow

**Estimated payoff**: highest bug-find probability of any audit available.

---

## Tier 2 — Do soon (content + cohesion)

### 4. Vertical-slice playtest scenario

**Why**: 23 scenarios but no "this is the game." A player who
downloads the game today doesn't know what to do. You need ONE
playable arc: "spawn → goal → satisfaction" in 10-30 minutes. This
crystallizes which features actually matter and surfaces problems
you don't know you have.

**Concrete suggestion**: a single zone with these elements working together:

- Player spawn with a visible objective ("Reach the goblin chief in the cave")
- 3-5 hostile NPCs the player must outsmart or fight
- 1-2 friendly NPCs (use M3 ambient behaviors)
- A graveyard so M5 undertakers visibly do their thing
- A rune cultist patrolling (M6)
- A village well (M3)
- Save once mid-way, reload, finish

This is the highest-information experiment available right now.

### 5. Finish the 16×24 sprite atlas migration

**Why**: Half-shipped state. Assets are committed but rendering still
uses CP437. Half-shipped features suggest direction without delivering
it.

**What to do**: execute Phase 1 (plumbing) of
`Docs/tile-aspect-ratio-migration.md`. The end state is sprites visible
in-game.

### 6. Phase 12 — Calendar / world time

**Why**: Surprisingly high leverage for a small system. Once the game
has a day/night cycle, you unlock:

- NPC schedules (the entire M3 "ambient NPC behavior" payoff)
- Weather (M4 Interior/Exterior already laid foundations)
- Sleep/rest mechanics
- Time-gated quests

It's the foundational system that retroactively makes other systems feel alive.

---

## Tier 3 — Future foundations (queued, not urgent)

### 7. M7 — Turret system

Already planned in `Docs/goal-stack-content-gap.md`. Small (~3-4 days).
Reuses the M6 trigger-on-step pattern for a different sensor (line of
sight). Good focused-milestone warm-up.

### 8. Phase 13 — Zone lifecycle + NPC zone transitions

The big one. Currently NPCs can't follow the player between zones. This
is the prerequisite for:

- M8 (`MoveToZoneGoal`)
- Party / follower system (Phase 8)
- Multi-zone quests
- Persistent NPCs across the world

A 1-2 week investment but unlocks an enormous amount of game design space.

### 9. Phase 8 — Party / Follower system

Once Phase 13 lands, party becomes natural. "I have a companion who
follows me through dungeons" is one of those features that
fundamentally changes what the game is.

---

## Tier 4 — What to NOT do right now

- **More milestones in the M1-M6 style** without first walking the
  §3.9 priority backlog. Audit data says these don't ship bugs anymore.
  Diminishing returns.
- **More test methodology refinement.** §3.9 is solid. Adding §3.10 /
  §3.11 is yak-shaving when content is the constraint.
- **Phase 14 (AI combat intelligence) before the vertical-slice
  playtest.** You don't yet know what combat *needs* to feel good.
  Build the playtest first; let it tell you what's missing.
- **Refactoring CombatSystem before auditing it.** Audit first, fix
  the bugs you find, then refactor — refactoring without test
  coverage is how saga bugs are born.

---

## Single strongest recommendation

> **Run §3.9 audit cadence on SaveSystem THIS WEEK, with #2 (save/load
> UI) as the test harness.**

Reasoning:

- SaveSystem is the highest-EV bug-find target available
- It's the most player-facing recent feature
- It's untested in PlayMode
- The audit and the UI cross-pollinate (UI generates save/load
  traffic; audit catches corruption-class bugs the UI can't surface
  fast enough)
- Coverage on SaveSystem is 6.5% by LOC ratio (122 test LOC / 1876
  production LOC) vs 130%+ on every M-series surface
- The next "frozen-bug"-class saga is going to come from a save/load
  corner case if nothing else changes — get ahead of it

---

## Long-arc strategic note

The hardest part — getting the architecture right when no one's
playing yet — is behind us. Tier 1-2 collectively serve one goal:
**ship a vertical slice, make sure the foundations don't corrupt
under it, then iterate from real player feedback rather than from
what's elegant in the abstract.**

The methodology is sharp; the toolbox is full. The next horizon is
*players touching the game*.

---

## How this doc gets updated

When a Tier-1 item completes: move it out, demote a Tier-2 in.
When the project state shifts (new milestone shipped, new doc
landed, new audit data), refresh "Where the project actually is"
and re-rank the tiers. This doc is dated and meant to be living, not
authoritative.
