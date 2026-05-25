# Quest design catalog — what's implementable, and a suite to build

> Grounded in the ACTUAL quest vocabulary (25 predicates, 35 conversation
> actions, 4 world-object Parts, multi-stage + parallel objectives, facts,
> save/load, Q7 deeds). Part A maps quest archetypes to the primitives that
> realize them + their robustness. Part B is a brainstormed suite mapped to
> those primitives. Part C lists the 3 small primitives that unlock the most.

---

## Part A — Archetypes by feasibility

**The core rule (from QUEST-IN-WORLD.md):** objectives completed by a
*polled* trigger (`IfHaveItem`, `IfFact`) are **order-independent** → safe in
the live world. Objectives completed only by an *event Part*
(`CompleteObjectiveOnTaken`, `FinishObjectiveWhenSlain` alone) are
**timing-sensitive** → fine for scripted scenarios, risky in the wild unless
paired with a polled trigger. Every world quest below uses the polled form.

### Tier 1 — Robust in the live world TODAY (proven)
| Archetype | Primitives | Proven by |
|---|---|---|
| **Fetch / retrieve** | objective `IfHaveItem:X` (+`CompleteObjectiveOnTaken` fast-path); turn-in `TakeItem:X` to consume | RootBeerGuy (live 13/0) |
| **Slay / clear (single)** | mob `SetFactWhenSlain{Fact}` + objective `IfFact:Fact:>=:1` | RootBeerGuy gremlin (live 14/0) |
| **Deliver / talk-to** | recipient dialogue runs `SetFact` / `FinishObjective`; objective gates on it | Q3.3 + dialogue tests |
| **Investigate (fact-gated)** | `IfFact` — any condition expressible as a fact; `IfWitnessKnows` for rumor/witness leads | Q3 + witness system |

### Tier 2 — Authorable today with the existing vocabulary
| Archetype | Primitives |
|---|---|
| **Multi-stage chains** (do A → B → report) | linear stages + parallel objectives; rewards on stage OnEnter or turn-in actions |
| **Branching choices** | dialogue `SetFact` / `IfPathTaken`; later stages/choices gate on the fact |
| **Stat / reputation / faction gated** | `IfStatAtLeast`, `IfReputationAtLeast`, `IfFactionFeelingAtLeast`; reward `ChangeFactionFeeling` |
| **"Collect/visit N via dialogue"** | `AddFact:counter:1` on each talk → objective `IfFact:counter:>=:N` (AddFact *increments*) |
| **Tag-based fetch** ("bring any weapon/herb") | `IfHaveItemWithTag` + `TakeItemWithTag` |
| **Settlement repair** | `ResolveSettlementSite` + `IfSettlementSiteStage` (fix the well/lantern) |
| **Confrontation / ultimatum** | `StartAttack` (anger an NPC → it attacks); `FailQuest` on refusal; re-takeable |
| **Merchant / rental errands** | `StartTrade`, `RentItem`, `ReturnRentals`, `GiveInk`/`GiveDrams` |
| **Optional bonus objectives** + **prerequisite chains** | `Optional` flag; `QuestStarter.IfQuestCompleted`, `IfQuestCompleted` gate |

### Tier 3 — One small, well-scoped primitive away
| Archetype | What's missing | Cost |
|---|---|---|
| **Kill-N / collect-N counters** | `AddFactWhenSlain` Part (increment-on-death; mirrors `SetFactWhenSlain` but calls `AddFact`) + `IfFact:counter:>=:N`. The dialogue `AddFact` already exists. | small (1 Part + tests) |
| **Reach a location / explore** | a `QuestMarkerTriggerPart : TriggerOnStepPart` that `SetFact`s when the player steps on a cell (the abstract step-trigger base + rune subclasses already exist) | small (1 subclass + tests) |
| **Timed / deadline** | `QuestState.EnteredStageAtTurn` already exists — need an `IfStageAgeAtMost` predicate (or a tick check that `FailQuest`s on timeout) | small-medium |

### Tier 4 — Deferred
- **Escort / protect** — needs follower-survival wired to quest state (bigger).
- **Dynamic / procedural** — deliberately deferred (static authored only).

---

## Part B — The suite (Land of Ooo themed)

