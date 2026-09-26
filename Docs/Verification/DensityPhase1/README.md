# Density Phase 1 verification receipts

The implementation and interpretation live in `Docs/DENSITY-PHASE-1.md` §10.
These files preserve measured results; they are not a claim of Qud parity or
extended gameplay balance.

- `blueprint-diff.json`: parsed blueprint comparison against `102ba71a`;
  only the intended Creature DV stat changes in tranche 2.
- `native-editmode-first.json`: real Unity integration run exposing 14 missing
  model-coverage assertions before the rendering repair.
- `native-rendering-*.json`: focused RED, stronger batching RED, then 168 passing
  cases after rendering repair.
- `native-editmode-before-arrival.json`: 15,512 passing native tests before
  visual inspection found player/boss co-location on world-map descent.
- `native-arrival-red.json` / `native-arrival-green.json`: six failing cases
  plus four controls become ten passing cases; initialization failures are
  separately labeled and are not counted as assertion results.
- `native-editmode-final.json`: all 15,522 real Unity tests pass after the fix.
- `standalone-final.json`, `standalone-differential.txt`, XML `.gz` files and
  exclusion lists: complete comparison of old production against updated
  production with the same current test corpus; see the summary for selection
  differences. Remaining stub-environment failures are reported, not erased.
- `Native/<run-id>/report.json`: finite native new-game and world-map travel
  audit. The neighboring screenshot needs visual inspection in addition to
  the numeric report. Cleanup/inspection receipts state what was observed.

Unity 6000.3.4f1 supplies the native evidence. The standalone runner uses .NET
10.0.5, stubs and stable string hashing that does not reproduce Unity's exact
seed maps. Do not interpret its raw failure count as newly introduced failures.
Earlier temporary focused runner XML was lost during disk cleanup; historical
counts remain in the contemporaneous living doc. Durable final receipts are here.

The first native report's `player_arrival_is_safe` case checked only traps and
missed boss overlap visible in its screenshot. The final follow-up strengthens
that assertion: run `4d557e31683b41cd944ded0f18c6b871` passes all 26 numeric/native
assertions and its screenshot shows the player on a separate cell. Final
standalone results are zero newly failing and 164 newly passing; the preserved
pre-arrival comparison is in `BeforeArrivalFix/`.

The editor also logged two native memoryless-depth messages
that bypassed the audit's log callback; an `unexpectedErrors` value of zero is
not an assertion of a completely clean editor console.
