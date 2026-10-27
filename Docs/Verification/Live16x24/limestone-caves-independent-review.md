# Limestone source/runtime review before actual gameplay

2026-10-27. No significant runtime or source-art defect found in this bounded review. Suitable for the actual integration trial, not whole-area acceptance.

Reviewer authored the36-case test fixture, so its API/premise audit is self-review. The runtime and original artwork were authored by separate agents; those reviews are independent. This reviewer made no Unity/Git call, ran no native test or builder, and changed no production file.

## Verified scope and native RED

The phase plan matches actual factory semantics: LimestoneFloor has '.', '&w', the authored glyph variants and no Material/Thermal/Destructible; LimestoneWall has '#', '&w', '^K', owned Stone/Mineral/Thermal, max35/hardness3/Rubble. Damaged surviving1..35 remains solid rock. GenericWall, VineWall, sandstone/shale and props are separate identities despite sharing an eligible column.

Read and independently parsed `limestone-caves-native-red.xml`:36 distinct failures, SHA185bc6d5a6e1cee3af748ad534c1453e688cfc1837a6b09aee1f64216306a7ca.35 failed at missing new adapter; the3→4 transition case failed at the upper depth3 stair's native-anchor-versus-glyph assertion. Both failure shapes reflect absent intended presentation. The zero-case runner attempt is not a native RED result. Installed fixture SHAe8963f3f8f8ebfe9849fe4c35c26ba123935daa8e8690255ea3f3ce8eeae5c83. No fixture correction or speculative test expansion was indicated by the source audit. Root, not this reviewer, executed native RED. No GREEN is claimed by this pre-gameplay receipt.

## Independent runtime review

Pinned `FlatPixelLimestoneCavesCatalog.cs` SHA03ea7981d2ba80b845fbf1d303856760634f44503d9fc869018f263f40463ee5 and `FlatPixelGreyboxCatalog.cs` SHA80064f17b828f23d1d4f99d696475302aeef6ffcef808564b7c917a04f99d210 match the reviewed files.

- Canonical ordinary depth3/4 addresses are prepared once. Current attached/cache-identical graph, map shape, POI and surface-keyed saved-lair authority are rechecked without generating a zone or initializing a ledger. Static reserved columns stay excluded. This is current eligibility, not saved generation provenance.
- Literal floor resolution scans legitimate visible physical owners, refuses mixed/unknown terrain and tile state, and keeps selected foreground independent. Floor glyph suppression requires the selected actual floor and successful native ground. Foreground/subject routes retain selected/current/visible placement, sole-owned part, custody, current single-cell footprint, essential material/status and structural-state constraints.
- Owner-free ambient backing remains a separate flag and RGB112,112,100. It never becomes LocalGround, a material owner, a subject portrait or a floor-suppression reason. Real floor failures, authored backgrounds and disclosed hazards/competing meanings veto it; hidden/foreign/custodied occupants do not alter the decision.
- Greybox uses separate output variables, so a refused limestone group cannot erase a prior native body. Depth2 and opening admission are unchanged. Existing light/overlay painter contracts are reused without new suppression flags.
- Five explicit optional texture groups remain independent; a missing limestone atlas loses floor and wall together, while borrowed debris/stairs can remain. No resource reload in explicit-null injection. Sprite/Tile views are cached; disposal is idempotent and does not destroy borrowed textures or selector backing.
- No per-cell Split/LINQ/new collection, census, event dispatch or gameplay mutation was introduced. This is source reasoning, not an allocation or performance measurement.

## Independent native art inspection

Opened `contact.png`, `contact-native.png` and actual before07depth4/08wall-reader images. The contact shows a restrained dark gray/olive floor and distinctly lighter planar wall mass. Internal chips do not outline tile edges or create bevels/extrusion. Whole source adjacency remains quiet; obvious alternating diagonal wall fissures can still repeat over broad masses, so actual normal-zoom Cave grading and connected formations must decide whether a further named correction is needed. Source quietness is not proof of sufficient area furnishing. The actual reader baseline confirms structural35/solid; remembered/unexplored dark extent is not evidence of bare terrain.

An independent decode-only check compared all2304 atlas pixels with named source cells, each placed1×/8× contact pixel with its atlas cell, all27 exact preserved inputs and9 metadata files under the documented trailing-whitespace-only normalization, and all26 frozen handoff file hashes. All passed. The96×24 atlas contains6 distinct opaque16×24 frames and exactly7colors; floor0–3 and wall4–5 match the runtime contract. Source and Unity PNG bytes agree at SHAa0d6a6bc7e8e3273125de077ca522835b565ec1c7bc88a1f885e2a0d77f0093d. Source cells SHAd65dcd56506bc00699be9d947f385df2d98585529f5c482edf3ed1eaa2f1702e; editable blend SHA6b4893bbcc628d56a20a8d9df3b5c0435dc767e8dbc6d9b7751328ff721a125b.

