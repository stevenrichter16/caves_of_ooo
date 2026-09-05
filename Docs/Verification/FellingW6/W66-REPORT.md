# W6.6 — Stillleaf, the Sealed Library

Completed2026-09-05. Full EditMode suite **7614/7614 GREEN**, zero C# errors,
19:57:28–19:58:35 UTC. Baseline7547→7614 (+67), including36 dedicated
adversarial cases. All production changes were preceded by failing assertions;
raw failed and successful runs remain in this directory.

## Implemented

- Stillleaf is the coined slope mouth at(2,4), using the existing discovered
  world-cell map rule. SealedLibrary is append-only enum value3; explicit
  profile/name mapping, mouth and floor ship together. Unnamed hashes remain
  three-way, preserving existing worlds.
- Z2 has one closed vault, three material wall families,21 original sprites,
  four variants per repeated family, one keyed door and fixed archive shelves.
  Existing stairs retain identity/coordinates; exterior approaches are carved
  around the enclosure. Reserved interior/architecture excludes ambient content.
- Floor encounters and containers use actual Tier3 content. Surface/descent
  retain their existing routing; Z3 resumes ordinary underground generation.
- Closed archive barriers stop normal/forced movement, vaulting and tumbling.
  Closure derives from the existing lock, including relocking; the test-only
  key opens it normally. No key or readable-record system ships in content.
- Automatic lateral, stair and follower arrivals exclude tagged interior
  paving. An exhausted vertical search fails before removing the source actor.
  Walking and save restoration inside the room remain legal.
- Successful map ascent refreshes cached discovery appearance. Failed ascent
  does not reveal a site. Existing map terrain identities and occupants survive.

## Review and fixes

**Taxonomy:** actor membership, missing-content atomicity, stair geometry,
refused relocation, instance isolation, save closure, no random interior
population, protected flora/creatures and fixed-object manipulation checked.
No new destruction/hauling bypass found. Ordinary wall vaulting and ordinary
breakable scenery remain supported controls.

**Canon/content:** slope placement, three named materials, locked architecture,
Tier3, no key/quest/ending and hidden discovery checked against the W6 plan,
world design, Geography/Recension and canon authority hierarchy. CoO-original;
no Qud mechanical-parity claim.

🟡 Resolved: profile/name disagreement gave renamed authored sites sima
daylight and water passages. Three REDs plus a real-sima control precede one
ForSite resolver shared by routing, lighting and both passage gates.

🟡 Resolved: both movement facades ignored a refused Zone.MoveEntity result
and emitted false arrival notifications. Two REDs now pin refusal without
AfterMove and the ordinary-movement positive control. A third RED pins a
relocked door with stale Physics.Solid, including a subsequent real key bump.

🧪 Future constraint: version7 saves omit Profile and rederive canonical names.
There is no shipped POI rename/profile writer outside authoring/rehydration.
Runtime rename controls do not establish renamed-site persistence. A future
story writer requires the recorded save-format work; no schema workaround was
introduced here. The initial review classification as a current gameplay bug
was retracted after verifying all production writers.

## Native evidence and limits

Run **8d5a2efce37c462ba817df7116b2adc5**, launcher exit0, zero C# errors,
**12/12 PASS**,75.000941708seconds,71568frames,611 actual moves and6110ticks.
Eight deterministic system controls precede actual keyboard keyless refusal,
matching-key unlock, door entry and repeated open-door walking.

Raw data: W66-live-profile.json, W66-live-frames.csv.gz and
W66-active-metrics.json. The archive-barrier marker measures its shared
displacement query; Input/renderer capture shows the surrounding real workload.
Whole-editor GC is not attributed to this feature. Sparse whole-frame p99
can be zero, so the separate active-sample statistics are retained.

**Can verify:** object counts, routing, positions, lock state, actual native
input, movement refusal/success, save state, CPU samples and sprite import
properties. All21 sprites pass16×16, binary alpha, outline, metadata-template
and global GUID audits.

**Cannot verify:** scene composition/readability or visual feel from headless
execution. The contact sheet is asset evidence, not a live look-pass. Known
destroyed-camera renderer teardown debt remains tracked for the later audit.

## Deliberate scope

Geometry and indestructibility stage the future quest access; canon does not
declare these materials universally invulnerable. Material identity grants no
room-wide anti-fungal/Urqu aura. Closed barriers block ordinary gas diffusion;
direct grenade/on-hit gas deposition and Seeping gas retain existing rules.
Discovery uses surveyed-cell visitation on returning to the world map, not a
new precise mouth-proximity detector. Cached player-modified floors survive.

Files span the new builder/barrier, sinkhole identity/router, map discovery,
cell/movement/travel consumers, six surgically added blueprints,21 sprites and
their metadata, art generator, scenario/native launcher, four test classes and
living docs. Only incremental hunks enter the preexisting MovementSystem and
EnvironmentSpriteRenderer changes.
