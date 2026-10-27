# Sodden travel source review

Review status: bounded source pass after the P2 region-parser correction. No unresolved actionable finding in the reviewed changes. The native allocation measurement is explicitly inconclusive; this report does not certify zero runtime allocations, gameplay completion or release delivery.

## Scope and provenance

Read the active Sodden adapter, its public/submitted rendering tests, current Greybox routing, full/incremental liquid overlay wiring, and both Sill/Sodden hidden-cultivation corrections. Followed actual owner selection, regional map authority and renderer dirty-neighborhood dependencies. Read the retained native XML receipts, and visually inspected the revised28-frame source contact.

This was a cold review delegated separately from Unity operation. This reviewer also drafted portions of the adapter and regression patches; it is not a claim of wholly independent authorship. Root owns native execution, actual Play, profiling, preservation, packaging and Git. No Unity, Git, build or game process was run by this reviewer, and no active project file was edited in this review.

## Finding and correction

**P2 — repeated region-address parsing in per-cell rendering; corrected in saved source.** The earlier `FlatPixelSoddenTravelCatalog.IsSupportedZone` called `WorldMap.FromZoneID`; `WorldMap.cs:258` uses `string.Split`, creating an array and substrings. The gate runs at regional adapter selection and again in ground/foreground/reader readiness. The saved correction at `FlatPixelSoddenTravelCatalog.cs:50–54` maps only the already-whitelisted Stop/Crossing/Works constants to x15/16/17 and y7. Current cached-zone identity, map shape, live biome and POI checks are retained. No admission result is cached across map changes. Cold source inspection confirms the allocating parser call was removed from this path and the three constants match `SoddenDistrictPlan`.

The three native allocation probes were **inconclusive**, not RED or GREEN: `Docs/Verification/Live16x24/sodden-scope-allocation-probe.xml` records0 passed,0 failed,3 inconclusive. Its aggregate `result="Passed"` must not be quoted as three successful tests. Every case reports that the current-thread counter failed to detect deliberately retained positive-control arrays; therefore the1024-lookup measured loop was not reached. Live per-address biome rejection/restoration assertions occur before calibration. This is source-level removal of a known allocating call, not an empirical zero-allocation claim. Root is rerunning the307-case combined suite and owns real gameplay profiling.

## Reviewed behavior

- Exact supported district addresses plus current cached graph and actual Sodden/no-POI map authority remain bounded. Opening `IsAdmitted` was not widened.
- Selected visible owners govern native foreground and subject images. Remembered/unexplored cells cannot acquire the new regional kit; reader images do not authorize their own documents. Duckboard portraits remain isolated frame0.
- Current registration, exact semantic ownership, one-cell footprint, custody, visible Render, variants, materials, structure and owner-effect/thermal guards remain intact. Native RGB does not override unsupported active owner effects. Tile energy remains separately represented.
- Mire retains real bog-mire identity and coating even at zero stock. Frames16/22–24 are filled;17/25–27 are depleted wet film. The existing weighted coordinate function is reused; no random mutable state, cache growth, simulation write or decorative animation is added. The area test checks the approved family, quiet majority, repeated-query/owner-replacement stability, stock/HP/coating integrity and matching reader images without copying the hash.
- The grass correction relaxes only the generic Terrain veto for exact neutral Reeds/DeadTree/Duckboard. Actual competing cultivation/liquid/source/trigger markers and independent live tile state still veto first; unknown/custom variants do not qualify. No Grass or StoneFloor owner means no fabricated native substrate. Submitted-map tests exercise removing and replacing the actual lower floor, quiet rendering and no extra full redraw.
- Sill and Sodden no longer call the gameplay cultivation helper before disclosure checks. Visible owned cultivation remains a veto. Hidden/foreign Render markers cannot disclose themselves by erasing ordinary ground. Malformed actual floors still use conservative refusal, as deliberately scoped.
- `RepresentedMire` is distinct from Water/Brine and only follows successful exact native mire. Full and incremental paint receive it. Only the sole live bog-mire wave is relinquished; mixtures, unsupported owner/resource state, covering actors and higher-priority cloud/residue/energy cues retain truthful behavior.
- Optional resource outputs are isolated from earlier successful portable sprites. Borrowed resource groups remain independent. Disposal destroys generated Tile/Sprite objects, not borrowed textures or selector. The existing dirty-neighborhood path handles board adjacency without a new update framework.

## Evidence and limitations

Retained XML read directly: original99-case native RED(3 passed/96 failed), initial99/99 GREEN, hidden-cultivation2/2 RED, pre-visual-revision295/295 GREEN, and actual-defect10/10 RED. The latter identifies a filled-frame set of only16, three dry-overlay lower-ground failures and the revised StoneFloor expectation; the five negative-condition cases initially fail their required neutral baseline before reaching refusal checks. These are overlapping suites, not additive unique coverage.

Root reports304/304 after the28-frame visual correction and before the scope-parser correction. The three allocation probes are separately retained and inconclusive; the307-case combined post-correction rerun is pending at this review. Do not describe it as307 GREEN on the basis of an aggregate passed result if those three probes remain inconclusive. Earlier actual crossing, board reader, damage/Break and shelter captures belong to the22-frame candidate. Revised28-frame source contact removes the repeated paired-mound silhouette; it cannot establish whole-scene acceptance. Final28-frame ordinary routes, remaining district travel, save/reload, actual profile and packaged observation remain root-owned. No physical Deck, standalone build, all-biome or complete-world claim follows. Known actors, functional props and other regional identities remain outside this bounded material slice.

## Final source binding

The following hashes identify the saved candidate inspected after the exact-address parser correction. They bind this source review, not a packaged build or final gameplay capture.

Review updated 2026-10-27T05:40:01.537974+00:00 UTC.

- `Assets/Scripts/Presentation/Rendering/FlatPixelSoddenTravelCatalog.cs` — SHA256 `51a98488283b3e98189849e8221e3e70995e0a1d3181e43f4e68cecac337ce5d`
- `Assets/Scripts/Presentation/Rendering/FlatPixelSillEnvelopeCatalog.cs` — SHA256 `e39d3b4bdbb44d604ebe708ae52108369f6368180680fd04d09c3e39d20220fa`
- `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs` — SHA256 `a1cd346064c95442e9d7a7aa2ed212074f522e729d4f0d81b520d95a3458d4d7`
- `Assets/Scripts/Presentation/Rendering/ZoneRenderer.cs` — SHA256 `5cdb7644f8ac78cc6f3566b156442a20de638fb9532eb02de8f8cbb916aaebf3`
- `Assets/Scripts/Presentation/Rendering/ZoneRenderer.FlatPixelPaint.cs` — SHA256 `bce2c41b89e1c5a7002786c74a70c6b2b7f36046b0a53b67dc2c18b8f47e5c88`
- `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelSoddenTravelTests.cs` — SHA256 `75a62a25db17d9eb6d79014d78c5a9e0f1cfc80a25e51ce53fc92078e6516082`

Root acceptance addendum: the subsequent combined native XML records304 passed,0 failed,3 inconclusive. Final actual three-district gameplay and Editor restart are recorded in sodden-travel-play-summary.json, including13 original captures. This addendum does not turn the reviewer into a gameplay operator.
