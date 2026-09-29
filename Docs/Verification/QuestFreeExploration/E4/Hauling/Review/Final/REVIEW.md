# F9 final evidence cold review

29 September 2026. Read-only evidence/source audit; no new tests, Unity calls or shared/production edits.

**Finding:** the final full suite is not green. It completed21,862 cases:21,858 passed,4 timing failures,0 skips,5,789.7111409s. The historical full comparison correctly reports4 newly failing cases: disabled diagnostics cost and the142-surface coverage cases for64/1/1729. All128 added test names passed;9 removed names are accounted-for renames, giving119 net cases. The private and Integration copies of both comparison JSONs are byte-identical. All41 restored candidate asset hashes match the overlay receipt.

The separate same-condition four-test comparison is narrower: each version passes2/fails2, with no newly failing names in that subset. Seed64 coverage took224.4558623s candidate versus229.7377111s baseline; both exceeded180s. The disabled diagnostics benchmark also fails in both. This supports reproduction on published source under the later conditions. It does not erase the four full-suite failures, test seeds1/1729 on the paired baseline, establish a platform cause, or establish candidate performance improvement.

## Consequential census discrepancy

The two paired gzip hashes match the supplied receipts. Both complete142 surfaces/1 lair floor and record zero geometry/style failures, but candidate has313232 owners and baseline313257. The entire+25 baseline delta is one optional Wayhouse at`Overworld.11.7.0`:

| Blueprint | Candidate | Baseline |
|---|---:|---:|
|StoneWall|0|22|
|Crate|0|1|
|Signpost|0|1|
|VillageDoor|0|1|
|SpreadWayhouseState|0|1|
|MarlbackScrabbler|2|1|

All other **zone/blueprint counts** match. This is not an owner identity, equipment, position, random-state or graph equivalence comparison; report IDs and art variants differ as well.

This absence is not newly introduced evidence: the zero-geometry-failure seed64 coverage artifact`64-all-current-Spread-ecac1e05238a4dc7b99be9e69e204d7e.json.gz`, read directly from commit`d0fb582ca14131d3828384a701e46d6bdeb0e37f`, also has no Wayhouse and exactly the candidate's11.7 blueprint multiset. Its whole-world total313228 differs because it is older evidence; do not present it as the same current graph. All four retained seed64 all-world reports present in that baseline commit lack this Wayhouse. The current full run passes the dedicated`SpreadWayhouseTests.GeneratedDestinationHasOneFiniteRewardAndDistinctKeyWithBothEntrances` cases for1/64/1729, including64 at0.4508297s.

### What source inspection establishes

- `Assets/Tests/EditMode/Presentation/Rendering/SpreadBiomeCoverageTests.cs:45` uses the legacy detached manager. `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:54–61,70–83` keeps F9 helper/source capture outside that legacy pipeline.
- `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadWayhouseBuilder.cs` and `SpreadWayhousePlan.cs` are byte-identical to baseline. Selection is independent of the exploration version. The builder can legitimately refuse unavailable/changed sources at34–41, missing footprint at50, staging at90–91 or no safe layout at135. Its receipt checks at200–201 still use `MatchesOwnedState`.
- The only relevant changed generic receipt path is `SpreadWildernessSituationPlan.cs:57–59,115–119`: one added producer type branch and optional null-dictionary branch. With population/container sources and no dictionary, both retain the old semantics. The new ID-count dictionary from`CaptureFinalState` is not called by this Wayhouse builder. No consequential legacy-path change was found.

**Classification:** an observed pre-existing optional-realization/fixture-context discrepancy, with mechanism unresolved; not a demonstrated F9 source regression and not proven randomness. The visual census records neither admission/refusal reason nor original positions, owner graphs or RNG states, so these files cannot establish why the paired baseline accepted it. Do not claim identical generated graphs or diagnose a receipt bug from totals alone.

If exact causality is required, the smallest next check is one generation-only reproduction of the census prefix through11.7 using its actual factory/global RNG initialization, retaining`SpreadWayhouseRefused`/staged/final diagnostics and source facts on both versions. It need not render142 surfaces, alter thresholds or change production. Root decides whether this pre-existing optional-site bound warrants that separate work.

## Publication wording limits

The initial audit found the living document's opening and final paragraph still describing the full run as in progress; root subsequently reports those publication sections corrected. Retain a precise full-suite failure result and then the narrower paired-baseline reproduction. Keep the earlier45-sample hauling optimization result separate: those fixed source samples preserved their counts, unlike this optional Wayhouse comparison. No claim of environmental causality, all-green full regression, identical census populations or ordinary discovery is justified.

`PUBLISH-WORDING.md` supplies concise draft text. `census-delta-and-inputs.json` retains parsed differences, historical hash proof, inspected source/evidence hashes and restored41-file verification. Raw input data remains authoritative.
