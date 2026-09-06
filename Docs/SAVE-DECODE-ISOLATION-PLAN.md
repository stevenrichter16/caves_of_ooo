# Saved-session decode isolation — A08 / GA03d

Status: COMPLETE from70f704dd,9211→9263GREEN (+52). CoO-original load integration repair.
No save-format change, new content or visual asset. Existing supported v7 remains.

## Player contract

A malformed or truncated save must fail before replacing the current turn manager,
message history/announcement counters, reputation, or queued effects. A valid save
still publishes the decoded state, restores ownership/cache links, and regenerates
its status auras. The repair covers parser and section-check failures; arbitrary
custom constructors/deserializers, throwing load hooks and bootstrap apply callback
partial failures are separate audited scopes and must not be described as atomic.

## Source verification and corrections

| Source | Verified shape and correction |
|---|---|
| SaveSystem GameSessionState.Load363–402 | Clears ASCII/spell FX before header; publishes globals before final GameSession.End. Stage values until final check. |
| SaveGraphSerializer.LoadTurnManager949–971 | Parameterless TurnManager constructor publishes Active. Preserve ordinary construction; add unpublished candidate path for session decoding. |
| TurnManager constructor119, RestoreSavedState576 | Constructor only publishes Active; restoring saved fields does not invoke gameplay events. |
| SaveGraphSerializer.LoadMessageLog993 / LoadPlayerReputation1020 | Parsing currently immediately restores process globals. Separate parsed payload from publication, retaining standalone wrapper behavior. |
| SaveReader.ReadEntityBodies213–259 | Two phases: all OnAfterLoad calls then all FinalizeLoad calls. Defer both until final session check; standalone callers still run both. |
| PartRoundTripHelper84/146; split-identity tests | Standalone token graph loading relies on immediate hooks; do not remove that default. |
| StatusEffectsPart.OnAfterLoad312 | Repairs owners and emits auras; preserve valid-load regeneration after validation. Brain zone wins, active-zone fallback remains. |
| InventoryPart390/395; Body58; SkillsPart46 | Four concrete Part load hooks total. Only Inventory finalizes. No concrete Effect hook override or mutation hook was found. |
| SaveSystemAdversarialTests375/441/573 | Trailing bytes intentionally accepted, footer corruption rejected, poisoned NPC aura restored. Do not invent trailing-garbage rejection. |
| SaveGameService.LoadSlot631 | Catches decode failures and reports false; apply callback partial failure remains separate. |

Reputation and MessageLog are serialized from current globals, not from state fields:
serialize saved A while A globals are installed, then install distinct live B before
trying a decode. Preserve exact FX identities/payloads/order; Clear pools mutable
ASCII requests, so shallow snapshots that let them be reused are invalid evidence.

## Implementation and verification sequence

RED malformed header, late body/footer and truncation cases must preserve live B;
valid identical bytes must publish A. Repeat through real gzip QuickLoad and verify
zero apply callback on rejection. Footer corruption must invoke zero load hooks;
valid bytes must invoke both phases once in global phase order. Preserve no-manager,
empty state, aura regeneration, aliases and standalone graph compatibility.

Minimum implementation stages log/reputation and constructs an unpublished manager;
defers load hooks; consumes final check before publishing globals, clearing transient
FX and executing load finalization. Preserve current successful-load ordering of the
hook phases and world rebuild. No new per-frame/turn allocations or speedup claim.

Then dedicated20–30-case adversarial sweep, independent cold-eye/hypotheses, native
failed F6→successful recovery flow, full suite, daily log and attributable commit.
Protected SaveSystem and FX changes remain isolated; exact before/after attribution
is required. Any hunk depending on pending FX work needs a retained exact patch
instead of accidentally adopting the surrounding work.

Initial15-case regression fixture prepared with distinct A/B graph/globals and two
FX requests per bus. Read-only parser observer is nonthrowing in production; tests
record whether live globals remain visible before footer. ASCII pool is separately
isolated and restored so original pending/pool objects cannot be recycled by tests.

Fixture authoring compile correction: bundled NUnit lacks Assert.Multiple; replaced
with ordinary sequential assertions.3error CS log lines retained; no stale XML used.
Parser observation records booleans without throwing, then asserts after rejection.
No production edit before a compiling, executed RED.

