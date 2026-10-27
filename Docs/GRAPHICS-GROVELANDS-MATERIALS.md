# Grovelands native materials and forest envelope

2026-10-27. **Status: materials plus bounded permanent Fen-water integration are Editor-verified; latest native GREEN is 600/600. Sixteen material and seven water captures are retained; combined Grovelands packaging pending.** Root owns Unity, actual four-formation baseline captures, imagegen, native source authoring, integration and Git. This document retains the preimplementation contract and records the bounded verified integration. The user prioritizes coherent static sprite coverage across every biome/area and actual whole-chunk fullness ahead of subtle animation. It is not a whole-Grovelands completion claim.

## Scope and production gate

Follow [the source preflight](Verification/Live16x24/grovelands-next-kit-preflight.md) and the required capture → imagegen → owner/state extraction → editable native art → real integration → native/actual gameplay/package comparison cycle. Preserve all approved art. First make existing ordinary Grove, TendrilFen, FruitingWall and CompostingField owners legible; do not fill readable approaches with invented decoration.

New atlas contract `Sprites/Live16x24/grovelands-materials`: **14 native 16×24 static frames** (224×24). Root approved the independently authored native contact for the first trial after actual baseline and imagegen critique; bounded actual gameplay observations are recorded below; whole-area acceptance remains open. Frame ownership established before implementation:

|Frame|Exact owner/state|
|---|---|
|0–1|MycelialColumn quiet body variants|
|2|GroveSeep with positive finite water stock|
|3|GroveSeep empty stock, retaining wet filaments and actual independent water state|
|4|GroveSign|
|5|GroveRedGrowth|
|6–7|FruitingBody quiet solid shelves|
|8–10|CompostRow low remnants|
|11|CompostCache; no guaranteed visible coins|
|12|ChoirIronVein deposit, distinct from carried ChoirIron|
|13|WineLeafSundew reusable step-snare plant|

Four **borrowed owner families**, not four individual frames: Grass uses Sill20–23; VineWall uses Sill0–15 (N1/E2/S4/W8); Tree uses north-vegetation0; Bush uses opening-base5. All pixels remain immutable. DryBrush, alternate hazards, BloomingFruitingBody, crops, actors, furniture and special sites are outside this first adapter. Plain tile-state water remains real tile-state presentation, not a fabricated regional river owner.

## Preimplementation source corrections

