# Independent HUD performance review

The HUD retains its redraw limits. The first after run contained a large allocation/hitch anomaly; one unchanged-source reproduction did not reproduce it. Keep both receipts. This review does not establish a performance improvement, a physical Steam Deck frame rate, or the cause of the first anomaly.

## Matched evidence

| Receipt | Baseline | First after | Unchanged after reproduction |
| --- | --- | --- | --- |
| Run ID | `c698fb395ce546248e5348bc32ac35b9` | `00059d01db8144d586c8d6259fd552ab` | `53a296fe226c41bd9fafe3c5e3531eac` |
| Native checks |11/11|11/11|11/11|
| Raw observed frames |6,233|5,434|5,416|
| Hotbar / sidebar repaints |104 /70|104 /70|104 /70|
| HUD repaints in119+120 idle frames |0|0|0|
| Whole-frame allocation samples >1MB |0|152|0|
| Maximum whole-frame allocation sample |54,113bytes|343,494,172bytes|59,835bytes|
| GC collection-count deltas (each generation) |1|6|0|
| Message-phase allocation p95 |26,059bytes|12,732,664bytes|27,763bytes|
| Message-phase observed span |5.664s|8.886s|5.762s|

All three runs completed with no reported failures or unexpected errors. The comparator found no settings or observed-input mismatch in either baseline comparison, and a separate first-after/reproduction check also matched. The unchanged observer source SHA256 is `8158024d0e41a899da470830dec753f2e1e0d3e4c0b8336fa43ff9684bea02e4`; independently checked current source agrees with the coordinator's retained original import receipt.

The pair uses1920×1080, world target1461×918, native presentation, scale1, normal detail, shadows, FullFX, noVSync/unlimited cap, the same camera rectangles and audio settings. The ordinary original six abilities, initial log count7 and free tick10 match. Focus remains true; explicit snapshot/observer frame IDs match; actual slot and scroll sequences agree. This is editor evidence from the same recipe, not a Deck hardware measurement.

Raw evidence remains under [Profile](Profile/). The first comparison is preserved at [profile-comparison.json](profile-comparison.json); the baseline/reproduction comparison is [profile-reproduction-comparison.json](profile-reproduction-comparison.json). Both JSON files retain raw-distribution summaries, source-file hashes, matched conditions and limitations. No samples from the anomalous run were discarded or overwritten.

## What changed in ordinary measured cost

The reproduction's message-phase inner sidebar marker median is0.006750ms versus0.001583ms before; its p95 is0.011292ms versus0.002459ms. The outer zone-sidebar median is0.050292ms versus0.043167ms. The initial idle inner sidebar median is0.003167ms versus0.001333ms. These are small positive CPU costs consistent with validating exact retained log entries, not a CPU-speed improvement. The medians for whole-frame allocation are88bytes higher in like phases (for example17,773→17,861bytes); this is neither zero allocation nor scoped HUD allocation proof.

The104 hotbar and70 sidebar repaints correspond to the same actual24 slot cycles,40 empty-slot messages, existing log-scroll and Look interactions. There is no added idle repaint. Global tilemap-clear counts differ and are explicitly not HUD-only counters. New full-history reading is covered by separate functional native gates; this older-API observer does not open that reader.

## Allocation anomaly and bounded investigation

The first after run's152 samples above1MB are confined to a5.30s interval, frames1941–2583 (realtime11.1407–16.4426), within its messages phase;130 samples have zero HUD repaint counters. The same message sequence later returns below59KB/frame, and every other phase has no >1MB sample. Fixed two-row boundary trimming retains the anomaly. The largest outer sidebar marker is223.693ms while the inner sidebar marker is0.00625ms; this is compatible with an interruption during state building, but recorders can lag explicit counters and it is not an allocation call stack.

Source review found bounded30-entry log reads/copies,10-slot hotbar state, fixed cached glyph atlases, and no full-history reader opening in this route. The observer allocates fixed12000-row and five value/missing arrays before sampling, materializes one small diagnostic snapshot per frame, and serializes/captures screenshots after sampling. No new source path explaining the hundreds-of-megabytes samples was found. The MCP log contains no Unity tool calls during the first after sampling interval20:51:48.945–20:52:07.284Z. Editor.log shows the expected empty-slot messages without a corresponding import/error/warning. These observations do not prove that editor background work, diagnostics, residual test-runner work or an intermittent game path caused the spike.

The coordinator repeated the exact observer once without a production change. That reproduction has no >1MB samples or GC collections and normal message-phase allocation/elapsed span. This establishes non-reproduction, not proof that an intermittent problem cannot recur.

## Delivery decision and limits

No reproducible material HUD performance regression was established; retain the anomaly as an unresolved diagnostic observation and proceed with delivery. Stop this investigation after the source review and one unchanged reproduction. A new hitch in normal standalone/Deck play, or recurrence in a later diagnostic run, should reopen targeted allocation-call-stack work around state building, rendering, logging and the observer. Do not add speculative caches or rewrite the HUD based on unattributed whole-frame samples.

Markers are nested/inclusive and must not be summed. Recorder LastValue may describe an earlier completed frame; matching explicit frame IDs does not identify that recorder sample. Time-based input waits yield different frame counts, so raw sums are not performance comparisons. Whole-frame allocation includes diagnostic/editor work. Zero GC collections in the reproduction does not mean zero allocations. The native pixels and actual controller gates provide separate functional/readability evidence; subjective readability, standalone GPU cost, long-session stability and physical Steam Deck performance remain outside this measurement.
