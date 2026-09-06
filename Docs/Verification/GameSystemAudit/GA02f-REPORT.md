# GA02f — quantities and configured carry penalties

Status: COMPLETE. Baseline0345ef46,8122/8122.

A05 remaining quantity writers could change a handled stack without refreshing
its carrier's Speed penalty. Live weight already follows quantity; this wave
repairs the separate cached handling contribution. No shipped blueprint currently
sets nonzero CarryMovePenalty: tests and native arena explicitly configure that
supported field on actual content. The unrelated Speed penalty7 is preserved.

## References and corrections

Root read InventoryPart426–445/512, StackerPart quantity/clone callers, all six
service writers, actual seed/mineral/tinker/brew/forge/temper content and tests,
TurnManager speed consumption, and local Qud Stacker1–114 before implementation.
Qud's StackCount property raises an event; CoO keeps its public field because
blueprints and v7 saves reflect fields. This is CoO behavior, not Qud parity.

- Weight needs no cache fix. Equipped and contained objects do not count as carried.
- Successful output AddObject often already refreshes; standalone consumption,
  no-output Mishap, mixed restoration order and ingredient-free rollback reproduce.
- All authored V1 tinkering builds make1; NumberMade2 rollback is a supported
  recipe override. Missing-output tests explicitly remove a blueprint.
- First RED38 included one missing LogAssert expectation in an injected failure.
  Corrected and expanded RED42 ran before production; no fixture mistake is a bug.
- Diagnostics describe an applied Speed change, not tracker-only changes when
  the actor has no Speed stat.

## Implementation

Stacker MergeFrom refreshes both actual carriers after both counts change, once
for a shared owner. SplitStack/RemoveOne refresh the carried source before cloning.
Owner lookup requires Physics.InInventory AND actual Inventory.Objects membership.
Ground/container/equipped/detached objects cannot trigger a guessed owner's refresh.
Six service files explicitly refresh partial consumption and completed restoration:
seed planting, mineral gifting, quenching, tinkering, brewing and forging.
CarryPenaltyRefreshed reports actor and previous/current contribution plus delta
only on a real applied change with the event diagnostic channel enabled.

No saved fields, public count representation, normal per-frame/per-turn hooks,
new art, weight semantics, payment or target-unit semantics changed. A45–A47
crafting integrity remains separate. No performance improvement is claimed.

## Evidence so far

| Artifact | Result | UTC |
|---|---|---|
| GA02f-red.xml.gz |38:25pass/13fail;12real+1fixture-log|00:40:07–09|
| GA02f-expanded-red.xml.gz |42:28pass/14fail;corrected before production|00:42:29–32|
| GA02f-green.xml.gz |824/824 focused and neighboring tests|00:44:42–50|
| GA02f-adversarial-red.xml.gz |80/87;42reg+38adv pass,7 absent-arena RED|00:56:05–09|

All listed runs have0 C# errors. Subsequent strengthened tests, full suite and native capture are recorded below.

## Review and bounds

Independent actual-code review:0must-fix. Owner membership, source-before-clone,
both-owner merge, idempotent outer rollback and no callback boundary verified.
Root is strengthening partial-output rollback with intermediate diagnostic evidence
and missing-content preflight with exact inventory/stat/factory preservation.

Native planned: actual keyboard inventory PlantSeed, paid PaleSalt infusion,
disassembly, FireMoss Mishap and movement. Can verify item quantities, actual
configured penalty/speed, crop placement, enhancement/payment and diagnostics.
Cannot verify visual readability/feel; gifting/quenching, save, hauling and
rollback remain EditMode evidence. The scenario does not claim authored slowing
items or a native measurement of scheduler throughput.

## Adversarial and native completion

Expanded neighboring suite869/869 GREEN,00:58:04–14UTC. Strengthened final
focused89/89 GREEN,01:00:01–05UTC:42regression,40dedicatedadversarial,7staging.
The added source-only carrier merge pair and stronger intermediate rollback/
preflight assertions close review gaps. All checks use current production code.

Ten cold-eye hypotheses attempted,0newproductionmust-fix findings:

1. False owner reference refresh: ground/container/equipped/forged owner/missing
   physics controls deliberately leave an unrelated tracker stale to detect it.
2. No-op mutation refresh: invalid splits, singleton RemoveOne and full merge
   preserve both quantity and deliberately pending unrelated contributions.
3. Merge asymmetry: destination-only/source-only/two-owner/shared-owner behavior.
4. Double charging: same-owner conservation and repeated refresh stay quiet.
5. Missing Speed: safe, no diagnostic falsely claiming a Speed change.
6. Save/clone drift: token round-trip→split→remerge→split preserves the tracker.
7. Other penalties erased: real hauling and repeated release preserve carry and7.
8. Callback ordering: BeforeEquip observes reduced count and penalty; veto
   restores both, and a subsequent split still computes correctly.
9. Restoration order: six service writers, ordered singleton/stack restoration,
   Tinker early return and ingredient-free partial-output rollback.
10. Native route: inventory/mod/crafting/still routes checked against handlers;
    preview gating required a fixture correction recorded below.

Native firstrun a1e8a9f9e2784767994780a596a1a887,7.437534584s,26/27PASS,
exit1/0C#errors: crafting inventory refuses unstable preview before dispatch.
The failed raw log/report are retained. Corrected driver first proves that
refusal preserves quantity/penalty, then uses native C/right→actual still's
BrewMix command. No production change was made for this test assumption.

Native finalrun **ea8983e91fae4030b09538aab676b870**, **38/38PASS**,8.768810292s,
exit0/0C#errors. Evidence: GA02f-native.json and GA02f-native.log.gz. No
MissingReferenceException, NullReferenceException or ObjectDisposedException in
this successful capture. It verifies actual PlantSeed/crop, paid PaleSalt effect,
Dagger disassembly yielding exactly one B, refused inventory preview, committed
still Mishap spending one FireMoss and dealing2nonlethal damage, and native walks.
Applied contributions track55→51→47→43→39 totalSpeed.Penalty with speeds
45→49→53→57→61, exactly four-4delta records. No manual refresh in the driver.

## Self-review

- 🟡 Original A05 quantity paths repaired after corrected RED14/42.
- 🔵 Review assertion weaknesses fixed: intermediate merge/rollback diagnostics,
  exact preflight state and asymmetric owner counterchecks. These are test fixes,
  not additional gameplay bugs.
- 🔵 Fixture corrections: expected missing-blueprint log; Dagger's actualB yield;
  inventory preview refusal versus still's BrewMix route. Raw failures retained.
- ⚪ No authored nonzero carry penalty, new saved fields, art or hot-path hook.
  A45–A47 payment/target/receipt issues stay separate; ordinary raw field writes
  outside the repaired producers still require their owner to request refresh.
- 🧪 Native checks controls and script state, not pixels/feel, scheduler timing,
  gifting/quenching, save/drag or rollback. Those latter invariants have EditMode
  coverage. No75sperformance requirement applies to these one-shot changes.

Initial ownership audit32files,0protectedoverlap;2346uniqueGUIDs,0collisions.
Final file list and full-suite result are recorded at close-out below.


## Final verification

Full **8211/8211GREEN**,01:04:56–01:06:31UTC,94.7958239s,zeroC#errors;
GA02f-full-green.xml.gz. Baseline8122→8211(+89). Focused and native details above.
Owned runtime changes are the eight listed quantity/cache files; six new C# files
and their metadata provide regression/adversarial/staging plus native bench/player/
launcher. Audit/daily docs and all GA02f raw/report artifacts ship together.
All2346GUIDs are unique. No protected preexisting path overlaps this wave.
