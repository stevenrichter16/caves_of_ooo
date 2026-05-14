# Caves of Ooo — Project Identity

**Authoritative statement.** Source-of-truth for what genre Caves
of Ooo is.

---

## CoO is an RPG, not a roguelike.

This is non-negotiable design-intent. Anywhere a design decision
appears to assume roguelike conventions (permadeath, run-based
progression, randomized restart) — **the assumption is wrong.**
Re-read the design with RPG framing.

### What this means concretely

| Aspect | RPG framing (correct) | Roguelike framing (incorrect) |
|---|---|---|
| Character death | Recoverable (save/load) | Run ends, restart from scratch |
| Progression | One continuous character growing over time | Per-run progress lost on death |
| World persistence | Save state persists across sessions | Maybe persistent, often not |
| Permanent choices (Pacts, Brands, Marks) | Carry full narrative weight | Reset frequently, weight diluted |
| Meta-progression | Doesn't apply — it's all *just* progression | Often a separate layer |
| Player identity | Stable character with backstory | Often interchangeable archetype |

### Where this matters for design

- **Urqu Pacts** (from IDEAS.md cosmology) carry full weight.
  Once a player makes a Pact, it sticks. There's no "next run" to
  reset it.
- **Spirit Pacts** (Inquiry / Bloodlust irreversible transformations)
  are permanent character-defining choices, as designed. The
  Recanting ritual is the only reversal path.
- **Memory-Eating consequences** persist. A character who was
  consumed by the Choir is *that character*, with all the
  downstream narrative implications.
- **Lineage / descendant-readable burial** is a within-game arc
  (the player can choose preservation methods that affect what
  future characters can read), not a cross-run meta-progression
  system.
- **Faction reputation** persists fully. Burning the Pale Curation
  is a long-term commitment, not a run-strategy.
- **The world doesn't regenerate per run.** Zones, NPCs, lore
  state, faction states all persist via SaveSystem.

### What the title "Caves of Ooo" might suggest

The name + the CP437 tilemap + the 80×25 grid resembling Qud's
ASCII-roguelike-shaped aesthetic can mislead a reader (including
future Claude sessions) into assuming roguelike conventions. Be
explicit:

- **The aesthetic is ASCII** because that's a great way to
  render dense information cheaply, not because the genre is
  roguelike.
- **The grid is 80×25** because that fits a CRT terminal feel,
  not because runs are quick disposable affairs.
- **The Qud-parity port** mirrors Qud's mechanic depth, but Qud
  itself shipped as RPG-styled (with roguelike-styled options).
  CoO is RPG-styled by default.

---

## Files that echo this statement

To prevent future-Claude-sessions from drifting back to roguelike
assumptions, this identity statement is also embedded in:

- `CLAUDE.md` (project rules — every session)
- `~/.claude/projects/-Users-steven-caves-of-ooo/memory/MEMORY.md`
  (auto-loaded memory — every session)
- `Docs/WORLD-ENVIRONMENT-OVERHAUL.md` (the brainstorm)
- `Docs/W1-WORLD-CEMENT-PLAN.md` (the locked plan)
- `Docs/WORLD-DESIGN-INTROSPECTION.md` (the critique)

If any future doc proposes a design that *only* makes sense under
roguelike framing, that's a red flag — surface it as a question
before shipping.
