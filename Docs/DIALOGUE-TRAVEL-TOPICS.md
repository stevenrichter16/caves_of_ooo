# Matching travel dialogue topics

Status: implemented; 613 native EditMode regression tests PASS. Viewport-margin polish verified with 80 additional passing UI tests and a native screenshot recheck.

## Request and scope

Separate nearby settlements from wilderness reports, ensure explanatory text and selectable destinations agree, audit other NPC dialogue, and verify/polish without user intervention. Push completed task changes to main. This is an original CoO conversation repair, not a Qud source port.

The subsequent user-supplied `CavesOfOoo_Autonomous_Throwing_Audit_and_Implementation_Prompt.md` is queued after this delivery. Its full embedded appendices require section-by-section reading and exactly three additional pre-implementation audits; none of this dialogue work counts as a throwing audit or implementation. Source is `/Users/steven/Downloads/CavesOfOoo_Autonomous_Throwing_Audit_and_Implementation_Prompt.md`. Full attachment ingestion is now complete; see THROWING-EXECUTION-SOURCE-INDEX.md. Three separate pre-implementation audits are underway in parallel with the remaining dialogue validation, with no Throwing production changes.

## Baseline and verification corrections

Baseline main: `f14ec7e51` (prior Examine ledger delivery); tracked working tree clean at start. Unity 6000.3.4f1, macOS, existing instance idle in Edit mode. No user save/scene will be overwritten; native Play uses the existing private-save audit helper.

| Assumption | Verified correction |
| --- | --- |
| Settlement names are wrong labels for the rumors | The four coordinates are correct authored villages, separately chosen by RegionalGuidance; the report actions refer to different frozen encounter destinations. |
| Dialogue already describes all choices | ConversationManager.CurrentText appends only SpreadDiscoveryReports, while RefreshVisibleChoices adds both providers. RegionalGuidance has a Describe method but does not contribute it to the dialogue. |
| Only scribes are affected | Innkeeper_1.Rumors mixes the same providers; Morrowfast_Vennit.RegionOverview offers settlement choices with only a generic introduction. |
| All report destinations are nearby | Scribe/innkeeper reports are not proximity filtered. Only resident field reports have the four-cell bound; their extra peat-works lead is farther afield. |
| Splitting topics alone guarantees the dialogue fits | DialogueUI draws the first ten choices but navigates the whole list, and has no speech-height limit. An added root topic can expose hidden choices; long descriptions need bounded pages. |
| Current local NPC prose leaves suitable authored replies | LocalPeople replaces villager greetings/well/directions prose while some authored replies refer to warnings or missing people no longer mentioned. Audit and fix with paired fallback tests. |

## Design

- Scribe Start offers distinct nearby-settlement (`RegionOverview`) and wilderness-report (`WildernessReports`) topics.
- Innkeeper Start offers nearby settlements (`RegionOverview`) separately from wilderness reports (`Rumors`). Preserve gossip, rooms, trade, attack and Back behavior.
- Vennit retains his existing settlement topic; show its actual selected destinations without inventing wilderness knowledge for this role.
- Settlement topic displays the same provider's ordered names, coordinates, bearings and available local leads as its note choices. Empty eligible results say so without inventing destinations. Notes still require explicit selection and revalidate current eligibility.
- Report topic retains historical/unconfirmed semantics, origin/provenance and frozen world addresses. The text and Remember choices stay in matching order. Resident field reports remain separate from settlement choices and accurately acknowledge their farther lead.
- Add a ten-choice cursor-following window and bounded speech pages only where needed. Preserve short-dialogue layout, first-confirm reveal, hotkeys, pointer routing, no-turn semantics and previous-footprint cleanup.
- Apply bounded fixes to other proven prose/reply mismatches; document audited providers and limits instead of claiming semantic completeness for every authored conversation.

## Implementation sequence and acceptance

1. New regression tests fail on mixed topics, missing destination prose, real authored entry routes, unmentioned contextual replies and off-screen navigation/overflow.
2. Implement topic/content coupling and audit corrections, preserve note authority and no remote generation/quest acceptance. Update existing tests and native scenario routes whose topic IDs intentionally change.
3. Implement bounded dialogue rendering fixes after their RED. Countercheck short nodes, long lists/text, held controller input, mouse offsets and cleanup.
4. Run native conversation/note/provider/UI regressions, review differences, fix defects. Isolated native Play at 1280x800 and 1280x720: scribe, innkeeper, Vennit where feasible, topics, notes, Back, no-turn and controller behavior. Capture and inspect real screenshots.
5. Cold review, living doc/evidence update, coherent commit, fetch/rebase and push main. Then read the throwing brief fully and execute its required sequence.

