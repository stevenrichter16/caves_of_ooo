# The Ending Spine — closure-ledger and the first enacted ending

**Status:** plan and verification sweep, 19 September 2026. No production code yet. Release roadmap step 4 ([RELEASE-VISION](RELEASE-VISION.md) §A dependency-led roadmap; [GAME-STATE](GAME-STATE-2026-09-17.md) §15, §20). CoO-original; no Qud parity claim. Follows the Stillleaf Archive chain ([MIDGAME-STILLLEAF-ARCHIVE](MIDGAME-STILLLEAF-ARCHIVE.md)), which is the first content the ledger will read.

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

(appended per sub-milestone)