|Potential false premise|Saved correction and source|
|---|---|
|No TileStateSource means GroveSeep has no projected water; zero stock can be painted dry.|`Zone.ProjectPool` writes a permanent coating for an ordinary LiquidPool without checking Volume (`Assets/Scripts/Gameplay/World/Map/Zone.cs:796–806`). GroveSeep has no TileStateSource, but normal AddEntity still projects water. `WellDrinkingService.cs:31` only reduces Volume. Empty must retain the actual wet cue and no draw availability; never clear test state just to make art pass.|
|The same helper establishes full/empty native eligibility.|`LiquidSourceSafety.IsUnmixedPool` rejects Volume<=0 (`Assets/Scripts/Gameplay/Items/LiquidVesselService.cs:290–294`). It is a service-availability helper, not an empty-body gate. Check exact owned water pool, nonnegative stock, current neutral phase and actual unmixed state independently; keep the world overlay.|
|All vegetation has Material, Thermal and Destructible.|Column/Fruiting own all three; WineLeafSundew owns Plant+Destructible but no Thermal; GroveRedGrowth has none. GroveSign, CompostRow/Cache, ChoirIronVein and GroveSeep also lack those authored parts (`Assets/Resources/Content/Blueprints/Objects.json:29051,35118,35167,35199,36174,36229,36287,36445,36486`). Do not fabricate required parts or require the original finite stock40.|
|GroveSign is another RegionalSignpost.|It is solid Furniture with ordinary Examinable prose, no RegionalSignpost/Material/Thermal. Preserve actual grown-sign meaning rather than borrowing dynamic directional semantics (`Objects.json:36174`).|
|Compost rows promise coins or are a ground Terrain blueprint.|Row and Cache both inherit PhysicalObject, have RenderLayer1, are non-solid/non-takeable, and have no Terrain tag. The generator inserts Cache after a real Row; the existing fallback selector's >= tie chooses the later visible owner (`GrovelandsFormationBuilder.cs:322–370`, `FlatPixelFallbackCatalog.cs:72`). Native art follows that selected owner; it does not reorder the pile.|
|A failed cache roll leaves the source.|Cache yields1–3 GoldCoin at60%; a valid failed roll still consumes it. Existing `HarvestablePart.cs:120–190` owns atomic production/removal. Tests must show Row remains in both outcomes and no extra coins are invented.|
|Sundew is a Creature or triggers from adjacency.|WineLeafSundew is a non-solid plant with GreatdewSnare45%/three-turn hold/corrosion1.5 and ConsumeOnTrigger=false, no authored Thermal (`Objects.json:36445`, `TriggerOnStepPart.cs:440–482`). Show the hazard body without changing its existing mechanics.|
|A pale column requires a new glow animation.|Its actual LightSource is radius3, intensity0.5, green light; the body is pale with violet veins. Borrow quiet static pixels and retain current light state; no new light, pulse or mutation (`Objects.json:35118`, `LightSourcePart.cs:13–32`).|
|A renamed world cell is ordinary Grovelands.|`GrovelandsCompositionPlan.IsWildernessZone` excludes authored named places, sinkhole mouths and the doll grove. Current map dimensions, live biome/null POI, exact cached graph and read-only saved-lair veto are additional mandatory authority (`GrovelandsCompositionPlan.cs:35–47`, `FlatPixelBeatingMaterialsCatalog.cs:47–64`). No renderer-created ledger or generation.|
|Forest underlay must be removed beneath every Terrain or trigger owner.|Exact neutral dry Column/Fruiting/Bush and the mapped Sundew body can sit over actual Grass. Unknown terrain, cultivation, pools and independent tile state cannot be erased by a generic green fill. Hidden/foreign/custodied owners cannot change disclosed ground.|

## Proposed implementation seam

The implemented `FlatPixelGrovelandsMaterialsCatalog` follows the existing Beating companion; no new renderer framework. Constructor injection is `(Texture2D atlas14, Texture2D sill24, Texture2D vegetation4, Texture2D opening10, FlatPixelFallbackCatalog borrowedSelector)`, plus a normal resource constructor taking the selector. Public methods: `IsSupportedZone`, `TryResolveForeground`, `TryResolveGround`, `TryGetSubjectSprite`; Greybox field `_grovelandsMaterialsArt`. Native tests use reflection so absence produces behavioral RED without a missing-type compiler failure.

Precompute only immutable candidate addresses. Query current manager/map/POI/saved-lair authority on every use. Use only current visible selected owners; exact subject identity/cell, custody, sole owned essential parts, single-cell physical body, unmodified visual identity and active owner tint determine eligibility. Successful regional art must not widen opening `IsAdmitted`. Cache generated tiles/sprites; borrow source textures and selector without modifying/disposal/reload on missing injection. Current stock and damage may vary without repaint-time simulation or exact original-HP/price gating.

GroveSeep body requires actual owned finite water + usable owned Well, nonnegative stock, supported neutral current phase and sole live water coating; positive stock and zero stock choose different bodies while the actual water overlay remains. Mixed, frozen, expired/missing projection, broken-service, reskinned or foreign parts retain truthful fallback. Dry native ground is never used to conceal nonempty independent tile state.

## Native RED contract and counterchecks

The new **91-case** `CavesOfOoo.Tests.FlatPixelGrovelandsMaterialsTests` exercises public Greybox world and subject routes, backed by the actual entity factory/current manager and the existing submitted ZoneRenderer fixture. It covers canonical owners, all four naturally generated formation fixtures, current authority revocation, hidden/covered/removed/repurposed owners, exact borrowed resources and disposal, stock→actual draw→empty-still-wet, real harvest/source removal, cache failed-yield/row retention, disclosed VineWall connectivity and incremental repaint. Each invariant has a same-setup counter; no synthetic test is presented as a natural visit.

