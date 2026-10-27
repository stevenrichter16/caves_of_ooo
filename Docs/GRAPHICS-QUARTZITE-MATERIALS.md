# Quartzite depths 9–10 — bounded preflight

Source preparation only, 2026-10-27. Root verified Slate source `702cb9d8096e6dd8eebfd93cdeb08b22d5aece27` remotely and is packaging it. **Depths 9 and 10 remain unobserved.** No new art, test, adapter or area acceptance is claimed. This is the next scoped plan; root owns Unity and gameplay.

## Exact contracts and false premises

| Avoid assuming | Verified source contract |
|---|---|
| Slate's 38HP / hardness3 applies | `Objects.json:8643`: exact `QuartziteWall`, inherits `Wall`, glyph `#`, color `&C`, background `^c`, HP/MaxHP55, hardness5 and `Rubble` wreckage. Surviving1–55HP retains the same body. Do not copy Slate's14-strike gameplay expectation. |
| Quartzite requires a new physics material | `Wall:3280` supplies layer0, solid Physics and Solid/Wall tags, Stone/Mineral, combustibility0 and Thermal flame1200/heat capacity3. Preserve these mechanics. |
| Floor has material or structural parts | `QuartziteFloor:8913` inherits `Terrain:3438`: layer0, Terrain, glyph `.`, color `&C`, variants `...,.'` followed by a backtick. No authored Material/Thermal/Destructible; background is empty. |
| Quartzite covers all deeper levels | `SolidEarthBuilder.cs:42–49`: exact9–10, between Slate7–8 and Obsidian11+. Pair positive9/10 checks with refused8/11. |
| Natural layout is unchanged from Slate | `StrataBuilder.cs:34–54`: depth9+ uses CA seed65, three passes and noise0.38, versus Slate60/3/0.42. Open-cell rolls remain85% floor,10% Rubble,5% empty (`:99–117`); these are probabilities, not a census. |
| Current depth proves original generation | `UndergroundLayoutPlan.cs:23–59`: ordinary Spread/Sodden/Beating columns at depth3+ may use the25% roomed recipe. That selection is not serialized render provenance. Exact real owners may occur in either layout. |
| Danger matches the observed depth8 return | `PopulationTable.cs:1102–1158`: depths9/10 use tier4, adding StoneGolem and ObsidianBrute possibilities. Neither presence nor safety has been observed. |
| The16 old images are wall adjacency masks | `EnvironmentSpriteRenderer.cs:527–568,797–814`: `quartzite_m00`…`m15` are16×16 slices of a64×64 macro floor. Preserve them; do not stretch them into16×24 cells or treat their suffixes as wall masks. |

All blueprint references above are in `Assets/Resources/Content/Blueprints/Objects.json`; generation sources are under `Assets/Scripts/Gameplay/World/Generation`, population under `Assets/Scripts/Data/Tables`.

Read-only decoding of the16 old Quartzite PNGs found4096 opaque pixels: RGB104/116/120 in3276,114/126/130 in608 and92/104/110 in212. This is a **candidate palette**, not a finalized floor/backdrop constant. Actual normal Cave grading must establish the quiet floor versus bright granular solid-wall contrast. Straight opaque silhouettes remain the default; the parked Shale study is not a template or permission for another parity detour.

## Short earned route for root to verify normally

Use unchanged `Docs/Verification/Live16x24/Fixtures/slate-materials-wreckage115`: zone `Overworld.16.13.8`, turn13050, HP33; the final play receipt places the player at44,10.

- `Quick.json`: `56247f43b8081569f76986c4d0aa5eb6e3e3400acff09a833966f0e08583f648`
- `Quick.sav.gz`: `39f806076ba6d06910a29148e88052263d632a5d3525c94575ce07c8e7db3e00`

Continue, then four Left retraces the used row to the Up stair at40,10. The disclosed depth8 census shows the Down stair at40,12. Inspect the current scene, take two Down if permitted, then `>` to9. This is a short candidate route, **not a safety guarantee**: actors and hazards can react. Preserve actual damage and refusals; no healing, teleport, reveal or save edits. No hidden saved owners were used to infer deeper positions.

At9, capture the actual arrival and read the Up stair, QuartziteFloor and QuartziteWall. Verify ordinary `<` back to8 before longer exploration. Return9 and find its real visible Down stair through normal play. **Depth9's Down and depth10's arrival positions are unknown.** Capture10 and a9↔10 return when reachable. `ZoneTransitionSystem.cs:371–428` refuses a physical departure with no return stair; do not repair or inject an endpoint to complete the test.

## Finite policy and gates

Candidate scope: two new material identities plus unchanged Rubble, Stalagmite, StairsDown and StairsUp controls. A six-frame96×24 atlas (floor0–3, wall4–5) is sufficient for first review; frame count is not accepted-area count. Keep Rubble/Stalagmite centered16PPU from their16×16 sources, opening-anchors frame4 Down and opening-cellar frame0 Up. Shared storage, furniture and portable actors retain their own contracts.

Use the plain Slate/Limestone policy: default borrowed-selector constructor; injected five Texture2D groups plus selector. Change only Quartzite semantics, depth band, resource and the eventually approved floor base. Preserve canonical addresses, exact current `WorldLocationContext.For`/CachedZones reference, valid live map/POI arrays, null current POI, read-only saved-lair veto and reserved Sinkhole/Root/MultiCell/authored-column exclusions. No provenance framework or broad admission bypass.

Keep floor-owned `LocalGround` separate from owner-free `AmbientBackdrop`. Missing/rejected actual floor art must not silently become ambient backing. Hidden, foreign and custodied owners cannot affect disclosure. Preserve coatings, heat, charge, cultivation, authored backgrounds, selected foreground, actor-over-ground and authorized reader identity. Cache views once, isolate optional resource failures and dispose owned Sprite/Tile objects only. No new hot-path parsing, event dispatch, scene scans or RNG.

Meaningful RED pairs:9/10 versus8/11/aliases/POI/lair/stale graph;55/5 structure with real damage to54 and destruction to Rubble; exact floor/render/owned parts and reskin/custody/footprint counters; visibility/memory; empty/Rubble backing versus real floor; submitted full/dirty paint with actor cover/removal, hazards and missing resources; reader parity; ordinary8↔9↔10 and missing-return refusal. Reuse the bounded fixture. Write and run the failing behavioral tests before production implementation; the genuine RED receipt is required.

Production order remains actual9/10 baseline/census → imagegen study → part critique → original editable native source with protected-input checks → RED → minimal adapter/GREEN/related tests → normal-zoom state/readers/travel/fresh Continue/profile → independent review, source push and fresh packages. Reject altered concept fog/UI/placement. Archive integrity and Editor profiles do not prove Linux/Deck execution or speedup.

## Keep the broader scope open

Obsidian11+, special/authored/lair/sinkhole columns and generic Wall/VineWall/Pillar/BrokenColumn remain separate. The observed Slate IceSheet, BrinePool, SteamVent, TarSeep, CharredHusk and ChoirIronVein are separate families;9/10 needs its own disclosed census. Remaining surface actors, crops, settlements, services, knowledge/equipment and other world families stay queued. All55 inventory and five Stump-area polish statuses remain unchanged. Repeated slabs, olive Rubble, mixed glyph hazards and whole-chunk composition are polish debt, not erased by six cells.
