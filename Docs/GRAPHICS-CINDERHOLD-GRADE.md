# Cinderhold native material grade

2026-11-15: bounded Editor acceptance for Cinderhold6.6, authored Grovelands/Village/Cinderhold/PruningPost. Native sandstone, packed roads and interior stone now use white filter, contrast8, saturation0 and vignette0. Canonical Grovelands remains(.88,.92,.85),8,6,0 elsewhere and in legacy presentation. No texture, material selector, geometry, light, shadow strength, entity, encounter or save-schema change.

`BiomeColorPatcher` gates the native grade on exact surface address, Grovelands, active native rendering, displayed-zone reference, current settlement graph/identity and no saved lair. Admission loss and same-biome travel restore all canonical values. Existing shared volume profiles remain unchanged. Coverage/population/shadows are separate; full-town composition and V0–V7 remain open.

## Verification

Evidence: `Docs/Verification/GraphicsExecution/CinderholdGrade`.

- Native RED7d8d289896f44358aa4d7b94804f7c63:14 genuine new failures/81passes. GREEN30632b3cdc7b40138645b59c6cf89c9c:103/103 travel-grade and Cinderhold-road cases,0skips/inconclusives. Native mode, sprites disabled, lost/changed POI/name/type/profile, cache replacement, displayed saved graph, saved lair, foreign biome, depth/aliases and shared profile controls. Alias tests establish refusal of the neutral candidate, not exact canonical values for every malformed address. Existing unchanged shadow semantics reuse prior road/shadow acceptance; no redundant shadow suite.
- Independent source review `sumphold_grade_source_review`:0P1/P2. Independent actual visual review `sumphold_baseline_review`:0visibleP1/P2, clearer stone/road/sandstone distinction with quiet floors, retained edges/contact shade and lighting/fog boundaries.
- Fresh actual GameBootstrap Continue from CinderholdRoad/AfterCheckpoint1880at33,11HP40Dusk. Ten Left/four Up reached LedgerOffice23,7HP40turn2020; same route replayed after change. Both center and interior pairs retain exact hotbars; center entire sidebar exact. World differences638512/634040 of639200 reflect the regional grade, not a geometry change. Office logs differ22003pixels.
- Actual full Field-notes reader for SandstoneWall21,8 retains original description and HP30/30. Existing checkpoint logs already contained historical wall strikes; this slice added no strikes. No separate portrait is shown by this reader.
- Unprotected ordinary6.6→5.6→6.6 verified same-biome canonical(.88,.92,.85)/8/6 outside and white8/0 inside. Return plus oneLeft, F5turn2110HP40at38,11; fresh Continue exact.801tracked SandstoneWall/RoadStone/StoneFloor IDs, positions and available HP values match before travel, before save and reload. World/hotbar reload difference0; sidebar logs differ18505pixels. Inherited wet status retained; no F12 protection used for Cinderhold.
- After-only Editor profile65.881s,2321×1283,AppleM5: rendererp95 .069417ms/max73.259333ms,Mainp95 1.809083ms/max767.321792ms. Includes UI/captures/probes. No causal performance or packaged/hardware claim; R11 remains open.

The first post-test launcher session had an unbound GameBootstrap manager and ignored Continue. It was archived, finished and replaced once; `excluded-unbound-startup.png` is explicitly excluded. The fresh second session loaded correctly. No blind modal acceptance or normal-save access.

## Seed and preservation

Independent binary decoding of format8 confirms both `GameSession.WorldSeed` and `ZoneManager.WorldSeed` are **704860946** for retained CinderholdRoad/AfterCheckpoint and new OfficeCheckpoint, GameID9ba2c3d179134acc9f898addede41f94. The matrix's old generic observed_seed65 is not evidence for these checkpoints. New grade evidence uses704860946; earlier unrelated shadow evidence is not silently relabeled.

80preexisting dirty-file hashes and normal gameplay saves/preferences preserved; TestResults.xml, Unity session-end telemetry and four Unity/test bookkeeping preference keys are the only differences. Owned audit roots archived before Finish; final Editor idle/rootnull/clean. Prior builds retained. Current source468a88Mac/Linux excludes reeds63012cd36 and this grade; batch both next, then continue different-area environmental work. Deck-specific work stays deferred. Hourly automation ACTIVE; actual report04:18:14Z.