Source checks confirm the literal factory blueprint names, injected resource names, reflection-only missing-companion access and existing public gameplay APIs. The test file reuses the submitted-render fixture shape already exercised by Beating; no alternate painter or world generator is introduced.

Independent source review caught one fixture premise after the initial native RED job. The saved accessor correction uses the already-warmed companion before considering a view query. This lets an unexplored-cell subject refusal be tested without first asserting that an unexplored world view succeeds. Root refreshed this saved correction before the second native RED job.

Root native RED receipts (`Verification/Live16x24/grovelands-native-red-jobs.json`):

|Run|Native result and exact bounds|
|---|---|
|Initial, job `d0d082507f664e41b6f83cf81ee65aea`, 08:57:01–08:57:17 UTC|Completed91/91; status failed. Known positive assertions fail because native bodies are absent. This run preceded the accessor correction.|
|Corrected fixture, job `3183baabe1444390b249890841fe10a1`, 08:59:00–08:59:09 UTC|Completed91/91; status failed, before production. MCP retained only25 failure details and returned null final Result. This proves RED and the reported positive failures, not a complete91-distinct-failure count or an XML receipt. The service-object evidence is also saved in `grovelands-native-red.json`.|

Root authorized production after the corrected RED. Native GREEN, bounded ordinary Editor actions and fresh restart are now verified below; package acceptance remains pending. Root's actual four-formation baseline, one normal water draw 40→39 and a normal forage harvest yielding 2 GroveRed are documented in [the baseline action record](Verification/Live16x24/grovelands-baseline-actions.md). The live source population/layouts already supply composed density; no generation change is part of this implementation. The imagegen prompt/concept critique and native authoring are separate root/art-owner work. The native scope does not replace the actual four whole-view after comparisons or prove every one of the 37 eligible addresses was visited.

## First implementation after RED

The 350-line adapter borrows the 14-cell new atlas and 22 used approved forest cells across three existing textures. It follows current map/POI/lair and exact graph identity without mutating authority, and current disclosed owner/sole-part/custody/footprint/material/essential-state selection without changing gameplay. Immutable candidate metadata and generated sprites/tiles are cached. No new per-query LINQ, texture reads, resource reloads, generation or random choices are introduced. Allocation behavior still requires meaningful runtime evidence; it is not certified by source inspection.

The public Greybox route applies a real Grass substrate independently of the selected foreground, then a dedicated Grovelands foreground output so an optional regional miss cannot overwrite global portable equipment/products. Only a represented actual Grass owner has its glyph suppressed. The world subject reader uses the same exact owner and resources; VineWall portraits use an isolated owner frame while its world shape only reads visible current selected neighbors. Disposal frees generated tiles/sprites, not borrowed textures or selector.

The full/depleted GroveSeep native body never suppresses the current tile-state water mark. Heat, clouds and other independent marks retain the existing overlay priority; this does not mean every coexisting channel receives a simultaneous glyph. Mixed or frozen coating, absent projection, changed source, foreign/duplicate parts or a broken service retain fallback. The empty body follows zero finite stock while real permanent water remains; no service helper requiring positive stock is misused as its art gate. Current Column light remains independent from the quiet static body.

Root imported and compiled this implementation. The first combined trial and corrected final GREEN are recorded below; runtime and tests are now frozen during actual gameplay verification.

## Fullness and remaining coverage

Judge actual readable routes and the existing clustered canopy, open grove destination, wet fen banks, broken fruiting shelves and quiet compost bands. Preserve authored central gaps and intentionally sparse special sites. Do not raise density merely to hide missing sprite families. No placement changes are authorized by test success alone.

