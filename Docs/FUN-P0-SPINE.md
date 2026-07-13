# FUN-P0 — The Playability Spine

> **Status:** In progress (M1 → M2 → M3, strict order)
> **Branch:** `feat/fun-p0-spine`
> **Goal:** Convert the shipped systems-sandbox into a game that generates
> the want-to-play loop: *I might die → so I want the sword → which is
> THERE on the map → and when I take it the game says so and hands me the
> next thread.*
> **Origin:** 2026-07-12 fun-gap analysis (7-agent code-grounded audit +
> adversarial design judge). This doc is the master; per-move plans live in
> `FUN-P0-M1-MORTAL-START.md`, `FUN-P0-M2-REWARD-GEOGRAPHY.md`,
> `FUN-P0-M3-THREAD-FORWARD.md`.

---

## 1. Diagnosis (why the game plays shallow)

The engine is deep and tested; the *content layer never feeds it*, and the
debug-first loadout ships the **end state of the progression loop as turn
one**. `GameBootstrap` grants 500 HP, six attack mutations, every tinker
recipe, eight tonics, and free weapons — so threat, XP, drams, tonics,
crafting, loot, and rentals all compute correct answers to questions the
player never asks. The first-hour trace found the motivation loop dies at
minute 75–90, when the second village proves the world is a photocopy
(same layout, same always-Fouled well, same NPC roster, same 6-quest pool,
same full grimoire chest).

The repeating pattern — **system exists, nothing feeds it**:

| Deep system (built + tested) | Starvation |
|---|---|
| NPC spellcasting path (`KillGoal → TryUseRangedAbility`) | zero creatures have abilities |
| XP/leveling (Qud cubic curve) | bosses award less XP than a snapjaw; most creatures award 0 |
| 147 nodes of faction-ambassador dialogue | ambassadors spawned by nothing |
| Quest engine `QuestCompleted` GameEvents | only listener is a dev bench — completions are silent |
| HouseDrama engine | Vex conversation IDs dangle (C-press silently no-ops) |
| Ink/rental economy (43 adversarial tests) | zero `GiveInk` in content — ink unearnable |
| 12 unique elemental weapons + enhancement minerals | no spawn path outside dev scenarios |
| Fog-of-war (computed + serialized) | no renderer consumes it |

**Design rule for this feature:** P0 adds *wiring and content* against
tested systems. New engine surface only where a gap has no existing
mechanism (quest-event announcer). Anything that smells like a new
system is out of scope (deferred to P1+, see §6).

## 2. The three moves (strict order)

1. **M1 — Mortal Start.** One `DebugLoadout` flag (default OFF in play
   builds); real Level-1 statline; per-creature natural-weapon pass;
   XPValue pass (bosses 150–400); MP stat wire; skill re-costing with
   `Requires` chains. *Switches on threat, leveling, healing, shopping,
   and crafting simultaneously.*
2. **M2 — Reward Geography.** Grimoire chest restricted to the starting
   village and cut to a starter subset; remaining grimoires distributed
   (scribe stock, element-keyed lair-boss drops, ambassador rewards);
   lair bosses drop unique weapons + LockedChest treasure; one faction
   ambassador spawned per village keyed off `PointOfInterest.Faction`.
   *The second village disproves the photocopy instead of confirming it.*
3. **M3 — Thread Forward + The Game Speaks.** Quest/objective/arrival
   MessageLog announcements; mute-Vex fix; 2–3 cross-zone quests naming
   map destinations; completion-node pointer choices chaining the
   existing quests; promote the three orphaned quests (Enchiridion,
   IronKey, CinnamonBun) into the world pool. *There is always a named
   next want, and the game acknowledges deeds.*

## 3. Verification sweep (methodology §1.2)

Every file:line claim the moves build on was re-verified against source
before implementation. Corrections table: see §3 of each move doc.
Sweep run: 2026-07-12, 6 parallel verifiers. Findings that changed the
plan are called out inline in the move docs as **[SWEEP]** notes.

## 4. Test discipline

- Baseline (pre-implementation, branch point `c7fdd5a` + user WIP):
  **5,203 EditMode tests, 10 pre-existing failures** (StatusEffect color
  ×2, TurnManagerAdversarial ×7, VenomDagger ×1 — all in the user's
  uncommitted exercise areas; NOT touched by this feature).
- Gate per sub-milestone: no new failures beyond the baseline 10.
- Editor runs unfocused → stale-assembly trap applies: after each edit
  cycle, verify compilation actually landed (new-test-count drift) before
  trusting green.
- TDD per sub-milestone: RED → GREEN → counter-check → adversarial where
  the taxonomy applies (§CLAUDE.md).

## 5. Commit / merge plan

Each sub-milestone commits on `feat/fun-p0-spine` using the §2.3
template, doc updated in the same commit. Merge to `main` after final
gates (cold-eye both angles + adversarial sweep + PlayMode sanity).
User's uncommitted files (`PaperSkin.cs`, `Packages/*`, `Assets/Plugins/`)
are never staged.

## 6. Scope prunes (with rationale)

- **Thinning world-pressure scalar** — cut by the design judge: builds a
  NEW system in a codebase whose diagnosis is "systems starved of
  content". Revisit post-P1.
- **Character creation / origins** — P2. No first-hour death traces to
  its absence; the mortal statline is the P0 slice of it.
- **Fog-of-war rendering, pack encounters, monster mutations, Village
  Book main quest, displacement death, ink faucet, descriptions pass,
  graphics-polish un-gating** — P1 workstreams, sequenced after this
  spine (see fun-gap analysis synthesis).

## 7. Status dashboard

| Move | Sub-milestone | Status | Commit |
|---|---|---|---|
| M1 | a. DebugLoadout flag + real statline | pending | |
| M1 | b. Natural-weapon pass | pending | |
| M1 | c. XPValue pass | pending | |
| M1 | d. MP stat wire | pending | |
| M1 | e. Skill re-costing + Requires | pending | |
| M2 | a. Starter chest cut + distribution | pending | |
| M2 | b. Lair boss drops + LockedChest | pending | |
| M2 | c. Ambassadors per village | pending | |
| M3 | a. Quest/arrival announcer | pending | |
| M3 | b. Vex mute fix | pending | |
| M3 | c. Cross-zone quests | pending | |
| M3 | d. Quest chaining + orphan promotion | pending | |
| Final | cold-eye + adversarial + PlayMode + merge | pending | |
