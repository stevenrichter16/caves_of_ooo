# FLOW3 — truthful shortcuts and deliberate menu exits

Status: COMPLETE — NATIVE AND FULL REGRESSION VERIFIED. Baselinebb9b785e,8623GREEN.
Accepted smoothing step4/A23; CoO-original control repair, no Qud parity claim.

## Behavior and implementation

Pickup/container rows exclude G(close) and JK(navigation); world/dialogue rows exclude
JK while preserving usable authored world letters. One internal MenuShortcutMap
provides positional labels and key codes. World maps reserve every usable authored
preference first, then give unused letters to duplicate/reserved/unkeyed executable
rows. Raw InventoryAction references, order and Key fields remain unchanged.
CraftNoop headings stay unlabelled; exhausted rows retain navigation/Enter access.
Positional dispatch stops at alphabet exhaustion, so long lists add no unbounded
frame scan. World mapping allocations occur at Open, not each input frame.

Dialogue reveal, rendering and dispatch share the map: first mapped key while text
streams finishes reveal; the next selects. J/K remain navigation while streaming.
Pickup's hint now says Enter/key takes, with Tab-all and Esc/G-close retained.

All four affected menus record actual letter activation separately from authored
data. InputHandler captures it on closure, before world ConsumeSelection clears it,
and waits for release before held normal movement. Enter/mouse/cancel record None.
Existing four-argument world execution seam remains intact. No payment, action cost,
blueprint, sprite or save-schema change. Other menus and parked-hover behavior remain
outside this wave; dialogue's existing10-row display limit remains recorded debt.

## Source corrections and scope divergence

Pre-implementation sweep and correction table are in the smoothing plan.
- Actual seven distinct carried-and-dropped items reproduce pickup G collision.
- Chest capacity10 supports seven-item loot; Sack6 does not. Seven same-cell Sacks
  are explicitly staged container topology, not a claim of ordinary random placement.
- Break raw K collides; Indestructible suppresses the actual action.
- Actual still b/B collides; actorless forge f/F is not claimed ordinary player reach.
- Elder has at most7 visible authored choices plus Attack and optional Trade=9.
  Ten-visible-choice dialogue is staged9choices+Attack. ChoiceData has no Key field.
- Pickup excludes G too: zero-based9/10 map M/N; dialogue9 maps L. Two test
  expectations were corrected after minimum implementation, not runtime changed to
  fit mistaken arithmetic.
- Composed Unity InputTestFixture provides isolated queued player updates. Manual
  EditMode updates skip processing; Editor updates do not advance the player-step
  counter used by wasPressedThisFrame. Three raw fixture failures and one compile
  correction remain archived. No dispatch claim comes from failed delivery.
- Independent review found held-letter leakage also in pickup/container/dialogue
  closures. Valid REDs moved10,10→7,10; paired Enter stayed put. This supported
  extending activation provenance to these three menus before implementation.

## TDD and adversarial gate

27 regression plus31 dedicated adversarial plus8 native staging cases.
Valid dispatch RED27:11pass/16fail,04:49:48–50UTC,1.650369s,0compilererrors.
Minimum52:50pass/2fixture arithmetic failures,04:50:51–53UTC,1.7166655s.
Dedicated+neighbors83GREEN,04:51:56–04:52:00UTC,3.4494628s.
Staging66RED:58pass/8missing-bench,04:54:35–38UTC,3.1478145s.
Native bench initially assumed generic RemovePart; source takes a Part instance.
Compiler log retained, corrected before any native execution.

Ten attempted breaks: reserved close/navigation; single/batch case collision; late
authored preference starvation; duplicated/unkeyed/unsupported keys; inert headings;
absolute scrolling and alphabet exhaustion; raw identity preservation; reveal versus
selection; stale provenance on Consume/Open/cancel; closing held movement versus
Enter and fresh-press controls. Positive controls exercise unchanged ordinary keys,
indestructible action absence, release and normal movement, and overflow Enter.

Independent taxonomy and existing-CoO-contract cold-eye completed:0remaining
concrete production 🟡/🔴 findings. LookMode uses key-down direction handling and
returns after modal dispatch, unlike Normal's held movement; no source-proven
LookMode hold leakage found. Root review agrees. No Qud parity claim.

## Native / measurement honesty bounds

Disposable scenario stages actual seven ground drops, seven same-cell Sack contents,
Chest Break, actual still with GlimmerBrine3, and an existing Villager sprite/blueprint
with a staged conversation. Keyboard events go through real InputHandler. Reflection
observes state/rows/tiles; it does not select actions or force rendering.

Can verify script-observable selected references, shown glyphs, exact payments,
held/released position and native keyboard dispatch. Cannot verify subjective
buttery feel, broad visual quality or OS mouse delivery. FLOW1's explicit desktop
legacy-mouse limitation remains. Source mouse provenance is not live pointer proof.

The75second workload captures25seconds idle,25navigation,25Examine/reopen using
existing input/zone-render markers and whole-frame GC. Samples observe the prior
completed frame, so phase boundaries are approximate; editor/harness/input work is
included. The historical FLOW2 workload differs; it is not an identical baseline
or evidence of speedup. No causal performance improvement claimed.

Final full-suite and ownership results follow before commit.

## Final native gate and review corrections

Focused172GREEN,04:58:18–25UTC,7.3841479s,0compilererrors.
Native runfd51189b03b04facbfb18f9406646008: **50/50PASS**,
88.335126666seconds,exit0,0compilererrors,no logged native exceptions.
Final pickup now takes the last item with a held A, so all four closing letter
handoffs have native hold/release proof. Intermediate Tab-all proof remains archived.

Measured75.217160291seconds,78823frames;70/70navigation changes and
46/46confirmed Examine-closed/Normal→exact-Still-reopened cycles. All recorder
handles valid. Input max.782958ms,p99.004208ms; ZoneRenderer max3.754291ms;
whole-frame GCmax8,706,710bytes,p9920,938bytes. Raw compressed CSV and JSON
retained. No comparison/speedup or subjective feel claim.

Independent review found and fixed one 🟡 observer weakness before close-out: the
first counter only checked final open state. Stricter capture correctly failed
0/46because the driver assumed E while shipped Examine uses X. Final driver reads
the displayed binding and asserts actual closure/reopen target. Intermediate
426b0f127a58467e8c62e9febf7d6b77's46reopen count is invalid evidence; its other
functional checks and raw capture remain. Strict failed88704dcaff744870a5677e364f57c94a
retains47functional passes and the correctly failed measurement.

Other retained native route corrections: structuralHP Part instead of creature Stat;
CraftToggle automatically reopens station, so an extra C toggled the mark back off;
NPC requires actual Chat menu action; fresh L opens Look, so driver east travel uses
RightArrow. A49normal vi-L/Look debt is already documented in ALPHA-READINESS and was
reconfirmed, not newly discovered. Known A31camera teardown appears in failed logs;
final native log has none. No command rules were changed to accommodate the driver.

Asset GUID audit:2387unique,0collisions. New code metadata follows existing C#
metadata shape; no sprites/content added. InputHandler is staged incrementally so
preexisting spell changes remain separate.

Final full suite **8689/8689GREEN**,0compilererrors,0failed/skipped/inconclusive;
2026-09-06 05:13:34–05:15:32UTC,118.2246955seconds. Added66tests.
Final owned-path/index checks preserve the preexisting spell/art work: whole files
have0protected-manifest overlap; InputHandler uses only the five recorded incremental
handoff hunks. No remaining production or verification 🟡/🔴 findings.