Read the builder, README, critique, geometry and reproducibility receipts. Original integer positions are explicit; references are retained but not read as raster inputs. The source author's reopened-blend receipt records2304unit quads/26named clusters with matching emission colors. This reviewer did not reopen Blender. Two fresh-build native/contact/source-cell byte identities are recorded; blend bytes differ and are not falsely claimed reproducible. PNG normalization removes only volatile timing text chunks. Ten meaningful source corruptions are recorded as rejected; these source tests do not replace native or actual gameplay checks.

## Remaining gates

Root's current native GREEN, actual depth3/depth4 world and reader comparisons, blocking/travel/persistence, profile and fresh package delivery remain separate gates. Generic walls, vine walls, pillars/columns, hazards, creatures, loot, special sites, shale/deeper bands and all-area composition/fullness/polish are not accepted by this six-cell foundation.


## Native verification appendix — 2026-10-27 18:28:55 UTC

Root's completed native runs were independently parsed and their file hashes verified:

- `limestone-caves-native-green.xml`: **36/36 Passed**,18:25:08–18:25:13UTC, SHA256`27cfe560ea44924ecd24eee6e76ce80ef2befe97cc1ce6ff17f6b6ff425642b3`.
- `limestone-caves-related-green.xml`: **395/395 Passed**,18:28:30–18:28:55UTC, SHA256`561708b770dedf2925246e18268a9c323b0509a2462e7b6b11a244069c410e26`.

The two sets contain **431 distinct full test names with zero overlap**, all Passed; no failures, skips or inconclusives. This reviewer read the native evidence and did not execute Unity. The earlier RED remains retained and is not added to the total.

The first related run remains at `limestone-caves-related-first.xml`, SHA256`a6066873114d194df809e9c9c630595fee2c657a21369a3014e7f06b3802f6ba`:394Passed/1Failed. Its sole failure was the old shallow fixture's global `IsAmbient(View()) == false` expectation at depth3. Direct shallow `TryResolveCellBackdrop` refusal still passed. The new limestone companion correctly supplies its separate backing there.

The narrow test correction keeps shallow depth3 refusal and all other surface/noncanonical/reserved/current-POI/saved-lair exclusions. It now separately asserts world AmbientBackdrop=true, LocalGround=false, Owner=null and exact limestone RGB112,112,100, unequal to shallow128,106,76. The existing test identity is preserved. Final shallow fixture SHA256`dfea43d7ce313abc0d8a68e9c30c5325d8bd4b4859f8027cb31211ff98a21663` was verified. This was a superseded global test expectation, not a relaxation of the shallow adapter or a production fix.

Both production hashes recorded above remain unchanged after this correction. Native validation is now complete for this bounded source checkpoint; actual limestone world/reader comparisons, travel/persistence, composition, profiling and package/platform acceptance remain separate pending gates. Earlier pending-GREEN statements describe the chronology before these receipts.


## First actual limestone comparison — chamber, damage and restart

Independently opened all six actual `limestone-caves-after-01` through `06` PNGs and compared matched chamber01 with `deeper-strata-before-07-depth4-saved.png`, and wall reader02 with `deeper-strata-before-08-wall-reader.png`. These are actual1280×800 Editor captures, not the source contacts. Root owns the controls and state observation; this reviewer did not operate Unity.

**Bounded visual judgment: the six-owner material integration is a clear improvement and has no newly identified blocking visual defect in these views.** The earlier dim glyph field becomes a continuous, readable floor, with lighter solid wall masses and small low rubble silhouettes. Short internal wall fractures remain planar; no extruded edge, invented masonry or raised rim appears. Under existing Cave grading the source gray/olive becomes warmer brown/olive in the world, while the ungraded reader portrait remains pale. The world floor/wall value difference is still visible at normal zoom. The matching reader keeps the actual name, Stone material and35/35structure alongside the native wall cell, without visible text clipping.

The field is not a finished composition. Broad walkable areas remain lightly furnished apart from repeated rubble; short repeated fissures are visible along connected wall rows. Separate uncovered glyph owners and remembered-presentation transitions remain conspicuous. These are explicit later coverage/polish debts, not a reason to invent props or count fog/occlusion as bare terrain. This entry/chamber view does not inspect the entire80×25chunk, all landmarks or every ordinary column.

Damage views03/04 retain the surviving solid wall rather than prematurely replacing it with walkable floor. The separately saved `limestone-caves-wall-history.json` records actual owner84455 at44,15 through35→34→28→21→18→9→4→3 structuralHP, then actual Rubble86926 with the cell nonblocking before entry. Root reports17 ordinary upward strikes. After entry, player2695 shares that cell, so its aggregate BlocksMovement becomes true because the player is present; this does not imply the rubble became solid. No damage mechanic change is claimed.

Views05/06 show the player at44,15,HP33 and sidebar contents `rubble, You`, with the destroyed-wall log and the new local visibility shape. The player covers the newly created rubble body, so those two screenshots alone do not prove its uncovered sprite or portrait; the saved owner history supplies the real replacement evidence. `Fixtures/limestone-caves-wreckage89/Quick.json` independently records depth4 `Overworld.16.13.4`,turn12270,HP33/40. Root reports archive/Finish followed by a fresh isolated Continue with the same player, rubble ID and position. Geometry/material presentation is visually consistent across the two images, but no byte/pixel identity is claimed: an independent RGB world-crop comparison `[0,0,940,680]` found15,042changed pixels out of639,200, maximum channel difference7, clustered around the player-lit area. This small lighting difference is not evidence of a changed terrain owner; its exact temporal cause was not established by this image review.

