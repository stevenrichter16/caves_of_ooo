# M1 historical reports and notes

Status: all 44 native baseline cases executed before production; private minimum implementation prepared. Initial and third-family logic are native GREEN; real keyboard/UI acceptance remains pending. No shared production publication by this agent.

## Corrections before implementation

- RegionalGuidance currently projects only village POIs. Preserve its four offers and 17 destination-keyed notes; append separate rare reports at Scribe RegionOverview / Innkeeper Rumors.
- Selection does not prove placement. Refused/committed/dead remote graphs must produce identical unconfirmed reports. No remote cache inspection or GetZone call belongs here.
- Entity.Properties are already serialized by SaveGraphSerializer. Two typed, versioned, family-keyed records need no new save binary or world metadata.
- String IDs alone cannot reject a same-ID replacement between offering and selecting. A bounded transient current-offer snapshot will capture exact owner/graph/part references and positions; it is not a saved discovery ledger.
- Existing ConversationActions required handlers already emit success/refusal diagnostics. Reuse those and give bounded stable rejection reasons.
- Informant prose should read bounded current Render.DisplayName directly, avoiding event-driven formatting callbacks during a supposedly read-only projection.

## Invariants and proposed files

New SpreadDiscoveryReports.cs and SpreadDiscoveryNotes.cs; narrow ConversationManager/ConversationActions/QuestLogUI hooks. Two initial families only; no fake wayhouse dependency. New source/test metas. No pipeline, SaveSystem, Objects, source selection, art, or RNG changes.

Opening conversation/report/journal is read-only. Explicit remember validates the exact current offer, then writes one bounded family record atomically. Notes retain original informant/origin/destination/report through travel, informant death, map changes and save/load. Corrupt records cannot create extra families or break the journal. Repeated selection replaces one record. Existing village/situation notes retain their own schemas/caps.

## Test-first and acceptance

Primary native fixtures will test current real conversation choice routing, historical remote-state equivalence, no destination generation, explicit note action, existing note compatibility, entity save/load, journal aggregation, stale/context replacements and malformed inputs. Dedicated adversarial additions follow actual RED/GREEN. Root owns Unity execution and native input/visual acceptance. This log will retain exact results and limitations; no compiler result is a native gameplay claim.

## Reader correction before implementation

Actual DialogueUI renders choice labels on one row but wraps ConversationManager.CurrentText. Full historical reports must append to the current eligible node text; action labels stay under52 characters. The source node remains untouched. A two-case native text supplement pairs both real role/node combinations with unchanged other-node text. At this pre-implementation checkpoint the primary/adversarial fixtures reference-compiled with 0 errors and independent fixture review was clear; no production code existed. The executed baseline and candidate status are recorded below.

## Executed RED and private candidate

- Native job `f90d1db324904f3a9036c144c86097d3`: Reports13 = 6 missing-feature failures / 7 exclusion controls passed; Adversarial29 = 29 missing-report failures. Raw `../Tests/m1-m2-m4-wayhouse-red.xml`.
- Native job `08e48d12355841e188f3555a6fa1b08f`: Text2 = 2 missing full-report failures. Raw `../Tests/m1-text-m4-composition-red.xml`.
- The private seven-path candidate implements the two fixed typed note families, read-only current-map projection, exact current offer snapshot, full wrapped conversation text, concise explicit remember actions, and existing journal aggregation. No new saved entity fields, remote graph reads, gameplay RNG or source changes.
- Native success remains unclaimed. Root owns publication and native GREEN. Candidate manifest: `/tmp/coo-spread-discovery-m1/production-manifest.json`.

Reference compilation: runtime and full current test assembly both 0 errors. Compiler-only inputs include the pending peer receipt/composition copies needed by the concurrently written WayhouseBuilder; these are not part of this seven-path M1 publication. Root native execution remains authoritative.

## Third-family test-first extension

Root authorized 10 native-compatible wayhouse report cases after the two-family candidate. They use actual `manager.Wayhouse.ZoneID` and map admission only, cover missing/empty/malformed old metadata, exact plan/map staleness, remote-state equivalence, independent older rare-plan state, preserved two-family wire records, three-family cap and entity save restoration. Existing 44 cases explicitly disable Wayhouse in their fixture so their original two-family assertions retain their meaning. Test-only three-path manifest: `/tmp/coo-spread-discovery-m1/wayhouse/test-only-manifest.json`; reference compilation 0 errors. Third-family production is unwritten pending actual native RED.

Independent source review (standalone verifier): no concrete blocker in the replaced bounded offer snapshot, exact current player/speaker/context/map/plan references, revalidation before historical writes, bounded canonical wire, or absence of remote generation/gameplay RNG. This is source review, not native GREEN or UI acceptance. Initial seven-path manifest and next three-path test manifest were rehashed unchanged before handoff.

## Initial native GREEN and third-family candidate

Actual native job `8bdb035064c6488c8b348b4a3d7c6028`, raw `../Tests/m1-green-wayhouse-report-packet-red.xml`: initial M1 **44/44 GREEN**; third-family **10 = 7 intended feature RED / 3 disabled-metadata controls passed**. The earlier stale-assembly job `2c1e` is excluded, not evidence for this implementation.

After that RED, the private two-source extension adds `turnbank-wayhouse` independently of older rare-plan initialization, snapshots the exact Wayhouse plan, and delegates new-offer admission to its current map-only `Selects`. Three fixed historical records are the total bound. First two serialized formats and formatting remain unchanged. Runtime and full test reference compiles have 0 errors; independent bounded peer review is clear. Exact delta `/tmp/coo-spread-discovery-m1/wayhouse/production/production-manifest.json`. Root native54 and real informant/keyboard/journal/save acceptance remain pending.

## Final M1 logic GREEN

Actual native job `675459b3bf4043c9985f8ffcaf21db4d`, raw `../Tests/m1-wayhouse-green-key-route-red.xml`: **all 54 M1 cases passed**. The overall batch was 83/84 because of a separate wayhouse key-route case, not an M1 failure. Reports13 + adversarial29 + text2 + wayhouse10 are the exact M1 set. The renderer agent owns the real generated-informant/keyboard/journal/save audit; its pending state is not covered by this logic result. No M1 source edits remain.

## Native closeout

The later main19 and ordinary14 runs exercised actual informant reports, all three explicit remember actions, Q/Tab reading and exact saved notes. Root reviewed their frames; `../Native/README.md` records the distinct ordinary/staged routes and limitations. Earlier pending statements above are historical checkpoints. Final integration is recorded separately in `../Integration/`.