After the bounded material pass, retain actually encountered Grovelands actor/NPC, shrine, crop, hazard and prop gaps; continue Overwrit/Stump and remaining surface, underground, settlement/interior and special-owner kits. Do not spend this pass on nuanced animation or claim complete regions from static sheets.

## In-phase self-review and changed files

- 🔵 CoO-original presentation work using existing world/transaction semantics; no new Qud parity claim.
- 🔵 Native final GREEN is 495/495. Bounded actual Editor material/interaction observations are recorded below; this does not certify full scenes, all 37 ordinary addresses or packages.
- 🧪 Package delivery, remaining owners and whole-chunk fullness remain open; the fresh Editor restart is verified below.
- 🧪 Empty wetness and the equal-layer cache/row are explicit tests because superficially plausible pictures would otherwise hide current truth.
- 🔵 Independent source review found no further fixture API/setup blocker after the accessor correction and no unresolved Yellow+ runtime/Greybox finding; see `Verification/Live16x24/grovelands-runtime-art-review.md`. That review is separate from native execution.
- NEW `Docs/GRAPHICS-GROVELANDS-MATERIALS.md`: preimplementation contract/corrections and acceptance limits.
- NEW `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelGrovelandsMaterialsTests.cs` and `.meta`: bounded native RED/GREEN tests and submitted-render counterchecks.

- NEW `Assets/Scripts/Presentation/Rendering/FlatPixelGrovelandsMaterialsCatalog.cs` and `.meta`: exact ordinary-region material/forest companion with independent optional resources and owned lifetime.
- MOD `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs`: scoped ground, foreground, reader, lazy resource and disposal integration; existing global portable policy retained.

## First combined native trial and saved fixture correction

The root-owned combined run completed 495 cases at 09:07:38–09:08:13 UTC: **494 passed, one failed, zero skipped/inconclusive**, saved as `Verification/Live16x24/grovelands-native-trial.xml`. The failing case was `SubmittedSeepKeepsItsActualWetOrHeatOverlayAndNoDryGrass(0,True)`; all other Grovelands/related execution occurrences passed in this trial. The final corrected GREEN is recorded below.

The failure was a test premise, not a missing production heat threshold. `ZoneRenderer.PaintTileStateMarkWithLiquids` selects live veil mist, residue, oil and then an unrepresented coating **before** Charge/Heat/Cold (`Assets/Scripts/Presentation/Rendering/ZoneRenderer.cs:1310–1354`). An ordinary GroveSeep deliberately retains its permanent water wave, so water plus Heat1 correctly submits CP437 `~`, not `^`. No production renderer or gameplay state changed to satisfy the wrong expectation.

The saved test now asserts the actual water-wave priority and that Heat1 survives unchanged. Its same-case counter removes the water coating in the test fixture while retaining Heat1, requires native seep-body refusal because the mandatory projection is missing, and verifies the existing painter then submits `^`. Fog still suppresses the mark. The suite retained 91 Grovelands cases for the same 495-case rerun; the final XML confirms this corrected case passed without a production priority change.

## Final native GREEN and frozen source

Root's saved `Verification/Live16x24/grovelands-native-green.xml` ran at
**09:12:50–09:13:35 UTC: 495 distinct cases passed, zero failed, skipped or
inconclusive**. Direct XML inspection confirms **91 Grovelands cases plus 404
related cases**. The failed first trial remains preserved; its lone failure
was the coating-before-heat test premise described above, not a runtime
regression. Native fixtures cover exact state/authority and submitted pixels;
they do not establish ordinary discovery of every owner or all 37 addresses.

The 14-frame original atlas is 224×24, SHA-256
`e994ff638167310ec4c50a733a5cdea5d5d23ef170e48297fdc154ca0dad60b2`.
`ArtSource/Live16x24/grovelands-materials/verification.json` confirms exact
native/contact decoded pixels, binary alpha, the runtime copy and **407
protected inputs unchanged**. Editable source, concepts, source checks and
reproducibility evidence remain in that directory. These authoring checks are
not additional native test or whole-scene acceptance. Runtime/tests and approved
source art remain frozen after the bounded actual gameplay/restart checks.