Initial15RED:4pass/11fail,09:06:24–25UTC,.2526993s,0CS. Rejections mutate
FX/global manager, parse observers see candidate state and footer failures run hooks.
Independent pre-implementation review uncovered a second manager publication:
SettlementManager constructor assigns Current; overworld construction assigns it
again, then swaps the instance property without repairing the global alias. Added
four RED probes (two failures, valid session alias, standalone loaded alias) plus
ordinary-constructor/null-manager controls before production changes. Add unpublished
constructors for Settlement/Overworld and activate the final loaded settlement after
validation. This is a concrete scope expansion required by the same contract.

Fixture correction: both this new fixture and prior HotbarSaveFixture now restore
SettlementManager.Current, previously omitted. BodyPart monotonic ID allocation is
not current-session publication and remains outside this bounded guarantee.

Settlement-expanded21RED:6pass/15fail,09:08:27UTC,.2896609s,0CS. All four
new settlement publication probes failed; ordinary/null-manager controls pass.
Minimum implementation now stages messages/reputation, constructs unpublished
turn/settlement/overworld candidates, validates footer before hooks/publication,
and explicitly publishes the final loaded settlement instance. Standalone wrappers
retain immediate behavior; full-session hooks preserve their two-phase order.

Minimum80/80GREEN,09:13:00–01UTC,.888551s,0CS. Dedicated24-case matrix now
includes section boundaries, compressed refusal, both aura zone routes, ownership,
recovery, trailing-byte/standalone defaults, singleton-manager variants, stats, log
notification silence and exact settlement contents. A follow-on source hypothesis
probes whether the loaded settlement manager retains its POI resolver for unrecorded
Sill; matching missing-POI control included before any resolver implementation.

Dedicated24 + regression/neighbors61:60pass/1confirmed sparse-state resolver
failure,09:16:58–59UTC,.6705444s,0CS. Loaded registry had no POI lookup delegate,
so an unrecorded saved Sill could not create its well for API/dialogue predicates.
Normal first-entry already passes POI explicitly; no normal-entry failure is claimed.
Repair rebinds the existing loaded manager’s resolver from its owning loaded map,
alongside its turn provider. Preserves saved site authority and delayed publication.

Expanded104/104GREEN,09:18:57–58UTC,1.06255s,0CS after resolver binding.
Independent cold-eye reports no further must-fix. Added7hypothesis controls: saved
site authority with/without POI, cached-entry initialization with/without village,
other-village intentional no-well, standalone partial message/reputation payloads.
Dedicated total31; new test total52. Native driver prepares an owned corrupt-footer
F6 failure followed by exact payload restoration and real F6/F5 recovery.

Native authoring compiler correction: InputHandler.ZoneManager is typed as base
ZoneManager. Observe settlement ownership through the actual OverworldZoneManager
instance cast.9error CS lines/3unique locations retained; no test XML consumed.

Post-review111:110pass/1test-precondition failure,09:22:50–51UTC,1.1215885s,
0CS. The proposed other village used10,10, which is Sill itself; guard caught it.
Moved that explicit counterfixture to11,11, retaining the unequal-ID assertion.
Native review strengthened final F5 with a new success message and a changed payload
verified by a following F6; mere preexisting file presence would be insufficient.

## Close-out

Final focused111/111GREEN,09:24:32–33UTC,1.1069278s,0CS. Native22/22PASS,
run6b49f0fb7fa9468db4e77aa1e822cb81,3.1558679s,shutdown3.1740675s; exactly
one expected footer rejection and no other native error. Owned root removed after
exit. Full9263/9263GREEN,09:27:39–09:29:56UTC,137.4268484s,0CS.
2438unique Asset GUIDs/0collisions.52new tests=21regression/controls+31dedicated.

Independent sweep/review/hypotheses complete, no remaining🟡+. Existing three
FX-clear lines remain applied after validation but unstaged; exact placement patch
and before/verified/stage hashes retained. The owned SaveSystem edit is staged
without adopting those protected lines. Other protected work remains excluded.

Files: SaveSystem.cs; TurnManager.cs; SettlementManager.cs; OverworldZoneManager.cs;
GameAuditDecodeIsolationTests/AdversarialTests; GameAuditHotbarSelectionTests fixture
isolation correction; GameAuditDecodeSaveBenchPlayer/Batch and their metadata;
plan/report/raw evidence and whole-game/flow/daily logs. See GA03d-REPORT.md for
failed-authoring evidence, sparse-state classification and exception/visual bounds.
Next: A09 hauling lifecycle in HAULING-LIFECYCLE-PLAN.md.
