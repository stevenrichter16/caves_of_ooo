# Slate depths 7–8 — next bounded material coverage slice

**Status, 2026-10-27: native integration under gameplay verification.** Shared
furniture source and fresh package evidence are verified on main atdc6ed52.
Slate7–8 baseline, original six-frame source, runtime and36 RED→GREEN cases
are observed; related tests and actual postintegration gates remain pending.
This updates the work-order context of [the earlier slate audit](GRAPHICS-SLATE-CAVES.md),
without rewriting its history. The initial preflight below is retained with later evidence sections. Root owns
Unity, normal gameplay and Git; no completed area-polish claim follows.

The cave reference experiment remains parked/default-off. Keep **straight wall
silhouettes**, flat native16×24 and the normal lighting/grade. The retained
preference for shaded layered2D material relief and quieter flat floors does
not authorize another isolated parity detour. Remaining world families and the
later serious biome-by-biome/area-by-area polish pass stay open; none of the55
inventory or five Stump area polish statuses changes.

## Corrected source contract

All production references in this table were reread for this preflight.

| Potential false premise | Verified contract and consequence |
| --- | --- |
| Shale and Slate share a depth or structural profile | [SolidEarthBuilder:42](../Assets/Scripts/Gameplay/World/Generation/Builders/SolidEarthBuilder.cs:42) assigns SlateWall/SlateFloor only to7–8, between Shale5–6 and Quartzite9–10. [SlateWall:8584](../Assets/Resources/Content/Blueprints/Objects.json:8584) has HP/MaxHP38, hardness3 and real Rubble wreckage, not Shale28/2 or Limestone35/3. Surviving damage1–38 remains the same body. |
| A blue comma is enough to identify slate | [SlateFloor:8875](../Assets/Resources/Content/Blueprints/Objects.json:8875) is exact blueprint SlateFloor, comma, `&b`, GlyphVariants `,,,.'` followed by a backtick. [Terrain:3438](../Assets/Resources/Content/Blueprints/Objects.json:3438) supplies layer0/Terrain; no authored Material, Thermal or Destructible is added. Match actual owner/parts, not glyph alone. |
| Slate needs a new physics material | SlateWall inherits [Wall:3280](../Assets/Resources/Content/Blueprints/Objects.json:3280): solid, layer0, Stone/Mineral, combustibility0, Thermal flame1200/heat-capacity3. Slate changes render to `#`/`&b`/`^k` and its structural values. Preserve existing mechanics and real thermal/status cues. |
| The sixteen existing images are adjacency masks | [EnvironmentSpriteRenderer:527](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:527) describes spatial64×64 macro fields; [floor mapping:567](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:567) and [suffix loading:797](../Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs:797) use SlateFloor and m00…m15 positions. These are opaque16×16 floor slices, not16×24 wall masks. |
| Depth proves original natural generation | [UndergroundLayoutPlan:37](../Assets/Scripts/Gameplay/World/Generation/UndergroundLayoutPlan.cs:37) permits ordinary roomed geometry for selected Spread/Sodden/Beating columns. It is cold generation input, not saved render provenance. An ordinary material adapter may cover valid owners in either layout; it must not claim to know a restored graph's original recipe. |
| Every open cell has SlateFloor | [StrataBuilder:41](../Assets/Scripts/Gameplay/World/Generation/Builders/StrataBuilder.cs:41) uses the mid-depth cave parameters, and [PlaceFloor:99](../Assets/Scripts/Gameplay/World/Generation/Builders/StrataBuilder.cs:99) rolls85% floor/10% Rubble/5% empty. These are probabilities, not an inspected census. Retain disclosed owner-free ambient backing without creating floor entities. |
| An ordinary column contains only six material owners | [CreateUndergroundPipeline:580](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:580) also applies connectivity, stairs, optional route reservations, landmarks, hazards, population and containers. Generic Wall, VineWall, pillars, mineral nodes, hazards, actors and services are separate identities, not aliases for SlateWall/Floor. Actual presence remains to be censused. |
| Copy the current Shale companion wholesale | Shale now also contains the parked opt-in parity experiment. The simpler [Limestone companion](../Assets/Scripts/Presentation/Rendering/FlatPixelLimestoneCavesCatalog.cs:12) is the bounded policy template; change only verified Slate semantics, depth band and new art contract. Do not copy the study's alpha silhouettes, under-wall floor recesses, preview/light controls or72-frame contract. |

