# GA03e / A09 — hauling lifecycle verification

Status: COMPLETE. Removed/destroyed loads no longer reappear when hauled, and
removing either endpoint refunds only its stored hauling penalty. Full-session
loads repair stale links after rebuilt zone indices, including wholly unplaced
nonplayer tokens. Healthy hauling, vetoed destruction, unrelated penalties and
other actors' valid grips remain intact.

The formerly unused ValidateLink is now called from movement and world repair.
Successful removal detaches captured identities. Cleanup-created grips cannot
consume the old AfterMove; a genuine nested movement retains its own follow.
Repair entries use the saved clock temporarily, preserve historical timestamps,
and restore the prior clock provider even if an observer throws.

## Executed gates

| Gate | Result | UTC interval | Seconds |
|---|---|---|---|
| red | 8/26 PASS; 18 FAIL | 2026-09-06 09:48:58Z – 2026-09-06 09:48:58Z | 0.4513278 |
| minimum | 82/82 PASS; 0 FAIL | 2026-09-06 09:57:21Z – 2026-09-06 09:57:22Z | 0.6660317 |
| adversarial | 106/108 PASS; 2 FAIL | 2026-09-06 09:58:12Z – 2026-09-06 09:58:13Z | 0.9944075 |
| hypotheses-red | 53/54 PASS; 1 FAIL | 2026-09-06 09:59:41Z – 2026-09-06 09:59:42Z | 0.6256526 |
| focused | 163/163 PASS; 0 FAIL | 2026-09-06 10:00:52Z – 2026-09-06 10:00:53Z | 1.4562462 |
| full-before-log-review | 9318/9318 PASS; 0 FAIL | 2026-09-06 10:03:47Z – 2026-09-06 10:06:15Z | 147.600372 |
| log-review-red | 31/33 PASS; 2 FAIL | 2026-09-06 10:06:54Z – 2026-09-06 10:06:55Z | 0.5351171 |
| final-focused | 167/167 PASS; 0 FAIL | 2026-09-06 10:07:54Z – 2026-09-06 10:07:55Z | 1.4057711 |
| full | 9322/9322 PASS; 0 FAIL | 2026-09-06 10:11:58Z – 2026-09-06 10:14:15Z | 137.3362033 |

Every listed test run had0C# errors. Test-authoring missing Destroy cause, native
invalid33character GUID and root namespace retry logs are retained separately;
none is substituted for runtime RED evidence. Dedicated33 plus initial26 gives
59new tests:9263→9322. Test count is not bug count.

The first dedicated sweep had two terrain-wall fixture failures: Physics-only
walls expose the separately recorded A36 arrival predicate issue. Solid-tag
terrain controls now pass; Physics-only arrivals are not claimed fixed.

Independent cold-eye report is GA03e-INDEPENDENT-REVIEW.md. Three notable
findings were resolved: replacement-grip old-event consumption, repair-entry clock,
and diagnostic intent wording. Normal removal/death uses Released; validation or
follow failure uses Slipped. Kind alone does not identify voluntary intent.

## Native queued keyboard and measured workload

- Before: 41PASS/0FAIL, run `7c0a58f6914d41c4b68df9e567adb841`, 84.3397s; 75.2867s measured/76774frames. Shutdown 84.3594s, runtime unregistered, isolated save root held through teardown and removed afterwards.
  idle: 25.0001s, 0attempts/0accepted, 0held repeats, 0failures.
  discrete: 25.0684s, 137attempts/137accepted, 0held repeats, 0failures.
  held: 25.2182s, 178attempts/178accepted, 89held repeats, 0failures.
- After: 39PASS/0FAIL, run `80191807638a490a9e1b5e21954a9981`, 82.9493s; 75.2901s measured/77031frames. Shutdown 82.9685s, runtime unregistered, isolated save root held through teardown and removed afterwards.
  idle: 25.0008s, 0attempts/0accepted, 0held repeats, 0failures.
  discrete: 25.0681s, 134attempts/134accepted, 0held repeats, 0failures.
  held: 25.2212s, 178attempts/178accepted, 89held repeats, 0failures.

Both use25s idle/discrete/held phases with the same real HaulBarrel, Strength16,
Speed100→70, structuralHP8, closed8-step route, actual C/direction/G menus and
F5/F6 controls. Before mode deliberately expects/reproduces stale-load and normal
removal resurrection; those PASS entries prove defects, not fixes. After mode
requires release/refund/no resurrection. A deliberate Period wait before final
post-fix F6 separately proves repair timestamps use the saved, older clock.

The first before run exhausted30,000sample capacity; retained capacity-failed
artifacts contain no held-frame samples and support no performance conclusion.
Final before/after capacity200,000 captures all phases. The first37check post-fix
core run and its artifacts are retained under after-core, before the log review.

### Observed marker maxima (milliseconds)

| Phase | Marker | Before max | After max |
|---|---|---|---|
| discrete | COO.ZoneRenderer.LateUpdate | 15.5864 | 14.8247 |
| discrete | COO.Input.Update | 3.8959 | 3.6845 |
| discrete | COO.Turns.EndTurn | 0.3365 | 0.2995 |
| discrete | COO.Turns.ProcessUntilPlayerTurn | 0.0688 | 0.0170 |
| discrete | COO.Turns.Tick | 0.0018 | 0.0015 |
| held | COO.ZoneRenderer.LateUpdate | 17.1013 | 24.8417 |
| held | COO.Input.Update | 1.9703 | 1.9583 |
| held | COO.Turns.EndTurn | 0.0516 | 0.0600 |
| held | COO.Turns.ProcessUntilPlayerTurn | 0.0145 | 0.0433 |
| held | COO.Turns.Tick | 0.0021 | 0.0026 |

Raw logs and per-frame/per-step CSVs are gzip-compressed losslessly; JSON includes
p99/mean/max and GC Allocated In Frame bytes. These are single uncapped headless
editor observations with scenario/input overhead and different frame counts.
They establish exercised paths and accepted cadence, not a performance speedup,
statistical equivalence, zero allocation or subjective smoothness. Movement
validation adds no collection creation or zone-wide per-move scan; loaded graph
traversal/snapshots are confined to world rebuilding.

## Honesty and scope

Can verify: script-observed native keyboard dispatch, exact load/player aliases,
positions, Speed, HP, scheduler costs, save payload receipts, stale-save recovery,
message ticks, sampled markers and teardown isolation. EditMode additionally
covers removed haulers, real combat death, successful/refused horizontal/vertical
travel, destruction veto, callbacks, wrong-zone removal and graph edge cases.

Cannot verify: physical keyboard/mouse ingestion, barrel artwork, rendered lighting,
visual polish, subjective feel or broad arbitrary event-hook atomicity. Existing
HaulBarrel is its authored yellow0 glyph and has no matching barrel sprite; no
blueprint rename or new glyph-only content was introduced. This visual debt is
recorded separately. The event marker protects hauling cleanup replacement within
a processed move; arbitrary earlier non-drag listeners are outside that contract.

No new content, save-format field, reach/weight/minimum-Speed/grab-cost change.
This is CoO-original lifecycle integration, not a Qud parity claim. Protected
Movement/Destruction/Entity/FX work remains untouched; SaveSystem's three existing
transient FX lines remain applied/unstaged and are excluded from the owned blob.
Throwing arbitrary load hooks/application rollback and A36 remain separate debt.

Next: A10 forced equipment detachment, then the remaining whole-game repair queue.
