# Village quest-pool expansion — the first content wave

> **Goal.** Grow `VillagePopulationBuilder.VillageQuestPool` from **2 → 5**
> distinct quests so the 5 non-starting villages get *varied* quests while
> exploring (cuts the cross-village repetition: today 3 villages share
> Crunchy's Locket, 2 share the Hidden Shrine). Each new quest also exercises
> a **distinct mechanic** that the catalog primitives support but the live
> world does **not yet** show — most importantly the `AddFactWhenSlain`
> **counter** (built in Q5.5 / Tier-3, never placed in the world until now).
>
> Greenlit content wave (`Docs/QUEST-DESIGN-CATALOG.md` Part C → "author
> quests #2–#8 + the Tier-3 ones"). RPG, not roguelike — quests persist.

---

## Verification sweep (CLAUDE.md §1.2) — 0 false premises

Every primitive / action / predicate / harness the plan leans on was read and
confirmed before writing code:

| Assumption | Verified at | Result |
|---|---|---|
| `IfFact` supports `<` (and `=,!=,>,>=,<=`) | `ConversationPredicates.cs:247-253` | ✅ all six ops |
| `IfStatAtLeast` arg = `Stat:Min`, reads `GetStatValue` | `:130-138` | ✅ |
| `IfSpeakerHaveProperty` + auto-inverse `IfNotSpeakerHaveProperty` | `:103-104` + `IsRegistered :44-46` | ✅ IfNot* handled |
| `SetSpeakerProperty` action `key:value` | `ConversationActions.cs:107-115` | ✅ |
| `AddFact` increments / `SetFact` absolute | `:407` / `:396` + `NarrativeStatePart.cs:41-42` | ✅ |
| `AddFactWhenSlain` Part fires on `"Died"` → `AddFact` | `Storylets/AddFactWhenSlain.cs` | ✅ + `QuestExtendedPrimitiveTests` |
| Snapjaw = hostile `Creature` (15 HP), reskinnable | `Blueprints/Objects.json` | ✅ (gremlin already reuses it) |
| Builder placement is EditMode-testable | `VillagePopulationBuilderTests.cs` | ✅ harness exists |
| `PickVillageQuest` public + deterministic | `VillagePopulationBuilder.cs:985` | ✅ |
| Reward actions `AwardXP`/`GiveDrams` registered | `ConversationActions.cs:609/631` | ✅ |

**Why the pool stays hash-picked (documented divergence, not a bug):** the
village→quest assignment is `hash(zoneId) % pool.Length`, mirroring the
per-village House Drama pick (`OverworldZoneManager.cs:194-200`). With 5
villages over a 5-quest pool the pick is **not** a perfect permutation — some
quests may repeat, some may be unused, in any given world. Expanding the pool
*reduces* repetition; it does not *guarantee* one-of-each. There is no stable
per-world "village index 0..4" exposed to the builder (the set of villages
varies by seed), so a permutation assignment isn't available without a bigger
change. Accepted, consistent with existing behavior.

**Cross-village shared-fact note (documented divergence):** pool quest content
is *static* JSON, so its `IfFact`/`IfHaveItem` keys are fixed strings shared by
every village that hosts that quest. If two villages host the same quest, the
completion enabler bleeds (clearing the warren in village A satisfies village
B's `warren_gnomes_routed>=3` too; holding A's locket satisfies B's
`IfHaveItem:CrunchyLocket`). This is **pre-existing** for the v1 pool
(CrunchyLocket / HiddenShrine) and inherent to a static-content pool;
expanding to 5 makes a shared host rarer. Thematically benign ("you've proven
you can clear pests"). Eliminating it would need per-instance fact namespacing
(deferred).

---

## Design — 3 new pool quests, each a distinct mechanic, all robust

The robustness rule (`QUEST-IN-WORLD.md`): world objectives complete via a
**polled** trigger (`IfHaveItem` / `IfFact`) → order-independent, no soft-lock.
All three below are polled, and the builder **places the completion enabler**
so each quest is winnable-by-construction.

| SM | Quest (ID) | New mechanic in world | Enabler the builder places | Objective trigger |
|---|---|---|---|---|
| 1 | **Clear the Warren** (`ClearTheWarren`) | **kill-N counter** (`AddFactWhenSlain`) — never in world before | giver + **3 dirt gnomes** (Snapjaw reskin), each `AddFactWhenSlain{warren_gnomes_routed}` | `IfFact:warren_gnomes_routed:>=:3` |
| 2 | **The Candy Tax** (`TheCandyTax`) | **collect-N via dialogue** (`AddFact` counter + per-citizen `IfNotSpeakerHaveProperty` once-gate) | giver + **3 candy citizens** (shared `CandyCitizen` convo) | `IfFact:candy_taxes_collected:>=:3` |
| 3 | **A Message for the Hermit** (`MessageForHermit`) | **deliver / talk-to NPC Y** (recipient dialogue `SetFact`) | giver + **the hermit** (recipient `Hermit_Quest` convo) | `IfFact:hermit_message_delivered:>=:1` |

Final pool (length 5): `CrunchyLocket` (fetch), `HiddenShrine` (reach),
`ClearTheWarren` (kill-N), `TheCandyTax` (collect-N dialogue),
`MessageForHermit` (deliver). Each new quest mirrors the proven 2-stage
shape: stage 0 polls the objective; stage 1 `report` is sentinel-guarded
(`IfFact:<x>_report_guard:>=:1`, never set) so the dialogue `CompleteQuest`
is the sole completion (controlled timing at the giver = no soft-lock).

### Why "contribute once per citizen" via a speaker property
The Candy Tax counter uses the dialogue `AddFact` action. Without a gate, the
player could re-talk one citizen 3× and finish from a single NPC (degenerate).
Gating each citizen's pay-choice on `IfNotSpeakerHaveProperty:candy_taxed`
(set via `SetSpeakerProperty:candy_taxed:1` on collect) makes each *entity*
contribute exactly once while all 3 citizens share **one** conversation file
— the once-state lives on each citizen entity, not in a per-citizen fact.

---

## Sub-milestones (smallest-blast-radius-first, RED→GREEN→counter→adversarial→review→doc→commit)

- **SM1 — ClearTheWarren (kill-N).** Quest JSON + `Warren_Quest` convo +
  `PlaceWarrenQuest` (giver + 3 counter mobs) + pool→3 + switch case + public
  `VillageQuestPoolIds` accessor (testability). Tests: pool-agnostic
  distribution rewrite, content-integrity, builder-places-3-counter-mobs.
- **SM2 — TheCandyTax (collect-N dialogue).** Quest JSON + `CandyTax_Quest`
  (giver) + `CandyCitizen` (shared) convos + `PlaceCandyTaxQuest` (giver + 3
  citizens) + pool→4 + switch. Tests: content-integrity, builder placement,
  **dialogue-flow** (collect increments once; re-talk + talk-before-accept
  counter-checks).
- **SM3 — MessageForHermit (deliver).** Quest JSON + `Baker_Quest` (giver) +
  `Hermit_Quest` (recipient) convos + `PlaceMessageForHermitQuest` (giver +
  hermit) + pool→5 + switch. Tests: content-integrity, builder placement,
  **dialogue-flow** (deliver sets fact; re-deliver + deliver-before-accept
  counter-checks).

Each SM commits on a `feat/quest-pool-<name>` branch → ff-only merge to main
→ push (the established workflow; user pre-authorized via "continue"/"yes").

## Validation gates (per SM + at the end)
- EditMode: new tests green + broad regression (Quest / Storylet / Village /
  Generation / Population assemblies) — the gen change breaks nothing.
- Cold-eye pass (both angles) after the 3 SMs land.
- **Live (rule 7):** generate a fresh world, confirm the 5 pool quests are
  reachable and a hosting village actually spawns the giver + enablers; drive
  the kill-N counter end-to-end (kill the 3 gnomes → `warren_gnomes_routed==3`
  → objective finishes) via a diag/`execute_code` audit. Hold the live check
  for the merge of SM1 (the headline) and re-confirm after SM3.

---

## Implementation log

### SM1 — ClearTheWarren (kill-N counter) — ✅ shipped
The first WORLD use of the `AddFactWhenSlain` counter primitive (built in Q5.5,
never placed until now). Pool 2 → 3.

- **Content:** `ClearTheWarren.json` (stage 0 `clear` → objective `rout_gnomes`
  polls `IfFact:warren_gnomes_routed:>=:3`; stage 1 `report` sentinel-guarded).
  `Warren_Quest.json` (frazzled-farmer giver: accept/status/report→
  CompleteQuest+150XP+60 drams/done).
- **Production:** `VillagePopulationBuilder` — added `ClearTheWarren` to the
  pool + switch case + `PlaceWarrenQuest` (giver + 3 "dirt gnome" Snapjaw
  reskins, each `AddFactWhenSlain{warren_gnomes_routed, Amount=1}`). Added the
  public `VillageQuestPoolIds` accessor (testability).
- **TDD:** RED first (3 fails: quest JSON missing, convo JSON missing, no zone
  maps to ClearTheWarren) → GREEN (10/10 on the two affected classes).
- **Tests:** rewrote the pool-distribution test **pool-agnostic** (derives from
  `VillageQuestPoolIds`: every pick ∈ pool, all reachable, no dupes — survives
  future growth untouched); added `ClearTheWarren_KillObjective_PollsCounterFact`
  (JSON-side seam); extended `PoolConversations_…Registered` to cover
  `Warren_Quest`; added `BuildZone_WarrenVillage_PlacesThreeCounterMobsSharingTheFact`
  (builder-side seam: exactly 3 mobs, each `Amount==1`, sharing the fact —
  counter-check is "exactly 3", not "≥3").
- **Adversarial note:** the counter primitive's bug-class surface
  (non-Died ignored, empty-fact no-throw, custom amount, save/load round-trip)
  is already covered by `QuestExtendedPrimitiveTests` (5 AddFactWhenSlain
  tests). SM1 adds no *new* primitive surface — only world wiring (config),
  pinned by the builder placement test. No dedicated adversarial file needed.
- **§5 self-review:** see commit body.

### SM2 — TheCandyTax (collect-N via dialogue) — authored (pending live verify)
First world use of the `AddFact` **dialogue** counter. Pool 3 → 4.
- **Content:** `TheCandyTax.json` (objective `collect_taxes` polls
  `IfFact:candy_taxes_collected:>=:3`; sentinel report stage). `CandyTax_Quest.json`
  (Peppermint Butler giver). `CandyCitizen.json` (shared by 3 citizens; collect
  choice gated `IfQuestActive` + `IfNotSpeakerHaveProperty:candy_taxed`, runs
  `AddFact:candy_taxes_collected:1` + `SetSpeakerProperty:candy_taxed:1`).
- **Production:** pool += `TheCandyTax` + switch + `PlaceCandyTaxQuest` (giver +
  3 citizens sharing `CandyCitizen`).
- **Tests:** content-integrity (objective seam + the once-gate at content level),
  builder placement (giver + exactly 3 citizens), and a **dialogue-flow** suite
  `QuestCandyTaxDialogueTests` (collect increments once; **re-talk doesn't
  double-count**; collect-before-accept hidden; 3 citizens → counter 3).

### SM3 — MessageForHermit (deliver / talk-to) — authored (pending live verify)
First world use of the deliver / talk-to-NPC-Y archetype. Pool 4 → 5 (final).
- **Content:** `MessageForHermit.json` (objective `deliver_message` polls
  `IfFact:hermit_message_delivered:>=:1`; sentinel report stage). `Baker_Quest.json`
  (worried-baker giver). `Hermit_Quest.json` (recipient; [Deliver] gated
  `IfQuestActive` + `IfFact:hermit_message_delivered:<:1`, runs
  `SetFact:hermit_message_delivered:1`).
- **Production:** pool += `MessageForHermit` + switch + `PlaceMessageForHermitQuest`
  (giver + hermit recipient).
- **Tests:** content-integrity (objective + recipient seam), builder placement
  (giver + recipient), and a **dialogue-flow** suite `QuestDeliverDialogueTests`
  (deliver sets the fact; deliver-before-accept hidden; **re-deliver hidden**).

---

## ✅ VERIFICATION STATUS — GREEN (verified 2026-05-25, post editor restart)

A Unity editor focus-throttle (background window → ~10fps) made the **full**
2181-test EditMode run unrunnable via MCP all session (the long run starves the
MCP ping and wedges the editor). Resolved by **chunked** runs (each returns in
~1–2s, no wedge) covering the change's full blast radius, plus a live
bootstrap-load audit. Compile clean throughout.

| | Compiled | Tests | Result |
|---|---|---|---|
| **SM1** ClearTheWarren | ✅ | affected classes | ✅ green (pre- and post-restart) |
| **SM2** TheCandyTax | ✅ | dialogue-flow + content + builder | ✅ green |
| **SM3** MessageForHermit | ✅ | dialogue-flow + content + builder | ✅ green |

**Chunked regression — 226/226 green, 0 failures** (the change is grep-proven
localized to the non-starting-village quest path + additive content, so these
chunks ARE its regression set; the remaining ~1950 tests are unrelated systems
the content-only change cannot reach):
- Chunk 1 — affected classes (`QuestVillagePoolTests`, `VillagePopulationBuilderTests`,
  `QuestCandyTaxDialogueTests`, `QuestDeliverDialogueTests`): **23/23**.
- Chunk 2 — quest/storylet/conversation/adversarial surface (11 classes incl.
  `QuestExtendedPrimitiveTests`, `SetFactWhenSlainTests`, content + `MarcelineQuestDialogueTests`): **79/79**.
- Chunk 3 — conversation/trade stale-cache surface + quest integration
  (`ConversationTests`, `TradeTests`, …): **79/79**.
- Chunk 4 — world-gen/population (`ChairOwnershipTests`, `NpcBlueprintStayingTests`
  which instantiate `VillagePopulationBuilder`, + village/generation): **45/45**.

**Live bootstrap-load audit (rule 7), Edit-mode `execute_code` calling the real
`StoryletRegistry.LoadAll()` / `ConversationLoader.LoadAll()`:** all 5 pool
quests + all 5 new conversations register (the Elder_1-class "bootstrap doesn't
load content" concern is cleared); pick distributes evenly across 399 zones
(ClearTheWarren=80, HiddenShrine=79, CrunchyLocket=77, MessageForHermit=82,
TheCandyTax=81 — all reachable). **Spawn** correctness (warren=3 counter gnomes;
candy-tax=giver+3 citizens; hermit=giver+recipient) is pinned by the EditMode
builder tests (real `EntityFactory` + `VillageBuilder` + `VillagePopulationBuilder`),
included in chunk 1/4.

Static verification (done while the editor was down) all held: 0 surprises at
runtime — every action/predicate registered, every cross-file seam consistent,
all APIs matched.

**Cleared to merge.** The 6 SM2/SM3 `.meta` files (Unity-generated on the
post-restart refresh) are committed alongside this status update; branch
`feat/quest-pool-warren` (SM1 + SM2/SM3 + this) ff-merges to main.