The proposed finite identity set is **two new material owners** plus four exact
borrowed controls: Rubble and Stalagmite retain centered native16×16 resources;
StairsDown uses opening-anchors frame4 and StairsUp opening-cellar frame0.
A provisional six-frame96×24 atlas (floor0–3, wall4–5) is sufficient to evaluate;
final palette/frames remain contingent on actual before views and part review.
No broader family is claimed covered because it appears in a Slate zone.

Read-only decoding reconfirmed the16 old Slate macro images:4096 opaque pixels,
RGB78/82/96 (3243),86/90/104 (550),68/72/86 (303). Preserve all original PNGs,
metadata and editable sources. Their blue-gray material direction can inform
original native16×24 production; stretching those squares or repurposing their
macro indices is not the proposed work. Root must judge collision contrast and
repetition under the actual normal Cave grade, not compensate from a concept.

## Initial earned baseline and planned additional travel (historical preflight)

The retained [actual package receipt](Verification/Live16x24/slate-caves-before-play.json)
and directly inspected [entry image](Verification/Live16x24/slate-caves-before-01-packaged-entry.jpg)
show a narrow, very dark glyph corridor with player40,12 HP33 on the up stair.
Root originally Continued97 at depth5, used `>` to6, moved Up to40,12 and used
`>` to7; F5 saved [fixture98](Verification/Live16x24/Fixtures/slate-caves-entry98/Quick.json)
at `Overworld.16.13.7`, turn12620, HP33. Normal `<` returned to6 without damage.
This is an entry/return observation, not depth8, a full chunk or a material reader.

Read-only fixture hashes still match the receipt:

- Quick.json: `d18714d583a8210a3c023950bc9f3a09a63647d0f125a5e21435c26fa5617bc1`
- Quick.sav.gz: `948d90f6114983c8f229215a1394e29d87434cc8067c71c5cb695c03a3363d19`

The least additional travel is unchanged earned98 in root's usual isolated
save session, ordinary Continue, inspect the up stair/real floor/wall readers,
then explore in short normal movement batches for the actual down stair.
Depth8 and its stair position have **not** been observed; do not infer a safe
route from hidden saved entities or generate a prettier graph. Both depths
use population tier3 by [PopulationTable:1102](../Assets/Scripts/Data/Tables/PopulationTable.cs:1102),
so preserve actual combat, damage, refusal and save outcomes. The earlier
undamaged return is not a promise that the next route is safe.

## Admission, backing and essential counterchecks

Prepare only canonical7/8 addresses and preserve the established special-column
exclusions (sinkholes, RootSite, MultiCellPilot, authored wilderness). Require
[WorldLocationContext.For](../Assets/Scripts/Gameplay/World/WorldLocationContext.cs:5),
exact current CachedZones reference, correctly sized live map/POI arrays, null
current POI and the read-only [saved-lair exclusion](../Assets/Scripts/Gameplay/World/Generation/LairStacks.cs:203).
No generating/ledger-adopting query or global regional admission bypass. Source
eligibility is current authority, not proof of historical generation.

Keep actual floor-owned LocalGround separate from owner-free AmbientBackdrop.
The [existing backing policy](../Assets/Scripts/Presentation/Rendering/FlatPixelLimestoneCavesCatalog.cs:124)
refuses nonempty tile state, competing visible meanings/authored backgrounds and
missing/rejected real floor art. Empty-cell/Rubble backing must not disclose
hidden, foreign or custodied occupants or invent passability/material ownership.
Ground beneath an actor never suppresses that actor. Selected-body and authorized
reader identity, memory/FOV, coatings, heat/charge and dirty clearing remain real.

Required genuine RED pairs, before any production adapter:

- Exact7/8 six-owner positives versus6/9, stale graph, special/POI/saved-lair and
  malformed map refusals; genuine visible native preconditions before mutation.