Ship-order favors Tier 1/2 (buildable now) first. "Place" = where it'd be
encountered; village quests use the `VillagePopulationBuilder` pattern,
dungeon quests place the target in an adjacent cave zone.

| # | Quest | Giver / place | Archetype(s) | Primitives | Tier |
|---|---|---|---|---|---|
| 1 | **The Missing Notebook** *(shipped)* | Root Beer Guy / starting village | fetch + slay → report | IfHaveItem + SetFactWhenSlain + dialogue CompleteQuest | 1 ✅ |
| 2 | **The Vampire's Errand** | Marceline / village | fetch + slay | retrieve a stolen bass-string (IfHaveItem) from a cave-beast (SetFactWhenSlain) | 1 |
| 3 | **The Enchiridion** *(content exists)* | a tomb sage / dungeon | fetch + slay + optional | find the Enchiridion (IfHaveItem) + best the guardian (SetFactWhenSlain) + loot the vault (Optional) | 1–2 |
| 4 | **The Candy Tax** | Princess Bubblegum / village | collect-via-dialogue | talk to 3 candy citizens, each `AddFact:taxes_paid:1` → `IfFact:taxes_paid:>=:3`; reward GiveDrams + faction | 2 |
| 5 | **Ice King's Love Letter** | Ice King / village edge | deliver + branch | carry a letter to a princess; deliver (IfPathTaken→Ice King rep) OR confess it's creepy (princess rep, ChangeFactionFeeling) | 2 |
| 6 | **Repair the Old Well** | Well Keeper / village | settlement repair | ResolveSettlementSite + IfSettlementSiteStage; reward village reputation | 2 |
| 7 | **Lemongrab's Decree** | Lemongrab / lemon keep | fetch + ultimatum | bring the exact item or be sentenced — refuse → StartAttack / FailQuest (re-takeable) | 2 |
| 8 | **Strongest in Ooo** | trapped citizen / village | stat-gated rescue | `[IfStatAtLeast Strength 18]` bend the bars (else "come back stronger"); reward + rep | 2 |
| 9 | **Tree Trunks' Pie** | Tree Trunks / orchard | collect-N | gather N apples — `AddFactWhenSlain`-style not needed (items): N apple pickups via `AddFact:apples:1` on a pickup Part, `IfFact:apples:>=:N`; reward a healing pie | 3 (counter) |
| 10 | **Sngggh the Marauder** | Banana Guard / cave | slay-N / clear | drive off a pack — `IfFact:marauders_routed:>=:3` via `AddFactWhenSlain` on each | 3 (counter) |
| 11 | **BMO's Lost Cartridge** | BMO / village | reach-a-location | follow clues to a spot in the woods (QuestMarkerTrigger SetFact) → IfFact; reward a fun trinket | 3 (location) |
| 12 | **Escape the Crumbling Crypt** | self-triggered / dungeon | timed | reach the exit within N turns (EnteredStageAtTurn + IfStageAgeAtMost) or the crypt collapses (FailQuest) | 3 (timed) |

**Spread:** 8 of 12 are Tier 1–2 (buildable with zero new code); the 4 Tier-3
ones share just **3 small primitives** (counter, location-trigger, deadline).

---

## Part C — The Tier-3 primitives — ✅ BUILT
All three shipped (`QuestExtendedPrimitiveTests`, 15 tests, the established
Q5.4 pattern), so the whole Tier-3 column is now buildable:
1. ✅ **`AddFactWhenSlain`** — increment-on-death; mirrors `SetFactWhenSlain`
   but calls `AddFact`. Kill-N / clear-N (N mobs share a fact →
   `IfFact:fact:>=:N`). Proven by the 3-kills→3 test.
2. ✅ **`QuestMarkerTriggerPart : TriggerOnStepPart`** — `SetFact` when the
   PLAYER steps on a placed marker (player-gated; `ConsumeOnTrigger=false` so a
   passing NPC can't despawn it). Reach-location / explore.
3. ✅ **`IfStageAgeAtMost` predicate** (`questId:maxTurns`, reads
   `EnteredStageAtTurn` vs current turn). Timed gating — gate a success
   objective on it; once the window lapses it goes false. (Active fail-on-
   timeout would still want a tick `FailQuest` — a follow-up.)

Next: author quests #2–#8 (Tier 1–2, no new code) + the Tier-3 ones (#9–#12)
now that the primitives exist, as the first content wave.
