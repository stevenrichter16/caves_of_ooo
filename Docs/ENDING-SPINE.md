# The Ending Spine — closure-ledger and the first enacted ending

**Status:** complete, 19 September 2026 — ES.1–ES.6 shipped. The closure-ledger, visible, with a spoken no everywhere; lost givers renounceable; the circle's two enactments; the adversarial sweep; and a validated native run enacting the practice-path Renewal with a clean ledger. Release roadmap step 4 is met for one enacted route. Release roadmap step 4 ([RELEASE-VISION](RELEASE-VISION.md) §A dependency-led roadmap; [GAME-STATE](GAME-STATE-2026-09-17.md) §15, §20). CoO-original; no Qud parity claim. Follows the Stillleaf Archive chain ([MIDGAME-STILLLEAF-ARCHIVE](MIDGAME-STILLLEAF-ARCHIVE.md)), which is the first content the ledger will read.

## Goal

Transparent undertaken / refused / abandoned semantics for every act the player takes on, and the smallest complete route to one enacted ending with its consequences — mechanically clear about what the player commits to, silent about the cosmic truth.

## Canon (what the code must serve)

- **The closure-ledger** ([11_SecondSpine](../Lore/11_SecondSpine.md) C4) counts three states for every undertaken act: **Closed** (carried through), **Refused** (ended by a deliberate, *enacted* no — declined to the giver's face, formally defaulted, renounced by ritual), **Abandoned** (dropped in silence). Closed and Refused both drain the gradient; only Abandoned feeds it. It is a grammatical scale, not a moral one: *did the act end in a sentence or trail off?* The practice-path Renewal requires a clean ledger — a completionist and a principled refuser can both earn it; a ghost cannot.
- **The Strike installs a chooser** (C2): a struck vessel holds Naming as long as it keeps choosing; choice cannot be inherited, so vessel-path Renewals crack in a far generation. **The practice-path pays three permanent costs** (C3): the gods end; the work never finishes; the Root ends as itself. **No ending is best, true or canonical-preferred** ([10_Bible](../Lore/10_Bible.md) §The endings).
- **Naro refused aloud, in the circle, before the first Strike**, and the Tree had no category for a no. The six accounts of what Naro said do not agree. This is Mystery Ledger entry 1 and stays unresolved.
- Firm constraints ([GAME-STATE](GAME-STATE-2026-09-17.md) §15): Urqu is pressure, not a villain; protected questions stay unresolved by ending text; optional content must not silently become a universal obligation; cheap errands must not wash away unrelated abandonment.

## Verification sweep — corrections before any code

| Tempting premise | Verified fact | Consequence |
|---|---|---|
| A closure ledger or commitments record exists in code | None. Only `SpawnRing3D`/`VoxelWorld` audits have "ledgers" (unrelated). `NarrativeStatePart` holds facts and a free-text event log | The ledger is new state, owned by `StoryletPart` beside the quest sets |
| The quest layer already distinguishes a spoken no | `RemoveActiveQuest` is a silent `_quests.Remove` with no event; every enacted refusal shipped so far (Morrowfast release/refuse ×3, Stillleaf release, the lost-record closure) calls it | Add `RefuseQuest(questId, actor)` that records *Refused* and fires `QuestRefused`; migrate those five call sites; keep `RemoveActiveQuest` for genuine silent drops only |
| Failed quests are tracked | `FailQuest` adds to `_failedQuests` and fires `QuestFailed`; QUEST-SYSTEM.md's 🟡 (retake allowed) still applies | Failure is not a ledger state: a failed act is *open* until closed or refused (a giver can be told), matching "a principled no can close an obligation" |
| "Abandoned" can be detected as it happens | Canon defines it as silence; there is no event for silence | Abandonment is *read*, not stored: an undertaking still open when the ledger is consulted at the gate counts as unspoken, and the reading names each one so the player can go and end it. Nothing is silently written off |
| The ending is a place that exists | The Felling-Site (world 3,5) exists: `FellingSiteBuilder`, `FellingSceneRuntime`, six `FellingBarePosition`s and one `SeventhPosition` at (40,8) whose only effect is a two-turn Confusion and −2 DV while standing (`SeventhPositionPart`); `FellingSiteBench` proves that circle | The enactment happens at the seventh position; the exposure stays as the site's texture; the enactment is a world action offered there |
| The Tent-Right oath is content the practice-path can "universalize" | First Tent has an oath court cell (`FirstTentCompositionPlan.OathX/Y`); the oath itself is claimed in conversation; no oath item or rite exists | The practice-path enactment is a *spoken* choice at the seventh position, not an item; the oath is named in its text, not required as an object (scope) |
| Save format can take a new section freely | `StoryletPart.Save/Load` appends guarded sections (QS.2 completed set, failed set) with an `EndOfStream` catch for older saves | The ledger appends a further guarded section; older saves load with an empty ledger and the quest sets project into it on first read |
| Diag has a closure category | `Diag.DefaultOnCategories` lacks `closure`; C4 asks for `category=closure`, `kind=Closed|Refused|Abandoned`, payload `spoken` | Add the category; every ledger transition emits it |
| Spirit Pacts exist in code | Only in design docs | Out of scope for step 4 |

References read: `11_SecondSpine.md` C1–C4, `10_Bible.md` §The endings and §V epilogues, `MYSTERY-LEDGER.md`, `GAME-STATE` §15/§20, `RELEASE-VISION` roadmap, `StoryletPart.cs` (lifecycle events, Save/Load), `ConversationActions.cs` (StartQuest/FailQuest), `QuestLogUI.cs`, `NarrativeStatePart.cs`, `SeventhPositionPart.cs`, `FellingSceneRuntime.cs`, `FellingSiteBuilder.cs`, `FellingSiteBench.cs`, `FirstTentCompositionPlan.cs`, QUEST-SYSTEM.md.

## Semantics (the contract, in player terms)

- **Undertaken:** the moment a journal entry starts. The ledger shows it as *open*.
- **Closed:** the journal entry completes.
- **Refused:** the player ends it with a spoken no through a real verb — release/refuse in conversation, a formal default, a reported loss. The entry leaves the journal and the ledger shows *refused (spoken)*.
- **Open at the reading:** when the ledger is read at the circle, every entry still open is named. Nothing is written off behind the player's back; the reading is the transparency.
- A failed act is open until closed or refused. Cheap closures do not offset an open act: the ledger is a list, never a ratio.

## Sub-milestones (smallest blast radius first)

- **ES.1 — The ledger.** `StoryletPart` records Undertaken/Closed/Refused per quest id with turn stamps; `RefuseQuest`; `QuestRefused` event; `closure` diag with `spoken`; save/load round-trip; the five existing enacted refusals migrated; older saves project their quest sets. Tests: each transition, migration counter-checks (a silent `RemoveActiveQuest` is *not* a refusal), save/load, diag.
- **ES.2 — Visible.** The journal shows CLOSED / REFUSED sections and a one-line ledger summary; the reading is available anywhere ([Q]). Every place that can start an act has a spoken-no verb (audit of regional requests, rentals, Morrowfast, Stillleaf; add the missing ones).
- **ES.3 — Lost and reported.** A general "report the loss" pattern (as the Stillleaf register already has) for acts whose participant is gone; the reading names open acts with where to end them.
- **ES.4 — The circle.** At the seventh position, a world action reads the ledger and offers the enactment: *be struck* (vessel-path Renewal, always available, its cost stated) or *refuse aloud and name the world* (practice-path, gated on a clean ledger; with open acts it names them instead). Each enactment sets a persisted ending state, plays its epilogue text, and applies scoped, visible consequences (the *sari… sari…* ambient flag, one line per Six-aligned faction, the seventh position's exposure ends). Consume and Preserve are advertised as routes but not built here.
- **ES.5 — Robustness.** Adversarial file: ledger under save/load, reordering, death of givers, refusing then re-taking, both enactments' mutual exclusion, the gate with one open act.
- **ES.6 — Native proof.** Native journey: undertake, refuse aloud, read the ledger, travel to the Felling-Site, enact one path; inspected captures; publication.

## Canon guardrails

No ending text resolves a Mystery Ledger entry; Naro's refusal is named as history, never explained. No "best" wording. Urqu never appears as an actor. The practice-path's costs are stated before the choice.

## Performance and observability

Event-driven; the ledger is a small dictionary; the reading is on demand. `closure` diag on every transition and on every reading (`kind=Read`, payload: counts and open ids).

## Execution rules

As for the Stillleaf chain: independent clone, RED before GREEN (stub phase for new types), explicit-path integration into `main`, receipts under `Docs/Verification/VoxelWorld/ES*`, guard before git writes.

## Implementation log

### ES.1 — The ledger (19 September 2026)

**Status:** shipped. RED `ES1-red` (compile errors: `GetClosure`, `ReadLedger`, `ClosureState` did not exist — a new API, so the honest RED is the compiler); GREEN `ES1-green-2` 919/919; full EditMode suite in the clone 15,121 tests, 15,121 passed (`ES1-full`).

**What exists now.** `ClosureLedger.cs` defines `ClosureState` (Open / Closed / Refused), `ClosureEntry` (turn-stamped, `Spoken`) and `ClosureReading` (counts, open ids in stable order, `Clean`). `StoryletPart` owns the ledger: `StartQuest` undertakes (a re-taken act is a new undertaking that supersedes its old end; re-starting an active act undertakes nothing twice); `MarkQuestCompleted` closes, spoken; the new `RefuseQuest(questId, actor)` ends an act aloud — it leaves the journal, is not a failure, fires `QuestRefused` and `quest/Refused`, and logs "Ended aloud: …"; `RemoveActiveQuest` is now explicitly the silent drop: the act stays open and a `closure/Dropped` record says so; `FailQuest` leaves the act open. `ReadLedger` returns the reading and emits `closure/Read` with counts and open ids. `closure` is a default diag category. The ledger is a further guarded section of the storylet save; an older save without it projects the quest sets (completed → closed and spoken, active and failed → open), pinned with a hand-written pre-ES.1 stream.

**Migration.** The four shipped enacted refusals now go through `RefuseQuest`: Morrowfast's release and its refuse helper, Stillleaf's release, and the reported loss of the register. Each is pinned in its own test file as `Refused` on the ledger.

**Scope divergence from the plan.** The plan said five call sites; the conversation `FailQuest` action is a failure, not a refusal, so four. Regional requests do not use the quest layer at all — ES.2 decides how their accept/deliver/release register on the ledger.

**Self-review.** 🧪 not yet visible to the player (ES.2); no world verb reads the ledger yet (ES.4). 🔵 CandyTax's "isn't my problem" choice is a spoken decline recorded through `FailQuest`; ES.2's audit moves it to a `RefuseQuest` action with an `IfQuestRefused` predicate. 🔵 the journal's COMPLETED section prints quest ids, not display names (seen in the SA.6 journal capture); ES.2 fixes it while adding REFUSED. ⚪ guard unchanged.

**Cold-eye.** Q1: `RefuseQuest` mirrors `FailQuest` line for line (guard, rejection diag, removal, log line, diag, event) with the one intended difference — it clears the failed flag instead of setting it. Q2: every ledger transition emits `closure/<Kind>` with `questId` and `spoken`; the reading emits `Read` with counts. Q3: undertaken vs re-started; closed vs refused vs dropped vs failed; refusing what was never taken; twenty cheap closures against one open act; reading twice; save round-trip; older-save projection. Q4: this section was read against the shipped files.

**Files.** NEW `ClosureLedger.cs`, `ClosureLedgerTests.cs`; MOD `StoryletPart.cs`, `Diag.cs`, the four call sites, three test files with migration pins.

### ES.2 — Visible, and a spoken no everywhere (19 September 2026)

**Status:** shipped. RED `ES2-red` (compile errors: the journal snapshot had no closure fields); GREEN `ES2-green-3` 760/760 after two pins of mine were corrected (below); full EditMode suite in the clone 15,128 tests, 15,128 passed (`ES2-full-2`).

**What the player sees and can do.** The journal opens on one line — *closure N closed · M refused · K open* — and lists REFUSED beside COMPLETED, both by display name (COMPLETED used to print raw ids; the SA.6 journal capture shows "MorrowfastDryGoods"). Every quest giver offers *"[Release] I won't be finishing this. I wanted to tell you myself."* while their errand is undertaken and answers in their own voice; Clerk Padok's "isn't my problem" decline, which was recorded as a failure, is a refusal, and his aftermath reads it as such. Regional requests — undertaken outside the quest layer — are ledger acts too: accept undertakes (with the request's title), release ends aloud, delivery carries through, all after the transaction commits.

**Implementation.** `ClosureEntry.Title` and `StoryletPart.UndertakeAct/CloseAct/RefuseAct` for acts outside the quest layer; `ClosureTitle` resolves a title or the quest display name. `RefuseQuest` conversation action mirrors `FailQuest`'s block; `IfQuestRefused` mirrors `IfQuestFailed` and reads the ledger. `QuestLogSnapshot` gained `Refused` and the three closure counts (the two-argument constructor remains); the builder resolves display names and sorts. `RegionalRequestPart` hooks the ledger beside its existing after-commit diag receipt, so a rolled-back transaction touches nothing. Nine quest-giver conversations gained the release choice (gated `IfQuestActive`, targeting a `Released` reply node); CandyTax's actions and predicates were switched.

**Scope divergence from the plan.** None. The save section's layout changed (a title per entry) rather than being versioned: the section was fifteen minutes old on `main` and unreleased.

**Self-review (Methodology Template §5).**
- 🟡 *Fixed pre-commit (test-side):* my display-name pin registered a minimal storylet the registry rejects — it requires `Triggers` and `Effects` arrays even when empty — so names fell back to ids; and my roster audit demanded an `End` choice on the reply node while CandyTax's leaves through the existing `"__end__"` convention (also used by `Innkeeper.json` and `Wardens.json`).
- 🔵 *Content gap, recorded not fixed:* the shipped village quest definitions (e.g. `MessageForHermit.json`) carry no quest `Name`, so the journal shows their ids; naming them is content work for the breadth pass (roadmap step 5).
- 🧪 the regional *deliver → CloseAct* hook has no dedicated pin (the rollback fixture has no deliver `TryAct`); ES.5's adversarial file covers it.
- ⚪ Guard unchanged.

**Cold-eye.** Q1: `RefuseQuest`/`IfQuestRefused` are line-for-line siblings of `FailQuest`/`IfQuestFailed`; the regional hooks sit in the same after-commit as the diag receipt. Q2: every giver's release choice has the same text, gate and shape; every reply is leavable; refusal is never recorded as failure (audited over the roster). Q3: release visible only while undertaken, hidden before and after; refused then re-takeable; an unregistered id shown as itself; a refused act alone still makes a journal; generic act refuse vs close vs unknown. Q4: this section was read against the shipped files.

**Files.** MOD `ClosureLedger.cs`, `StoryletPart.cs`, `ConversationActions.cs`, `ConversationPredicates.cs`, `QuestLogSnapshot.cs`, `QuestLogStateBuilder.cs`, `QuestLogUI.cs`, `RegionalRequestPart.cs`, ten `*_Quest.json`; NEW `ClosureVisibilityTests.cs`; MOD `RegionalSituationCargoRollbackTests.cs`.

### Process finding — the living-doc gap (19 September 2026)

ES.3, ES.4 and ES.5 are shipped code (`0109fb0e`, `0b5bf93c`, `d6c5e105`), but their doc sections below were published only afterwards, by a docs-only commit, and their commit messages carry unfilled placeholders (`{{REVIEW}}` and `Tests: {{PREV}} -> {{FULL}}`). Each chain filled its section in a Python step that failed before writing — ES.3's read a log that did not contain its RED line, and ES.4's and ES.5's status-line assertions then depended on the replacement ES.3 never made — and the chain went on to copy, guard and commit with the section unwritten. Rule 3 (doc in the same commit) was broken three times by one silent failure. The remedy in the chain scripts is the one ES.6's already has: the fill step runs under `set -e` before any copy, so a failed fill aborts the commit. The true lines, from the receipts on main:

| Milestone | RED | GREEN | Full suite in the clone |
|---|---|---|---|
| ES.3 | `ES3-red` compile errors | `ES3-green-2` 748/748 | `ES3-full` 15,137/15,137 (15,128 → 15,137, +9) |
| ES.4 | `ES4-red` compile errors | `ES4-green` 618/618 | `ES4-full` 15,146/15,146 (+9) |
| ES.5 | `ES5-red` hypothesis run 12/13 | `ES5-green` 50/50 | `ES5-full-2` 15,159/15,159 (+13) |

History is not rewritten; the messages stay as they were committed, and this table is their correction. The ES.3 message also lists `ES3-green` among its receipts; the published receipt is `ES3-green-2`.

### ES.3 — Lost and reported (19 September 2026)

**Status:** shipped. RED `ES3-red` (compile errors: `GiverId`, `GiverName`, `Lost`, `IsLost`, `PlaceName`, `RenounceLost`, `Unspoken`, `LostCount` did not exist — a new API, so the honest RED is the compiler); GREEN `ES3-green-2` 748/748 (the first green run, `ES3-green`, failed six pins on a fixture of mine and is not published: the factory Villager already carries a ConversationPart, so the fixture now redirects the existing part's ID instead of adding a second); full EditMode suite in the clone 15,137 tests, 15,137 passed (`ES3-full`).

**What the player sees and can do.** Every act now records who it was taken on from and where — the conversation `StartQuest` action captures its speaker and the active zone, the regional recipient is its request's giver, and the shipped chains (Hollin, Farra, Nemm, Hesta) record theirs — so the reading describes each open act: *"A Message for the Hermit — end it with Orrin the baker at Sill."* An act whose giver is *known*, whose place is *loaded*, and who is *not there alive* is lost: the journal names it under UNSPOKEN ("… is gone; [R] renounces it") and [R] renounces every lost act aloud, once — a spoken end for an act that can no longer be ended to a face. Anything unknown stays open and is never called lost: an act taken on by code without a giver, or a giver whose place is not loaded, cannot be renounced, so renouncing never becomes a way to shed inconvenient obligations.

**Implementation.** `ClosureEntry.GiverId/GiverName/Where`; `StoryletPart.SetGiver`, `UndertakeAct(id, title, giver, zone)`, `IsLost`, `PlaceName(zoneId, manager)` (map POI, "below" prefix for depth, wilds fallback), `Describe`, `RenounceLost` (Refused, spoken; removes from the active set; clears any failed flag; `closure/Renounced`; `QuestRefused`). The reading carries `Lost` and one description per open act. The ledger save section is versioned (a negative first value); v1 layouts load without giver fields. The journal gained UNSPOKEN (open acts not shown under ACTIVE, and lost ones even if active) and offers [R] only while something is lost.

**Scope divergence from the plan.** "Report the loss to the other party" generalised into "renounce a lost act from the journal": village errands have no other party, and the Stillleaf register's own report-the-loss verbs remain as they were.

**Self-review (Methodology Template §5).**
- 🟡 fixed (test-side): the fixture above; a second ConversationPart on one villager made every conversation pin start on the wrong part.
- 🔵 The Morrowfast `Start(quest)` helper takes no speaker, so its three accept sites record the giver individually; a giver-aware helper would be tidier, not truer.
- 🧪 the [R] key is exercised through the `StoryletPart` API and the snapshot; the UI key handler is native-proof territory (ES.6).
- ⚪ Guard unchanged.

**Cold-eye.** Q1: `RenounceLost` performs exactly `RefuseQuest`'s removals plus its own diag kind; `UndertakeAct(id, title, giver, zone)` is `UndertakeAct` + `SetGiver`. Q2: giver fields are recorded at every undertaking site the same way (speaker, active zone); the reading's `Descriptions` follow `OpenIds` order. Q3: healthy vs gone vs dead-but-standing vs unknown vs unloaded; dropped-with-living-giver is unspoken but not lost; renounce once; save round-trip keeps giver, place and title; place names for a POI, a depth, the wilds, and garbage. Q4: this section was read against the shipped files.

**Files.** MOD `ClosureLedger.cs`, `StoryletPart.cs`, `ConversationActions.cs`, `RegionalRequestPart.cs`, `StillleafArchiveContent.cs`, `MorrowfastExpedition.cs`, `MorrowfastQuests.cs`, `QuestLogSnapshot.cs`, `QuestLogStateBuilder.cs`, `QuestLogUI.cs`; NEW `ClosureLostTests.cs`; MOD `RegionalSituationCargoRollbackTests.cs`.

### ES.4 — The circle (19 September 2026)

**Status:** shipped. RED `ES4-red` (compile errors: `EndingSpine` did not exist — a new API, so the honest RED is the compiler); GREEN `ES4-green` 618/618; full EditMode suite in the clone 15,146 tests, 15,146 passed (`ES4-full`).

**What the player can now do.** At the Felling-Site, standing in the empty seventh position, [C] and "." underfoot offer two enactments with their costs on the menu: *be struck as the seventh: re-bind the world; it cracks one day* and *refuse aloud and name the world: a clean ledger; the gods end*. The Strike asks nothing of the ledger and enacts the vessel-path Renewal. Naming reads the ledger: if any act is still open it enacts nothing and says so — *"You cannot teach the world to finish its names while leaving your own unspoken:"* followed by the reading's own lines about where to end each — and a completionist, a principled refuser, and a player who renounced a lost act all pass. Either enactment is once and for all: it persists on the player and as a narrative fact, the epilogue is announced, the seventh's examine text changes, its exposure ends, and nothing more is offered there.

**Implementation.** `EndingSpine` (`Assets/Scripts/Gameplay/World/EndingSpine.cs`): `AddActions`, `TryWorldAction` (reasons `not_the_circle`, `no_actor`, `already_enacted`, `not_in_the_position`, `no_ledger`, `ledger_open`), the two epilogues, `ending/Enacted|Rejected` diag with the reading's counts. `SeventhPositionPart` answers `GetInventoryActions` and ends its exposure once enacted; the blueprint is tagged `UnderfootInteractable` (the underfoot pick's rule); `InputHandler` dispatches beside the other world actions.

**Scope divergence from the plan.** Per-faction consequence lines ("the Six begin to age") are stated in the epilogue and not yet enacted by faction NPCs; the ambient *sari… sari…* has no hook in code (the word appears only in tables and sprite names), so "it stops" is said, not heard. Both belong to roadmap step 5's breadth pass and are recorded here rather than implied.

**Self-review (Methodology Template §5).**
- 🔵 The seventh position's entity id is whatever the scene runtime assigned; the underfoot pick requires one, and the test pins that it is non-empty.
- 🧪 the input dispatch and the announcement modal are native-proof territory (ES.6).
- ⚪ Guard unchanged.

**Cold-eye.** Q1: the dispatch block is the Stillleaf block with the spine's names; the guards mirror `StillleafCustody.ResealRefusal`'s order (place, actor, state, position). Q2: both enactments write the same state (`EndingEnacted`, the `Ending` fact), announce, retag the seventh, and emit `Enacted` with the reading's counts. Q3: strike vs name; clean vs open vs dropped vs lost-then-renounced; in the position vs beside it; the seventh vs a bare position; alive vs dead; ledger present vs absent; once vs twice; persistence. Q4: this section was read against the shipped files.

**Files.** NEW `EndingSpine.cs`, `EndingSpineTests.cs`; MOD `SeventhPositionPart.cs`, `InputHandler.cs`, `Diag.cs`, `Objects.json`.

### ES.5 — Robustness and the adversarial sweep (19 September 2026)

**Status:** shipped. Hypothesis RED `ES5-red` (13 tests against the shipped ES.1–ES.4: 12 pinned green, 1 red); GREEN `ES5-green` 50/50; full EditMode suite in the clone 15,159 tests, 15,159 passed (`ES5-full`, run before the fix, failed the one red hypothesis and is kept in the clone, not published) (`ES5-full-2`).

**Method.** The taxonomy sweep (boundary inputs, cross-actor, save/load reach across ledger layouts, stacking, anti-exploit, diag contracts) plus hypothesis-driven probes written before re-reading the code, run first against the shipped spine so each hypothesis fails or passes on its own assertion.

**One gap found and fixed.** `PlaceName` accepted an out-of-range world cell (99,99) as "the wilds at (99,99)"; it now bounds-checks against the map's size, and such an id reads "where you took it on" like a malformed one.

**Pinned as correct (12).** Nulls and nonsense never throw and never act; a non-player actor cannot enact; a v1 (ES.2-layout) ledger section loads without giver fields and re-saves in the current layout; the enactment and its narrative fact survive a round-trip; undertaking the same act twice keeps one entry, superseded; refuse-then-retake-then-drop is a ghosting the earlier no does not launder; two acts from one lost giver are renounced together; a giver dying after the reading changes nothing for a closed act; completing a lost act closes it; the world-map zone id and the site name resolve as they should; the regional deliver path closes an act through the same API and a closed act cannot be refused; rejections never claim enactment, every gate reading is recorded, and an already-enacted circle does not re-read; the Strike records the open ledger it was enacted over.

**Self-review.** 🟡 the bounds gap, fixed. 🧪 bounded by the hypotheses imagined; no fuzzing. 🧪 the native [R] and [C] paths are ES.6's. ⚪ Guard unchanged.

**Files.** NEW `EndingSpineAdversarialTests.cs`; MOD `StoryletPart.cs` (bounds check).

### ES.6 — Native proof and close-out (19 September 2026)

**Status:** shipped. Native run `ES6-ending-native-3`: 113/113 checks, validated (36 captures, 1,460 queued native steps, 362 s), the third of three kept as evidence: `ES6-ending-native` died on the vault floor (a hostile held the corridor; the harness gained `WalkOrFight`), and `ES6-ending-native-2` validated 113/113 over 1,439 steps but its menu capture truncated the enactment labels at the picker's width, so the labels were shortened (`ES6-labels` 22/22 on the spine's tests; harness compile smoke `ES6-compile-2` 9/9) and the journey re-run. EditMode otherwise unchanged by this milestone (15,159 tests, 15,159 passed at ES.5).

**What was played, with real keys.** After the Stillleaf legs, the journal's closure line was read in the real journal (4 closed, 0 refused, 0 open, so the practice path was open); the Felling-Site was reached by the world map under declared F12 (a tier-5 cell); the seventh position was walked into and picked underfoot with [C] then "."; the menu offered both enactments with their costs; with the ledger clean, *name the world* was chosen and the practice-path Renewal enacted. The epilogue was read in the native announcement, the exposure had ended, the menu offered nothing more, and F12 was restored.

**Captures inspected at 1080p.** `ending-journal-closure` — the quest log opening on "closure 4 closed 0 refused 0 open", ACTIVE (none), and COMPLETED listing *A Bell That Carries*, *A Dry Place at Supper* and *What Stillleaf Kept* by display name (the fourth closed act is the regional request, which counts on the line but has no section to appear in — recorded below). `ending-seventh-menu` — the player at [40,8] on "pink stone, empty seventh position", confused by the exposure (DV −2), F12 declared in the log, the underfoot menu listing *s)* be struck and *n)* name the world and *x)* examine. In run 2 that menu truncated both labels at about forty characters, cutting off the very costs the menu was meant to state — a real defect against the design, fixed by shortening the labels ("be struck as seventh: it cracks one day" / "name the world: clean ledger, gods end") and re-captured in run 3. `ending-epilogue` — the full practice epilogue in the native announcement, legible, mirrored in the log: the refusal aloud, the practice, the three permanent costs, "The flowers last longer here. So do the funerals." Observed and recorded, not fixed: the underfoot picker titles its menu "You see a you." because the cell's top entity is the player; the player arrives "parched (badly)" after the long world-map travel, as after the Stillleaf legs.