- Slate38/3 structural semantics, surviving damage and real destruction→Rubble;
  exact render/variant/owned parts, custody/footprint/reskin/foreign-role refusals.
- Actual floor versus Rubble/empty backing; actor covering/uncovering, hidden-owner
  invariance, missing independent resources/disposal and real reader authorization.
- Submitted full/dirty paint, removal and water/heat/charge/cultivation fallback;
  no view-only test substituted for actual submitted layer checks.

Use cached Sprite/Tile views and existing finite renderer fixtures. No per-query
resource loads, scene scans, parsing, sprite creation, gameplay events or RNG;
no borrowed texture destruction. Profile actual work and preserve unsupported
allocation probes as inconclusive rather than inferring performance from code.

## Finite gates and remaining world scope

1. Root captures fresh ordinary7/8 entries/returns, wider disclosed routes,
   owners/readers and state census before authoring; retain the actual room,
   landmark, service and actor gaps encountered. Do not seed props for fullness.
2. Actual capture→built-in imagegen study→owner/part extraction→original editable
   native production. Reject changed fog, geometry, UI, scale and generated pixel
   resampling. Keep straight boundaries and accepted source art stable.
3. Genuine RED, minimal companion/hooks, native GREEN and related regressions;
   then actual normal-zoom material/backing/route review. Fix named defects only.
4. Ordinary collision/destruction/Rubble traversal,6↔7↔8, reader and fresh Continue
   checks; bounded profile, independent review, same-commit living records, exact
   source push and fresh packages. No Linux/Deck execution claim without running.

Slate native materials do not finish the underground. Quartzite9–10, Obsidian11+,
special/authored columns, uncovered actors, mineral/hazard/service owners, and
remaining surface biome/settlement/interior families stay in the broader queue.
Already shared storage/furniture can retain their own valid global body contracts;
this slice must not reimplement them. Whole-chunk fullness and later serious
area polish require separate actual evidence, not atlas or owner counts.

## Actual baseline and concept extraction — 2026-10-27 23:21 UTC

Root resumed unchanged earned98 in the normal scene under isolated native16×24
presentation, study disabled. Actual before01 shows7at40,12HP33; reader02
confirms SlateWall39,12 at38/38/Stone;03reads SlateFloor40,11. Up2 reached visible
StairsDown40,10; ordinary>reached8at40,10HP33. Right4 disclosed the wider8view
at44,10 and F5earned114turn12690. Left4/<returned normally to7at40,10HP33.
Captured01–07 and archived all7 sessionfiles before normal isolation Finish;
Play false/rootnull/scene clean verified. No teleport, reveal, heal or save edits.
Current visible census is disclosure-limited, not an entire-chunk census.

Imagegen prompt01 uses actualbefore06 plus the approved shaleatlas as craft
reference. Outputscene-reference01 is an AI concept, not actual gameplay.
Its compact layeredbluegray wall planes and quieter floor are useful cues;
new boundarywall geometry, fog reinterpretation, oversized floor cracks,
1586×992 output versus1280×800 baseline and UI/scale drift are rejected.
Production extracts fourquiet original floorvariants andtwowallbodies only,
with straight opaque16×24boundaries. Generatedpixels are not copied/resampled.
Floorbase78,82,96 is frozen for backing continuity, with86,90,104/68,72,86
accents in atmost5%ofeachfloor. Wall internal planes will be authored separately;
actual normal-grade contrast remains a postintegration gate. Allpixels are
original integer-cell editable geometry, retaining the existing sharedcontrols.

The36-case reflection-only native fixture is copied from the reviewed scratch
draft before production. Native RED is pending; there is no Slate runtime or
resource atlas yet. Optionalmotion and the parkedShalestudy stay excluded.

Native RED observed before production: job58a7b38979104f1ba354229c3a8d9e36 completed36/36 with36Failed,0Passed. The missing adapter/native resource route fails explicitly, including the actual8 vertical path falling back to glyph instead of opening-anchors. Full rawreceipt slate-materials-red.json retained. No production implementation existed at that run. Independent cold fixture review found no concrete API/setup blocker. Floor anchor78,82,96 is confirmed.

