# Reference glade independent world review

Status:33 published independent tests plus24 root core and65 existing WorldMap tests,122/122 GREEN after three focused RED cycles,
26 September2026. No Unity calls, no production edits by this reviewer. Tests and scoped hardening are now published after the root native window.

## Scope and verified surfaces

Read ReferenceGladePlan, ReferenceGladeBuilder, the actual OverworldZoneManager
pipeline/authority hooks, WorldGenerator authored POI exclusion, Zone placement,
EntityFactory callbacks, HarvestablePart, DestructionSystem and the whole-world
save graph. Review concerns native ownership and persistent interactions; it does
not judge model fidelity, presentation performance or ordinary-stat combat pacing.

Positive controls establish deterministic dressing, three normal Marlback actors,
finite initial chest items, actual harvest/destruction and full four-direction
travel. Independent tests cover six seeds including integer extremes; every chest
and harvestable has a reachable adjacent cell and each edge is reachable. Read-only
plan exposure refuses mutation. BitLocker and invulnerability are absent.

## Findings

1. **Content failure escapes the generation contract.** Removing Grass.Render or
   Chest.Container causes a NullReferenceException during staging. An ObjectCreated
   callback that throws also escapes. The builder stages before publishing, so the
   zone is empty, but callers receive an exception rather than a diagnostic false
   and normal pipeline refusal/retry. These are three semantic RED cases.
   Recommended minimal fix: validate required realized native parts and catch
   factory/initialization exceptions around staging, preserving empty input.
2. **Late placement rollback passes.** Invalid Wall/Chest footprints fail after
   prior owners have been staged/published. All published owners are removed and
   existing generation reservations survive. Eight missing-blueprint checks also
   refuse before world mutation. No extra production change requested.
3. **Native depletion and authority survive saves.** Actual emptied Chest content,
   harvested GlowQuartzVein and destroyed lit Wall remain depleted/absent after a
   whole GameSessionState save/load; GetZone never regenerates the cached graph.
   A completely emptied cached zone also stays empty. Changed biome and runtime
   village POI authority remain disabled after load. No extra change requested.

## Cold-eye questions

Q1: creation writes native owners once; destruction/harvest remove those same
owners and renderer access does not recreate them. Zone cache restore retains that
asymmetry deliberately. Builder refusal removes only its own staged owners.
Q2: the special address alone is sufficient only for standalone test graphs;
managed/loaded zones consult their attached actual WorldMap. Existing map mutations
and POIs win. Rendering acceptance is separate and remains with root/render agent.
Q3: missing/occupied/invalid-footprint controls accompany successful placement;
real depletion/save probes accompany initial-content checks; six seeds accompany
one authored layout. Faulty required parts need the fix above.
Q4: the plan's transactional/refusal claim currently exceeds implementation on
throwing or structurally malformed factory output. No claim of visual match,
performance or balanced combat follows from54 logic tests.

Evidence: Verification/DensityCompletion/ReferenceGlade/independent-world-red.xml.gz
and independent-world-red.log.gz. The actual initial result is54 total,51 passed,
3 failed; no test-only compile corrections were needed for this fixture.


## Repairs and regression follow-through

After the independent RED, root requested this reviewer own the scoped repair.
`ReferenceGladeBuilder.SupportsContent` performs a read-only required blueprint/part
preflight. Direct builder calls refuse unsupported content without zone mutation;
manager pipeline selection falls back to existing Spread generation for incomplete
content packs. Realized staged owners are also validated, and initialization
exceptions are caught before anything is published. The generated full content
pack still selects the glade. Eight existing WorldMap travel tests had failed
because their deliberately minimal factory lacked this optional scene's content;
the fallback repairs those actual regressions without weakening their assertions.

The expanded RED had122 cases/13 failures: initial3 staging defects,2 new partial-pack
checks, and8 existing transitions. The first repair passed122/122. A follow-up
counter-check then found2 more failures: the fallback zone still acquired glade
presentation authority solely from its address/map. Attached authority now includes
the checked content capability. Both partial-pack cases now reject that art profile;
full-pack glade, changed map and save controls remain green. Final private result is
**122/122**, comprising33 independent+24 core+65 existing WorldMap. Published-source
rerun follows; root owns native rerun. Evidence includes modpack-regression-red,
fallback-authority-red and world-independent-and-regression-green XML archives.

Two private-runner configuration errors (duplicate patched manager source, then a
missing CooRun namespace on its deterministic hash adapter) were corrected before
this semantic run. No runner-specific hash code was published to runtime source.

Published-source follow-up: **248/248 GREEN**, combining122 glade/world regression
and126 C7 core/adversarial/integration-contract cases. Root owns native execution.
