# Breadth pass — the seams the campaign work left open

**Status:** B.1–B.3 shipped 19 September 2026 (the journal and the titles; Morrowfast's ground; after the ending). B.4–B.6 pending. Release roadmap step 5 ([RELEASE-VISION](RELEASE-VISION.md) §A dependency-led roadmap: "Complete release-scoped depth … defer systems whose principal contribution is scope rather than a stronger decision"). Follows the ending routes ([ENDING-ROUTES](ENDING-ROUTES.md), complete). CoO-original; no Qud parity claim.

## Goal

Close the seams the Stillleaf chain, the ending spine and the ending routes recorded as "not fixed" — places where the shipped game tells the player something untrue or leaves a promise unkept — before widening into new systems. Every item here is either a truthfulness bug the player can see or a promise an epilogue already makes. Nothing here is new scope.

## Scope (what was recorded, and where)

| Seam | Recorded in | Player-visible symptom |
|---|---|---|
| Three village quests have no name | ENDING-SPINE ES.2 | The journal shows `MessageForHermit`, `ClearTheWarren`, `HiddenShrine` as raw ids |
| Closed regional requests have no journal line | ENDING-SPINE ES.6 captures | The closure line counts a closed act that appears nowhere in the journal |
| "You see a you." | ENDING-SPINE ES.6, ENDING-ROUTES ER.6 captures | The underfoot menu title names the player instead of what is underfoot |
| Morrowfast's ground examines as "pink stone" | MaterialGuidance coverage review | Olive grass renders; the sidebar and examine call it the Stump's pink sandstone |
| Route givers keep offering after the world ended | ENDING-ROUTES backlog | Hollin still offers memory-marble "for the sealing" to a player whose world already ended |
| No voice acknowledges an ending | ENDING-SPINE ES.4, ENDING-ROUTES scope | The epilogue changes the world; nobody in it says so |
| The *sari… sari…* is never heard | ENDING-SPINE ES.4 scope divergence | Two epilogues say it stops and one says it changes pitch; it never played |
| Test isolation | ENDING-ROUTES ER.2 finding | Eight quest test classes leave a test-only storylet file in the static registry |

Recorded and **not** in this pass (they add scope, not truth): Selen's name-scene, the Reader-Salted reconciliation, Dohren's gate, a third native variant enacting Gathered, map-derived destinations in material guidance. Also moved to the day-to-day pass (next): log grammar for the player ("you is confused", "you picks up"), water on long legs (every Felling-leg capture shows the player "parched (badly)").

## Canon (what the code must serve)

- **The *sari* gradient** ([FELLING-WORLD-DESIGN](FELLING-WORLD-DESIGN.md) §2.3, §6, §7.7): tier drives *sari* frequency; it never sounds on tier-1 farmland except one scripted inciting beat at Sill; in the Grovelands it is audible at tier 3+; it is louder in the Overwrit than anywhere but the Felling-Site; "the *sari… sari…* is a MessageLog ambient system … Cheap, and the log IS this game's soundscape." The Sill story's own words: "It's the sound a name makes letting go" ([12_SariStory_Sill](../Lore/Codex/12_SariStory_Sill.md)).
- **The endings and the *sari*** ([10_Bible](../Lore/10_Bible.md) §V, [11_SecondSpine](../Lore/11_SecondSpine.md) C3): vessel-path — "the seventh position has a bearer and the *sari… sari…* stops"; Consume — "the *sari… sari…* stops: a world with one name has no seams left for the pressure to work"; practice-path — it "does not stop; it changes pitch — from the sound of naming failing to the sound of naming being done". Preserve: **canon is silent.** Derivation used here, recorded as CoO-original: the *sari* is Urqu's failure at the empty seventh; the Thinning is the Root tiring (FELLING-WORLD-DESIGN §0). Sealing the Root halts the Thinning but fills no seventh, so under Kept the *sari* continues unchanged — one of the vigil's costs.
- Guardrails: no line resolves a Mystery Ledger entry; the Choir never says "dead"; no ending is called best; Urqu never appears as an actor.

## Verification sweep — corrections before any code

| Tempting premise | Verified fact | Consequence |
|---|---|---|
| Unnamed quests fall back to something readable | `StoryletPart.QuestDisplayName` returns the raw id when `Name` is empty; `ClearTheWarren`, `HiddenShrine`, `MessageForHermit` have none | Add `Name` in the three storylet files (surgical splice) |
| Closed regional acts appear somewhere | `QuestLogStateBuilder` builds COMPLETED from `GetCompletedQuests()` only; regional acts live on the ledger (`UndertakeAct`/`CloseAct`) and are counted by the closure line but listed nowhere | COMPLETED also lists ledger entries closed outside the quest layer, by `ClosureTitle` |
| The menu title names the target | `WorldActionMenuUI.BuildTitleFor` uses `WorldInteractionSystem.DescribeCell(cell)`, which lists every non-terrain occupant — including the player, whose display name is "you" | `DescribeCell` leaves out the `Player`-tagged occupant |
| Morrowfast's ground is the Stump | Morrowfast is at (3,6), Grovelands biome, tier 3; `MorrowfastSceneRuntime.Install` lays every land cell as a `TepuiStone` ("pink stone", Stump prose) — 1,757 exterior, 95 interior, 81 solid wall cells | Name the three kinds of Morrowfast terrain per entity at install and on upgrade; the blueprint and its voxel mapping stay untouched |
| Givers gate their offers on something that ends | The `Stone`, `MuteStone` and `Thread` choices gate only on `IfNotHaveProperty`; `NarrativeStatePart` sets the fact `Ending` on every enactment; `FactBag.Get` returns 0 when unset; `IfFact` supports `=` | Add `IfFact Ending:=:0` to the three offers (content only) |
| An ambient message system exists | None: `sari` appears in code only inside epilogue strings; the only "ambient" class renders motes | New `SariAmbience`, called beside `SeventhPositionPart.OnPlayerTurnEnded` |
| Tier is available per zone | `WorldMapAuthoring.TierAt(x,y)` is the authored table (Morrowfast 3, Felling-Site and Root 5, Stillleaf 4, Quillhold 1, Salt-Vault 2); sinkhole floors are surface +1 by canon | Tier = `TierAt` + 1 below ground, capped at 5 |
| Registry pollution is one class | Eight quest test classes `Reset()` in SetUp, `Register` a test quest, and never reset in TearDown; `Register` leaves the registry marked loaded | Add `StoryletRegistry.Reset()` to their TearDown |

## Semantics (the contract, in player terms)

- The journal names every quest; closed regional requests are listed under COMPLETED by their request title.
- Opening the underfoot menu on your own cell names what is underfoot, never "you".
- Morrowfast's ground examines as grove turf outdoors, a floor indoors, and a wall where it is one.
- After any ending, no giver offers the makings of another ending; each of the four voices tied to the endings (the Recension Searcher, the Curation Indexer, the Choir's tendrils, the Palimpsest's echo) says, once asked, what changed — never which ending was right.
- **The *sari***: in a zone of tier 3 or more, at the end of a player turn, the log may carry the *sari*. Chance per turn rises with tier; lines are at least a set number of turns apart; arriving in a tier-5 place always carries it once (the loudest places are never quiet on arrival). After the vessel path or Gathered it is never heard again; after the practice path it is heard with its changed pitch; after Kept it is heard as before.

## Sub-milestones (smallest blast radius first)

- **B.1 — The journal and the titles tell the truth.** Quest names; COMPLETED lists regional closures; `DescribeCell` leaves out the player; registry reset in eight test classes.
- **B.2 — Morrowfast's ground.** Per-entity names and examine text by cell kind, at install and on upgrade of a cached zone.
- **B.3 — After the ending.** The three offers close; four voices acknowledge each of the four endings.
- **B.4 — The *sari*.** `SariAmbience`: tier gate, per-turn chance, spacing, tier-5 arrival, ending states, `event/SariHeard` diag.
- **B.5 — Adversarial sweep.**
- **B.6 — Native proof.** Both accepted journey variants rerun with new checks: the *sari* on arrival at the Root and the Felling-Site, and after Kept; the seventh's menu titled by what is underfoot; COMPLETED naming the regional request.

## Performance and observability

`SariAmbience` runs once per player turn end: two integer reads, one table lookup, one hash; no allocation (lines are `const` strings). `event/SariHeard` (zone, tier, variant, reason) only when a line is written. Morrowfast naming runs once per install. Nothing per-frame.

## Execution rules

As for the ending routes: independent clone, RED before GREEN, explicit-path integration, receipts under `Docs/Verification/VoxelWorld/B*`, native runs under `Docs/Verification/ChunkGameplayImplementation/B6-*`. Every chain checks free disk space first, fills its documents under `set -e` before any copy, treats the guard's STAND DOWN as an abort, and deletes uncompressed logs once their `.gz` twin exists.

## Implementation log

### B.1 — The journal and the titles tell the truth (19 September 2026)

**Status:** shipped. RED `B1-red` (9 of 11 failing); GREEN `B1-green-3` 518/518 (two earlier green runs are published as evidence: `B1-green` failed to compile on my `Contains` over an `IReadOnlyCollection`, and `B1-green-2` failed the registry-wide name pin on `SillHearsIt`, a quest-less storylet that `JsonUtility` still gives an empty `QuestData`); full EditMode suite in the clone 15,234 tests, 15,234 passed (`B1-full`).

**What the player sees.** The journal names *A Message for the Hermit*, *The Warren Beneath* and *The Hidden Shrine* instead of their ids. A regional request closed at its recipient is listed under COMPLETED by its request title, so the closure line and the lists agree. Opening the underfoot menu on your own cell names what is underfoot — never "You see a you." — and a pile you stand on no longer lists "you" among its items.

**Implementation.** `Name` added to three storylet files (surgical splices, parse-validated); `QuestLogStateBuilder` adds ledger entries closed outside the quest layer to COMPLETED by `ClosureTitle`, never duplicating a quest-layer completion; `WorldInteractionSystem.DescribeCell` leaves out the `Player`-tagged occupant; eight quest test classes reset the static storylet registry on TearDown.

**Self-review (Methodology Template §5).**
- 🟡 fixed (compile): my first cut called `Contains` on an `IReadOnlyCollection<string>`, which has none without LINQ (`B1-green`); the builder now looks ids up in a `HashSet`.
- 🟡 fixed (test-side): the registry-wide name pin counted `SillHearsIt`, a one-shot storylet with no quest, because `JsonUtility` fills an absent `Quest` with an empty default (`B1-green-2`); the pin now counts storylets with stages. `SillHearsIt` is canon's scripted Sill hearing of the *sari* (fact `sill_sari_heard`) — B.4 leaves tier 1 to it.
- 🔵 `IsPileCell` still counts the player (standing on one item is a "pile" for the picker's flow); only the description text changed, so interaction flow is untouched.
- ⚪ Guard unchanged.

**Files.** MOD `ClearTheWarren.json`, `HiddenShrine.json`, `MessageForHermit.json`, `QuestLogStateBuilder.cs`, `WorldInteractionSystem.cs`, eight quest test classes; NEW `BreadthJournalTruthTests.cs`.

### B.2 — Morrowfast's ground (19 September 2026)

**Status:** shipped. RED `B2-red` (compile errors (18)); GREEN `B2-green` 706/706; full EditMode suite in the clone 15,240 tests, 15,240 passed (`B2-full`).

**What the player sees.** Morrowfast's olive ground examines as *grove turf* ("Short grass over hard-packed ground, worn to bare earth along the lanes between the houses."), its pale indoor floors as *flagstone floor*, and its house walls as *house wall*. The sidebar's "Contents:" line and the underfoot title read the same. Nowhere else changes: the Felling-Site's stone is still pink stone.

**Implementation.** `MorrowfastSceneRuntime.NameTerrain` names each authored land cell by its definition flags (solid and opaque → wall; interior → floor; otherwise ground) on fresh install and on every upgrade of a cached zone, so older saves read the same. The blueprint stays `TepuiStone`, so the voxel mapping, which keys on the blueprint, is untouched. Names persist through the entity round trip.

**Self-review (Methodology Template §5).**
- 🔵 No finding beyond the pins surfaced in the RED→GREEN cycle.
- ⚪ Guard unchanged.

**Files.** MOD `MorrowfastSceneRuntime.cs`; NEW `MorrowfastGroundNamesTests.cs`.

### B.3 — After the ending (19 September 2026)

**Status:** shipped. RED `B3-red` (7 of 12 failing); GREEN `B3-green` 378/378; full EditMode suite in the clone 15,252 tests, 15,252 passed (`B3-full`).

**What the player sees.** Once any ending is enacted, Hollin no longer offers memory-marble, the Indexer no longer offers mute-stone, and no tendril offers a thread to carry down. Hollin, the Indexer, any choir tendril and the Palimpsest's echo each gain a *What has changed?* line that appears only after an ending and answers for the ending actually enacted — four voices, four endings, sixteen short answers, none of which calls an ending right or explains the seventh's refusal. Before any ending nothing changes.

**Implementation.** Content only: `IfFact Ending:=:0` on the three offers (`NarrativeStatePart` sets the `Ending` fact on every enactment; an unset fact reads 0); four `IfFact Ending:=:N` choices and `After1`–`After4` nodes per speaker in `StillleafArchive.json`, `RotChoir.json` and `Palimpsest.json` (surgical splices, parse-validated; the apply step asserts the Choir's voice gate and the guardrails over the new nodes).

**Self-review (Methodology Template §5).**
- 🔵 No finding beyond the pins surfaced in the RED→GREEN cycle.
- 🔵 After Gathered the epilogue says no one remains, yet the speakers still speak: consequences are stated, not enacted (the plan's bound). The Gathered lines are written as the gathering under way.
- ⚪ Guard unchanged.

**Files.** MOD `Conversations/StillleafArchive.json`, `Conversations/RotChoir.json`, `Conversations/Palimpsest.json`; NEW `AfterTheEndingVoicesTests.cs`.