Hourly report actually sent2026-10-27 23:28UTC; next00:28UTC. Current source
production is in progress after genuine RED36. v1 wall contact rejected as
three detached rounded chips repeated per cell; bounded wall-only revision to
continuous open-ended cleavage planes authorized, with floors/palette/straight
opaque boundaries retained and draft-v1 saved. No actual postintegration or
GREEN claim yet. Shared-furniture publication is verified atdc6ed52.

## Native integration checkpoint — 2026-10-27 23:32UTC

Original v2 native atlas e3fe86854fb139721f42058585bd5d000f266fde58f2b426451dff6ad1dfb1ff
is installed as96×24 Point/16PPU/Clamp/uncompressed. v1 detached-chip design
remains preserved; v2 has two unequal open-ended cleavage planes. Independent
contact review allows an actual trial but notes repeated paired slabs as a
remaining polish watch. Floor1536pixels, palette and opaque straight edges
are unchanged by that wall-only correction.

Minimal companion plus Greybox hooks preserve exact Slate7/8 semantics, real
floor versus ambient backing, selected foreground/authorized readers, hazards,
memory/refusal and borrowed-resource lifetime. Independent source review found
no actionable defect. First scripts-only refresh lacked the new file import
and produced CS0246; full AssetDatabase refresh imported it and cleared all
compiler errors, without changing production source. Native jobbf36ca1384e94f3fb135b8874457135b
then passed36/36 after genuine RED36/36. Related tests and actual screenshots
remain separate gates; no allocation, performance, package or parity claim.

## Actual integration checkpoint and self-review — 2026-10-27

Native focused36 and related234 are270 distinct Passed,zeroFailed/Skipped.
The focused fixture includes non-vacuous identity, wrong-depth/reserved-column,
current-graph/POI/lair, disclosure/custody/duplicate-parts, partial damage, actual
destruction, dirty submitted layer, hazard, missing-resource and lifetime
counterchecks. Related fixtures cover neighboring Limestone/Shale/Shallow,
Greybox/fallback and shared furniture. No new gameplay mechanic or Qud-parity
claim: this is CoO-original presentation of existing owner semantics.

Actualafter01 resumes earned114atdepth8/44,10HP33 for the matched baseline.
02reads SlateWall44,7 asStone38/38; a normalUp keeps the player44,8 and reduces
structure38→37. Fourteen ordinary strikes in total destroy wall98277 into real
Rubble100658; Up then enters44,7. 06/07show rubble/floor readers;13shows the
borrowed Stalagmite portrait. Normal stairs8→7→6→7→8 preserve health33 and
straight-wall settings. Depth6 is a Shale control, not new Slate coverage.

F5earned115saves8/44,10turn13050HP33. After archive/Finish and a fresh isolated
Continue,15restores position,HP,currentvisible census and exactpassable
Rubble100658at44,7. The saved14→continued15world rectangle differs in13695
of564000pixels; no exactvisualidentity claim. Before06→after01 changes273850
pixels in the same crop and is an actual material comparison, not proof of
isolated pixel changes. Rawreceipts and all15aftercaptures are retained.

The74.793-second Editor profile contains27822samples without wrapping.
ZoneRenderer mean0.1118ms,p990.2375ms,max34.1552ms; MainThreadmean2.6801ms,
max1658.9924ms; draws/batchesmax42. It includes Look/reader, walking, wall
strikes and tool/idle time. No zone travel occurred during that window. No
controlled speedup,FPS,allocation or physicalDeck claim follows. Unity reports
two memoryless depth load/store messages; no clean-console claim.

- 🟡 Fixed before integration: three detached stacked stone shapes in first
  contact. v1 retained; v2 uses unequal open-ended planes and unchanged floors.
- 🔵 Independent source review found no actionable routing/state/lifetime issue.
- 🧪 Actual review accepts only bounded material readability: normalCavegrade
  warms the blue-gray source; repeated paired wall slabs, frequent olive Rubble
  and generic/glyph owners remain concrete polish/coverage debt.