## Evidence and review

Evidence directory: `Docs/Verification/DialogueTravelTopics/`. Audits distinguish source analysis, executed tests and live observation. Native Mac controller simulation and Deck-sized screenshots cannot establish physical Steam Deck feel or Linux behavior. No game build is needed for this dialogue fix.

### Executed evidence

- `initial-red.xml`: 76 cases, 49 passed / 27 failed before topic/UI/local-reply implementation. The failures establish the reported mixed topics, missing settlement descriptions, stale local replies and paging/choice-window defects.
- `topics-green-ui-red.xml`: 68 cases, 66 passed / 2 failed. The hidden hotkey regression stayed RED before the UI implementation. The other failure was a fixture incorrectly assuming an unrelated predicate-gated choice should be visible; corrected to compare actual predicate results.
- `green-regression.xml`: Unity EditMode, 613 passed / 0 failed / 0 skipped, 41.56 seconds, across 35 fully qualified fixtures (selected-fixtures.json). Initial short-name discovery found zero tests; discarded, never counted as PASS.
- Actual generated Sumphold scribe and innkeeper, seed729490642: distinct topics, descriptions and choices agree in real rendered screenshots at1280x800 and1280x720. Controlled setup transferred player/opened conversations; D-pad/A/B then navigated actual DialogueUI/InputHandler. This is a native modal check, not an unaided walk from spawn.
- `native-notes.json`: explicit settlement and wilderness note properties persisted on the live player; tick10 and energy1000 unchanged.
- `native-paging.json`: controlled80-line/13-choice fixture plus global Attack; RB→page1, D-pad→cursor11/window offset2, LB→page0, firstA reveals and nextA selects Chosen11. Tick/energy unchanged.
- Keyboard, pointer row mapping, hidden-key refusal, short-node behavior, clearing old footprints, topic rejection, stale report tokens and no remote-zone generation covered by the executable fixtures. Physical Steam Deck/handheld readability and an unaided human walkthrough: NOT RUN.
- Private native save isolation finished code0; editor stopped, original save root restored, owned virtual pad and custom GameView sizes removed. No user saves overwritten.

### In-phase self-review

- 🟡 Fixed: two travel providers appended choices but only wilderness provider appended prose. Separate node gates and common ordered settlement provider now couple body and offers.
- 🟡 Fixed: local prose overrides retained mismatching authored replies; exact scoped replacement now preserves predicates/actions/targets and authored fallback.
- 🟡 Fixed: hidden hotkeys and cursor could activate options beyond first ten drawn rows. Cursor-following window and visible-only activation share absolute indexing with mouse.
- 🟡 Native review: full-height45-row long popup leaves title at screen edge. Added two-row outer margin after four expected RED assertions (`margin-red.xml`); final 80/80 UI regression pass (`margin-green.xml`) and inspected `paged-margin-1280x720.png` prove the margin.
- 🔵 Independent cold source/data reviews found no additional issue. Parsed JSON comparison proved only intended conversations/fields changed; existing action/predicate and ordering behavior retained. See npc-audit.md and ui-audit.md.
- 🧪 Native Vennit visit not run; his destination body and choice provider are covered by RegionalGuidance native EditMode tests. No claim of exhaustive narrative consistency across every authored quest.

### Implementation log / files

Three conversation assets split topics and clarify local versus distant reports. ConversationManager combines eligible settlement prose and scoped LocalPeople reply presentation. RegionalGuidance and SpreadDiscoveryReports now have exclusive topic gates. DialogueUI pages long speeches and scrolls long lists while preserving reveal/confirm and short layouts. New topic/paging tests, expanded local/resident tests and migrated report fixtures/native scenario use the intended new node. All changes remain within the dialogue request; Throwing audit artifacts are a separate delivery.

Validation complete. Delivery commit contains this completed verification; fetch/rebase and remote publication are recorded in the following task checkpoint. Native setup snippets initially had two CodeDOM-only namespace/type errors; corrected before the successful checks, with no game-source compilation error. The margin RED receipt includes the separate Throwing audit regression: four passing controls and one intentionally failing floor-dagger reproduction; that Throwing failure is not a dialogue regression.