## Actual Editor actions after integration

Root used normal input from the **unchanged earned Grove baseline fixture**,
then ordinary world-map travel through the same four formation families. HP
remained 38 during the reported actions. The following 16 captures are actual
Unity Editor game views, not concepts or contact sheets:

|Capture|Verified observation and limit|
|---|---|
|[01 Grove](Verification/Live16x24/grovelands-after-01-grove.png)|Native forest envelope, column ring and central source in the actual Grove view. The existing layout was retained.|
|[02 seep reader](Verification/Live16x24/grovelands-after-02-seep-reader.png)|Normal context/reader identifies the actual finite seep.|
|[03 seep drawn](Verification/Live16x24/grovelands-after-03-seep-drawn.png)|Actual draw reduces current water stock 40→39. This is not an ordinary empty-stock observation; the last-draw/empty-wet state is covered by native tests only.|
|[04 sign reader](Verification/Live16x24/grovelands-after-04-sign-reader.png)|Actual grown-sign reader; no invented regional-direction behavior.|
|[05 FruitingWall](Verification/Live16x24/grovelands-after-05-fruiting-wall.png)|Normal travel reaches the native solid shelf formation. Remaining actors/props are not accepted by this material pass.|
|[06 CompostingField](Verification/Live16x24/grovelands-after-06-compost.png)|Actual low row and cache scene after normal travel.|
|[07 cache context](Verification/Live16x24/grovelands-after-07-cache-context.png)|Normal context targets the existing source/pile.|
|[08 cache picker](Verification/Live16x24/grovelands-after-08-cache-picker.png)|Normal selection reaches the actual harvest action.|
|[09 cache harvested](Verification/Live16x24/grovelands-after-09-cache-harvested.png)|Actual harvest yields 3 GoldCoin and removes only the cache; its real CompostRow remains. The failed-yield outcome is a native-test counter, not an ordinary observed roll in this pass.|
|[10 TendrilFen](Verification/Live16x24/grovelands-after-10-fen.png)|Normal travel reaches the native shelf/forest banks around real braided water. Plain-water glyph strips remain a visible material gap.|
|[11 forage harvested](Verification/Live16x24/grovelands-after-11-forage-harvested.png)|Actual GroveRedGrowth harvest yields one GroveRed through the existing transaction. This differs from the earlier before-art branch, which yielded two; the intervening cache/travel branch changes the RNG history.|
|[12 inventory](Verification/Live16x24/grovelands-after-12-inventory.png)|Current carried outputs after the normal actions. This does not establish acquisition of every source family.|
|[13 forage inventory](Verification/Live16x24/grovelands-after-13-forage-inventory.png)|Confirms GroveRed `51793`, count 1, alongside retained PaleSalt `42025`, count 1, and GoldCoin `49426`, count 3.|
|[14 forage details](Verification/Live16x24/grovelands-after-14-forage-details.png)|Actual carried forage details after the harvest.|
|[15 restart Fen](Verification/Live16x24/grovelands-after-15-restart-fen.png)|Fresh Editor restart returns to the earned Fen state with HP 38 and the harvested source absent.|
|[16 restart inventory](Verification/Live16x24/grovelands-after-16-restart-inventory.png)|Same three product IDs and counts survive the fresh restart.|

**Fresh Editor restart is verified.** The exact before/after tool receipt is
`Verification/Live16x24/grovelands-restart-state.json`: both records retain
`Overworld.0.8.0`, HP 38, PaleSalt `42025` ×1, GoldCoin `49426` ×3 and
GroveRed `51793` ×1. Seep `46948` retains stock 39; the harvested cache at
64,12 is absent while real Grass and CompostRow remain; the forage at 41,15 is
absent while real Grass remains. The owned restart session was archived at
`/tmp/coo-grovelands-restart-earned-e95598ac` before fixture Finish.