- ⚪ This column is not an entirechunk/biome polish review. No all-layout,
  special-site,actor/mineral/hazard or freshpackaged acceptance is implied.

Both owned sessions were archived with matching hashes (7files then3files),
then normal isolationFinish completed. Root verifiedPlayfalse,SaveRootnull,
scene clean. Normal saves,currentbuilds,approvedart and49foreignpaths remain
preserved; no storage reclamation. Final independent review, exactmainpush and
freshSlatepackages are the next gates. Last actual hourlyreport23:28UTC;
next2026-10-28 00:28UTC.

Files: new Slate catalog andmetadata; minimalGreyboxhooks;36-casefixture andmeta;
96×24Resourcesatlas/importmeta; original sourcebuilder/editablepacket including
AIreference,partgeometry,retainedv1,validator/reproducibility receipts; earned
114/115fixtures,baseline/aftercaptures,test/profile/comparison/preservation
evidence; living artdirection,completion,worldcoverage and bothJSONledgers.

Final independent review is saved atVerification/Live16x24/slate-materials-independent-review.md,zeroP1/P2. It independently reproduced270distinctpasses/all15capturehashes. Restart13,695pixels differ only withinx373–567/y194–389 nearplayerlight,max2/255/channel; a lighting-phase cause is an inference. No lostgeometry is visible. Root also checked all19 final sourcehandoff dependencies. Exactpublication andfreshpackages remain pending.


## Fresh delivery and restored environment — 2026-10-27 23:54 UTC

Source `702cb9d8096e6dd8eebfd93cdeb08b22d5aece27` is verified on
`origin/main`. The frozen source produced fresh Mac and Linux builds with zero
errors: Mac has five warnings, Linux four. The native evidence remains the same
270 distinct passes; packaging is not another native test run.

Eight actual Mac captures cover two processes. Normal Continue115, Rubble and
SlateWall readers, movement onto the destroyed wall's real Rubble and F5 produce
fixture116 at44,7 / HP33 / turn13080. Full quit, launch and Continue116 restore
that state. Independent comparison of captures05→06 finds zero changed pixels
in the 940×600 game rectangle. Ordinary stairs8→7→8 then retain real Charred Husk
ember/fire damage: HP33→25 and Charred, saved as117 at40,10 / turn13170. This is
not a damage-free route or a new packaged wall-destruction test.

The Linux transfer archive is128,812,762 bytes with SHA256
`805f5d7068c074cb0f42eb241889e93fee1782e0515ebb716dd447d6c222c352`.
Independent full read verifies202 safe members,189 file hashes/sizes/modes,
13 directory modes,188 internal checksums, credits/licenses, and gzip EOF.
The ELF64 x86-64 header is checked; Linux and physical Steam Deck were not run.
Both Mac logs retain six premature thread-finalization messages and one
ComputeBuffer disposal message each. No clean-log or sustained-performance claim.

The bounded restoration receipt at23:53:50UTC verifies all52,003 tracked snapshot
objects equal, including49 preexisting dirty files. Four build-generated settings
and the owned Slate importer whitespace were backed up and restored. Normal
Editor saves/preferences and previous builds remain unchanged; the owned Mac
test saves were archived and the previously absent standalone save root restored.
No cache or drive cleanup occurred; available space is408,061,194,240 bytes.

Independent actual review accepts these material and delivery checks. Repeated
paired slabs, olive Rubble, broad quiet floors, glyph hazards/actors and whole
chunk composition remain explicit coverage/polish debt. All55 inventory and five
Stump polish statuses remain unchanged. The Shale study stays parked and disabled.
Delivery evidence publication is the remaining administrative gate. Next source
scope is [Quartzite9–10](GRAPHICS-QUARTZITE-MATERIALS.md); those depths are still
unobserved. Last actual hourly report23:28UTC; next2026-10-28 00:28UTC.

Evidence: `Verification/Live16x24/packaged-slate-materials-actual-play.json`,
`packaged-slate-materials-independent-review.md`, `packaged-slate-materials-linux-archive.json`,
`packaged-slate-materials-restoration.json` and the actual Mac PNGs in that folder.