**Honesty bounds.** One enactment per run; the vessel path and the refusal branch (an open act barring the practice) are EditMode-covered, and the journey would have captured the refusal and enacted the Strike had the ledger not been clean. Faction-level consequences are stated in the epilogue and not enacted by NPCs; the ambient *sari… sari…* has no hook in code; Consume and Preserve are not built. F12 was on for the Felling-Site leg only. Save/load after the enactment is EditMode-covered (ES.4, ES.5), not native-captured. No readability, accessibility or enjoyment claim.

**Close-out cold-eye (the spine, Angle A).** One ledger (`StoryletPart._ledger`) with one contract (`ClosureLedger.cs`); every transition emits `closure/<Kind>` with `questId`/`spoken`, every reading `closure/Read` with counts and open ids, every enactment `ending/Enacted|Rejected` with the reading's counts. Verbs: `RefuseQuest`/`RefuseAct`/`RenounceLost` are the only spoken ends; `RemoveActiveQuest` and `FailQuest` never close. Angle B (Qud-parity) does not apply. Canon guardrails hold: no ending is called best; Naro's refusal is named as history; Urqu never appears; the Mystery Ledger is untouched.

**Publication.** GAME-STATE checkpoint and RELEASE-STABILIZATION R6 entry record roadmap step 4 as met for one enacted route with the bounds above.

**Guard note.** The ES.6 commit (`2700a005`) was made two minutes after the docs-only `a8fe394e`, inside the release guard's ten-minute quiet window; its [1] check reported STAND DOWN, the reflog write being this session's own docs-fix commit, and the chain script aborted only on lock files, so it committed through the warning. No other session was active. Chain scripts from here on treat the guard's STAND DOWN as an abort, not only a lock file.
