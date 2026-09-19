# The Ending Spine — closure-ledger and the first enacted ending

**Status:** ES.1–ES.2 shipped 19 September 2026 (the closure-ledger; visible, with a spoken no everywhere). ES.3–ES.6 pending. Release roadmap step 4 ([RELEASE-VISION](RELEASE-VISION.md) §A dependency-led roadmap; [GAME-STATE](GAME-STATE-2026-09-17.md) §15, §20). CoO-original; no Qud parity claim. Follows the Stillleaf Archive chain ([MIDGAME-STILLLEAF-ARCHIVE](MIDGAME-STILLLEAF-ARCHIVE.md)), which is the first content the ledger will read.

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
