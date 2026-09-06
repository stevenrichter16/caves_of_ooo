# GA03d / A08 — rejected-save isolation and loaded settlement wiring

Status: COMPLETE; full9263/9263GREEN (+52), native22/22PASS. Baseline70f704dd,
9211GREEN. CoO-original save integration repair, without format/content changes.

## Result and boundary

Session parsing now builds unpublished turn/overworld/settlement candidates and
stages message/reputation payloads. The final section check succeeds before globals
publish or load hooks run. Rejection preserves current manager identities, actor,
messages/announcements/counters, reputation and both pending FX queues. Success
publishes the actual loaded settlement registry and reconstructs its POI lookup from
the owning loaded map. Existing saved sites retain authority.

Standalone readers retain immediate publication/finalization. Full sessions run all
OnAfterLoad hooks before any FinalizeLoad hook, then rebuild candidate world indices
and appearance. Null saved managers retain prior behavior. Existing trailing bytes
remain accepted. The supported save format remains v7.

The guarantee covers parser/section-check failures. It does not cover arbitrary
custom constructors/deserializers, throwing load hooks, or bootstrap apply-callback
partial failure. Body ID allocation may advance during an abandoned parse; ordinary
skipped positive IDs do not mutate live anatomy. Extreme ID overflow is separately
recorded validation debt, not a completed repair here.

## TDD and review evidence

| Gate | Raw result |
|---|---|
| Test authoring compile |3error CS lines: bundled NUnit lacks Assert.Multiple; corrected to ordinary assertions; no XML consumed |
| Initial RED |15total,4pass/11fail;09:06:24–25UTC,.2526993s;0CS |
| Settlement RED expansion |21total,6pass/15fail;09:08:27UTC,.2896609s;0CS |
| Minimum + neighbors |80/80GREEN;09:13:00–01UTC,.888551s;0CS |
| Dedicated24 + neighbors |61total,60pass/1sparse-state resolver failure;09:16:58–59UTC,.6705444s;0CS |
| Resolver fix + neighbors |104/104GREEN;09:18:57–58UTC,1.06255s;0CS |
| Native driver authoring compile |9error CS lines/3locations: base ZoneManager lacks SettlementManager; corrected actual-overworld observation casts; no XML consumed |
| Post-review controls first |111total,110pass/1fixture coordinate guard;09:22:50–51UTC,1.1215885s;0CS |
| Final focused |111/111GREEN;09:24:32–33UTC,1.1069278s;0CS |
| Native |22/22PASS;6b49f0fb7fa9468db4e77aa1e822cb81;3.1558679s,shutdown3.1740675s;0CS;exactly1expected rejection |
| Full |9263/9263GREEN;09:27:39–09:29:56UTC,137.4268484s;0CS |

New tests52 =21regression/controls +31dedicated adversarial/hypothesis cases. The
initial failures demonstrate premature publication/FX clearing and pre-footer hooks;
review adds wrong settlement publication/identity, then the sparse-state missing
resolver. Failure counts are not distinct gameplay bug counts. Valid load, standalone
behavior and null-manager cases are already-correct controls.

## Findings and disposition

- 🟡 Independent sweep found SettlementManager.Current changes twice while decoding,
  then points at a default registry instead of the loaded one. RED-tested failure
  preservation and successful exact identity; fixed unpublished construction and
  explicit final publication.
- 🟡 Loaded registry had a null POI resolver. Dedicated RED reproduces an unrecorded
  saved Sill failing to create its well. Resolver now binds to the loaded map.
  This is sparse-save/API robustness: ordinary entry supplies the POI explicitly;
  the test does not establish a normal first-visit failure.
- 🟡 Fixed fixture isolation omission for SettlementManager.Current in both the new
  fixture and the previously authored hotbar fixture. Original pending FX objects
  remain outside test queues; original ASCII pool objects/order are separately
  preserved, preventing reuse from corrupting borrowed snapshots.
- 🔵 “Other village” fixture initially used10,10, which is Sill. An explicit unequal-ID
  guard failed; moved the counterfixture to11,11 and retained failed evidence.
- 🟡 Native final F5 proof initially checked only preexisting files. Strengthened to
  a new success message and a changed checkpoint value subsequently restored by F6.
- ⚪ Independent cold-eye finds no additional must-fix in publication, two-phase
  hooks, standalone defaults, or pooled-request ownership. Candidate world rebuild
  and shipped narrative/knowledge/storylet deserializers affect candidate objects.

Hypotheses/controls include full request fields/path copies and order, parser-time
observation before rollback could hide a change, malformed middle/late sections,
compressed rejection, failure→failure→valid recovery, aura owner/zone/duration,
Brain-zone and active fallback, inventory aliases, saved Speed/cooldown, one missing
manager, silent log restoration, saved site authority with/without POI, other-village
intentional no-well behavior, and ordinary cached-entry redundancy. All post-fix
controls pass; no speculative gameplay changes followed already-correct hypotheses.

## Native and performance bounds

Completed live audit uses real bootstrap N/F5/F6 through queued InputSystem keys.
It corrupts only the final raw section check inside an owned gzip checkpoint, allows
exactly that one expected logged rejection, checks unchanged live actor/managers/
reputation/history plus the controller's truthful appended failure message, restores
the exact valid payload, and retries F6/F5 with a subsequently loaded changed value.
Fixture marker/reputation changes and file corruption are explicit setup, not input
evidence. FX request preservation remains EditMode evidence.

Can verify: script-observed keys, state/identity, costs, actual disk payloads, expected
error count, owned paths and normal teardown. Cannot verify: physical keyboard input,
pixels, aura appearance, subjective feel or performance speedup. No new frame/turn
listener or per-frame allocation is added; this work occurs during load/construction.

## Attribution

The three preexisting transient FX-clear lines in SaveSystem move to the validated
publication boundary. They remain applied but unstaged, with their exact final
placement retained in GA03d-transient-fx-placement.patch and hashes in source-attribution
JSON. The staged SaveSystem contains only this wave's owned implementation. Its
before copy differs from HEAD only by those three protected lines; this is verified.
Other protected animation/art work remains excluded. All Assets metadata, including
ignored paths:2438unique GUIDs,0collisions.

Final native JSON includes the normal teardown assertion, appended after initial
completion. Root removal verified after process exit. No unexpected native errors;
the single exact corrupt-footer log is expected and explicitly counted.
