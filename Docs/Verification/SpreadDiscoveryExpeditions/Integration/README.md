# Spread discovery integration closeout

Baseline: `6015ba8b9c31bfe52658f8ec8b0bcf6f98218a14`. Gameplay production stayed unchanged throughout the final full native run and its later test-only follow-up. This is original Spread content using the approved existing biome models.

## Actual tests

The native Unity EditMode sweep ran 20,633 cases: **20,632 passed, one failed, none skipped**, in 876.8094541 seconds (2026-09-28 00:27:14–00:41:51 UTC). `full-native-first.xml.gz` is the unchanged authoritative receipt; `full-native-first.json` records its hash and failure. The full run is not relabeled green.

The sole failure was the pre-existing `LightSourceFlickerPartTests.HandleEvent_RenderFires_UpdatesIntensity`. Thirty synchronous render events all sample the same frame time; a legitimate noise midpoint can retain base intensity. The original test and lighting production were identical to the baseline. The exact failing time/noise sample was not recorded, so the receipt does not reconstruct its numeric cause. The corrected test primes the actual implementation, replaces intensity with a sentinel and requires Render dispatch to restore the same-frame value. Separate variation/bounds controls remain. A private severed-hook countercheck correctly fails this test with six controls passing. No lighting runtime code changed; see `LightFlickerFixture/`.

After the full run, only that existing test and a new two-case persistence fixture changed under Assets. Native follow-up job `79b6620947d042e5b89a39f37c32761c` passed **50/50**, zero failed/skipped, in 7.5276215 seconds: two wilderness persistence cases, two existing regional container lifecycle cases, 39 Wayhouse adversarial cases and seven lighting cases. The XML is `../Tests/final-persistence-lighting-green.xml`. The resulting registry has 20,635 unique cases, but there was no single all-green 20,635-case full sweep. These overlapping run counts must not be summed.

The two new cases use real rolled stock and ordinary TakeFromContainer commands. Full capacity refuses without losing stock; then a successful partial take leaves remaining supplies. Moving away, full serialization, loading and cached return retain exact cache/guard coordinates, stock IDs/quantities/backlinks, taken item ownership and guard gear. Bounded synthetic fixture selection and explicit player transfers make these core integration tests, not ordinary player journeys.

`full-run-inputs.json.gz` records 2,437 source/configuration inputs captured during the frozen full run; `full-run-input-drift.json` confirms none changed before completion. `post-full-test-only-delta.json` records the later narrow test changes. No Objects.json, model, renderer, gameplay or native-driver changes followed that full run.

## Native player evidence

See `../Native/README.md`, raw reports and `native-visual-review.json` for exact setup and inspected images:

- Main generated-site route: 19/19 twice, including final goods validation. Actual reports/notes, grain/stubble, front key/combat/door/reward/equip, separately restored live-guard rear branch and depleted F5/F6 state. Transfers to the field/site are disclosed.
- Ordinary grain round trip: 14/14, without transfers or grants. New character → Sill reports → actual field → harvest/eat the earned grain → Sill return → saved consumed/spent state.
- Generated-source cards: 9/9. Actual cargo/shelter/clue models and readers, plus 24 paid normal steps across the shelter bypass without player damage. Starting view/border transfers are disclosed.

The uninterrupted town→Wayhouse→town→depleted-site journey and a separate shelter combat journey remain unobserved. These are explicit playtest limits, not inferred gameplay defects. Eating at full HP proves consumption, not healing. Cached wilderness preservation does not introduce permanent retention after explicit ordinary zone unload.

## Review and performance

Focused RED/GREEN and counterchecks, three-seed source/value/conservation censuses, and independent Q1–Q4 review accompany the living plan. Corrected false-premise runs remain in the index. The final review repaired actual Wayhouse fixed-goods admission at creation and final generation: one ordinary value20 Buckler and one reusable NoTrade key. Existing saved sites are not rebuilt.

`Performance/README.md` records 60 alternating same-process component samples and the current dense native profile. Generation adds bounded cold CPU work; no renderer change is introduced. Native allocation counters were unusable. These measurements do not demonstrate whole-game, GPU, all-seed, long-term economy or causal before/after performance neutrality.

Owned sources/tests/metas and evidence are scoped for commits. Unrelated untracked art, generated test scenes and the two MCP logs are excluded. Fetch/rebase and push verification are recorded by the final task report.

Raw NUnit XML stack traces retain their emitted trailing spaces so their hashes and failure receipts remain exact. Source, metadata and authored Markdown pass the scoped whitespace check.