There is no Grovelands packaged-play claim: the current delivered builds remain harvest source
`8ab4508093bb0a7894d659af7c58a770e009e2ab` until root completes the next delivery.
No Linux execution, physical Deck, controller-device or sustained-performance
acceptance is implied.

The four ordinary views provide bounded material-coherence evidence. They do not
complete Grovelands or certify every chunk's fullness. **Fen plain-water glyph
strips, actors/NPCs, props, hazards, crops and special sites remain partial.**
Continue to inspect whole gameplay views, preserving quiet approaches and
existing coherent clusters. Do not add arbitrary decoration or spend time on
nuanced animation to conceal these remaining identity gaps. Regional breadth
continues through Overwrit/Stump and the remaining world after bounded delivery.

## Permanent ownerless Fen water follow-up — 2026-10-27 10:00 UTC

The earlier recorded Fen glyph-strip defect is closed for the bounded permanent
ownerless substrate. The [water implementation record](Verification/Live16x24/grovelands-water-next-preflight.md)
retains its source corrections and RED/GREEN evidence. It reuses unchanged
approved opening-water masks 0–15; none of the 14 material frames, 22 forest
cells or source palettes changed. Source-backed physical water, including
GroveSeep full/empty states, finite water, mixtures, ice and projections keep
their previous cues. No global water admission or overlay-priority change.

The first combined water GREEN passed **600/600 (12 new water +91 original
material +497 related)** with no failed/skipped/inconclusive cases. All 12 new
assertions failed before production in the saved raw native job receipt; no
RED XML is claimed. [Independent source review](Verification/Live16x24/grovelands-water-independent-review.md)
found no unresolved Yellow-or-higher finding.

[Seven actual Editor captures](Verification/Live16x24/grovelands-water-play-summary.json)
verify the exact earned turn-4430 Fen view at 40,15, HP 38, truthful adjacent
water context/reader from 40,16, native player on water at 40,17, opposite bank
at 40,19, return to 40,15 and F5 turn 4510, then fresh isolated Continue.
PaleSalt 42025 ×1, GoldCoin 49426 ×3, GroveRed 51793 ×1, seep 46948 stock 39,
consumed cache with its row, and consumed forage with Grass survive unchanged.
The checkpoint is `Verification/Live16x24/Fixtures/grovelands-water-crossed65`.

| Capture | Actual observation |
| --- | --- |
| [01 matched Fen](Verification/Live16x24/grovelands-water-after-01-matched-fen.png) | Same earned baseline view with native water continuity. |
| [02 edge context](Verification/Live16x24/grovelands-water-after-02-edge-context.png) | Actual adjacent water selection. |
| [03 reader](Verification/Live16x24/grovelands-water-after-03-reader.png) | Grass identity and actual water state remain truthful. |
| [04 player on water](Verification/Live16x24/grovelands-water-after-04-player-on-water.png) | Player body remains visible over the substrate. |
| [05 opposite bank](Verification/Live16x24/grovelands-water-after-05-opposite-bank.png) | Normal traversal reaches the real dry bank. |
| [06 return/save](Verification/Live16x24/grovelands-water-after-06-return-saved.png) | Returned to 40,15 and saved turn 4510, HP 38. |
| [07 fresh Continue](Verification/Live16x24/grovelands-water-after-07-restart.png) | New isolated session restores the earned route and persistent state. |

Material source `a2800c25f572838924e57f7fff6c3ce20c6ed607` is pushed; the water
source commit/push and combined packages remain pending. Current delivered
builds still use harvest `8ab4508093bb0a7894d659af7c58a770e009e2ab`. Next complete
that bounded delivery, then static actors/props and Overwrit/Stump/remaining
world coverage. Actors, NPCs, crops, hazards, special sites and whole-region
fullness remain partial; the actual crossing does not certify all water states
or all 37 ordinary addresses. No new Linux execution, Deck or performance claim.

Normal saves and 38 foreign tracked files remain protected; no cleanup occurred.
Last actual hourly report remains 09:27 UTC, next due 10:27 UTC.