Read the saved profile receipt:108.836538seconds,22,805samples, ZoneRenderer.LateUpdate mean0.112269ms,p950.196ms,max4.653708ms. Main Thread maximum is1400.857042ms. Its stated window includes the chamber, reader, four attacks, idle time and MCP/CUA instrumentation; it does not cover the entire later damage/entry/restart journey. No matched timing baseline, sustained FPS, package or Deck performance claim follows from these measurements.

Actual uncovered rubble/stair readers, wider depth3/4 travel, final state evidence and package verification remain to follow. No source-pixel revision is recommended from these first six actual views alone, and no whole-area polish status is promoted.


## Completed bounded Editor journey and delivery helper review

Independently opened the additional actual captures07–17, completing review of all17 current Editor images. No significant new visual defect was identified that calls for a source-pixel revision before the bounded source checkpoint.

Reader07 closes the earlier rubble-portrait evidence gap: the actual replacement at44,15 is named rubble, classified Terrain and shown with the existing low stone-pile portrait. Reader10 shows the pointed solid stalagmite at40,22 against its actual floor; readers11/14 distinguish the rising up stair from the recessed down stair. Reader15 identifies actual LimestoneFloor at39,14 with its quiet native floor portrait. The text and native portraits fit their panels without obvious clipping in these captures. These are literal object identities; none establishes an ambient backdrop as an examinable terrain owner.

World views08/09/12/13/17 support root's ordinary route from the damaged depth4 chamber to its up stair40,12, to depth3, through the up stair39,12 to sandstone depth2, back to limestone depth3 and finally down again to depth4. Across the observed crossings, shared debris/stair scale remains consistent and the old sandstone palette and directional texture remain visibly distinct from the quieter limestone material. Current visible areas retain native presentation; dark remembered/occluded extents and remaining unrelated glyph owners are not recolored or counted as empty chunks. Broader depth3 views reinforce the existing limitation: large fields are still mostly rock and repeated rubble, with limited furnishing visible in these viewports. The six-owner integration improves material readability but does not finish whole-area composition, all landmarks or all cave actors/props.

This was not an uninterrupted safe movement test. After the floor reader, the real Scrabbler occupied the up-stair approach. Capture16 shows the player's missed attack and12damage, leaving21HP at39,13. Root's recorded successful Right then Up reached the down stair40,12 while a further15damage left6HP; capture17 shows the subsequent depth4 return at40,12,6HP. Root reports that batched inputs included misses and that actual state checks prevented crediting failed movement as successful travel. No AI, health, collision or combat alteration is inferred. The final `limestone-caves-roundtrip90` metadata independently records `Overworld.16.13.4`,turn12490,HP6/40. This later save is distinct from `limestone-caves-wreckage89`,depth4,turn12270,HP33/40, which remains the selected retained package-smoke starting fixture. Root reports both isolated Editor roots archived and Finish completed; this reviewer did not manipulate saves or inspect private normal preferences.

The actual component/world/reader, structural damage→wreckage, normal stair-travel and earlier fresh-Continue evidence now support a **bounded limestone integration checkpoint**. There is still no full-chunk, every-column, special-site, shale/deeper-band, package, Linux, physical Deck or sustained-performance acceptance.

### Independent scratch helper cold review

Reviewed the new adapters as source, without importing or executing either helper:

- `/tmp/coo-package-limestone-caves.py`, SHA256`a0fc5478e990ac4a06bed420feb8dcac5906dc280911e4c78ee981ead038549f`.
- `/tmp/coo-limestone-caves-restore.py`, SHA256`57ba53c181362212478552a3f185f16a377333650fed84cbed1868bc6badea82`.

Compared against the previously reviewed shallow-cave helpers. Ten generic package functions are AST-identical, including source/build receipt validation, canonical path checks, collision-safe writes, tree manifest and full archive/hash/mode/gzip checks. Package main and native XML parser control flow are unchanged after normalizing string labels. The new evidence constants independently match the retained36/395XML hashes and431disjoint Passed identities; no failed attempt or overlapping earlier run is counted. New output folder names remain source-SHA-bound `DATE-limestone-caves-SHA9-current`, with source revision and freeze time supplied explicitly by root.

All three restoration function control flows are unchanged after string normalization. Scope changes are limited to the limestone receipt name and exact new PNG/catalog/test metadata allowlist. Existing generated-settings allowlist, reviewed hash/source/index/snapshot binding, normal saves/preferences/prior-build checks, explicit apply gate and exact introduced LastGameID aliases are retained. No broader preference or cleanup permission was introduced. No concrete helper regression was found. This is preparation approval only: fresh builds, archive streaming, package gameplay and the actual final restoration receipt must still be reviewed after root performs them.
