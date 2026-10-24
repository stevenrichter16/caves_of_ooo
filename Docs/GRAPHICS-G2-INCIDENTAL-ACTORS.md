# Next court actors — bounded source and capture audit

2026-10-24. Status: bounded native/ordinary acceptance complete for Farmer and ReedbackGrazer; see final acceptance section. Full graphics release remains open.

Read-only repository/capture audit by `/root/projection_cold_review`; Farmer source subaudit by `/root/projection_cold_review/look_focus_draft_review`. The initial audit below involved no Unity, gameplay, tests, source edits, art edits or Git operations. Subsequent implementation and actual acceptance are recorded at the end.

## Decision

There are **two different gaps** worth a small actor packet:

1. **Farmer needs a distinct native frame.** The actual seed65 Farmer7139 is a glyph on the cot. No approved Live16x24 Farmer asset or selector exists. Do not silently reuse the exact Villager frame, the Cook, or the keeper cloak.
2. **Reedback Grazer already has the approved native frame; connect it.** Seed64 ReedbackGrazer5033 visibly remains `g` in the east capture. `Assets/Art/ReedbackGrazer16x24/03-ridge.png` is the user-approved 16×24 image, preserved but absent from the current live flat selectors/resources. Reusing those exact bytes is preferable to new grazer art.

Magpie, Viper, Scrabbler and Cook do not need replacement art for this packet. A separate seed65 Cindercaller is another genuinely missing identity; keep it named as a later gap rather than borrowing Scrabbler/Gleaner art merely because its blueprint inherits Scrabbler.

## Actual evidence, not a fresh census

Read paired owner receipts and inspected these native PNGs: `working-court-64-east.png`, `working-court-64-kitchen-2x.png`, `working-court-65-kitchen-2x.png`, all under `Docs/Verification/Live16x24/`.

| Receipt | Actual owner | Observed/current disposition |
|---|---|---|
| 65 kitchen, tick1130 | Farmer7139 at(53,6), visible | Distinct pale `@` over Bed7197 at the same cell. Cook7190 at(51,6) and Magpie7169 at(58,6) already have native bodies. |
| 64 east, tick410 | ReedbackGrazer5033 at(51,17), visible | `g` visible among the worked enclosure. Approved low quadruped exists, but no flat binding. |
| 64 east | Scrabbler4996 at(6,24), visible | Native squat body visible near the lower left; existing static/motion identity already covers it. |
| 64 east | Farmer4957 at(66,5), not visible; Magpie4995 at(61,7), visible in receipt | Do not claim these are both on-screen: the PNG viewport ends before these world columns. Receipt FOV visibility is not screen-frustum evidence. |
| 64 kitchen, tick1060 | Magpie7159 at(28,19), visible | Native bird visibly stands beside the cot. It is not an unhandled glyph. |
| 64 kitchen | Viper7160 at(55,1), not visible at tick1060; visible at(64,1) in entry receipt | Native mapping exists, but these captures do not establish a close visible Viper appearance beside the court. |
| 64 kitchen | SpreadWaysideCook7183 at(27,17), visible | Approved Cook already connected. |
| 65 east, tick410 | Cindercaller4983 at(67,3), visible in receipt | No current native16×24 exact mapping; outside the inspected east screenshot viewport. No claim of visual acceptance. |

The complete creature identities in the saved seed64 east/kitchen receipts are those seven IDs above, determined from actual blueprint inheritance. HP>0 alone is **not** an actor filter: Hatchet and LeatherBoots also have positive recorded HP. These receipts do not claim a complete future population inventory.

Farmer7139's shared cot location persists through harvest/save/load/return receipts. Root's readback establishes Farmer Physics.Solid=true and Bed7197 Physics.Solid=false; the captured owner schema itself has no Physics field. This is legitimate current occupant obstruction, not bad court geometry and not evidence of a sleeping pose. Do not move/remove the actor, change cot passability, or invent sleep art in a graphics fix.

## Source corrections and exact binding seams

- `Objects.json:24562–24644`: exact Farmer inherits Villager, canonical `@`, `&Y`, layer10, Farmer conversation/trade and boots/gloves loadout. Inherited solid physics at `:11909–11914`. No equipped hoe is promised. `LandmarkBuilder.cs:795–812` can spawn a Farmer from MillStead; `VillagePopulationBuilder.cs:147–150` also places Farmers. The owner receipts do not prove which created this particular one, so avoid a new generation-role requirement.
- `FlatPixelOpeningArtCatalog.cs:587–596`: cast identity table is **exact** Villager/PetDog/Scrabbler/Gleaner. Farmer inheritance is not an alias. `ArtSource/Live16x24/cast/README.md:23–32` identifies frame0 as Villager and prohibits silently assigning civilian/marlback frames to another identity.
- `EnvironmentSpriteRenderer.cs:78–90,987–991,1729–1737` has legacy Farmer sprite support. Its PNG is **16×16**, importer uses cropped12×15 (`Environment/farmer.png.meta:90–96`). `Content/Data/EntityVisuals.json:22–37` also aliases Farmer to an older villager animation sheet. Flat mode intentionally exits first at `EnvironmentSpriteRenderer.cs:1152–1177`; bypassing that boundary to revive an older sheet would mix styles, not connect approved native art.
- `FlatPixelOpeningArtCatalog.cs:372–392`: exact Magpie→north-works5, Viper→6, Patchbearer→7/8, with owned Render/Physics and explicit-reskin/custody guards. `FlatPixelActorMotionCatalog.cs:61–73` accepts only Player/PetDog/Scrabbler/Gleaner. Do not widen this motion policy to suppress a new static actor.
- `Objects.json:40878–41000`: ReedbackGrazer is a squat quadruped with blunt muzzle/reed-like ridge, own SpreadGrazer part and Creature/Brain/Body semantics. `SpreadGrazerPart.cs:108–139` shows finite forage and proximity flight; Fed, Configured, current food/hunt/mood are mutable behavior, not body identity or native art permission.
- Approved ridge file measures **16×24**, SHA256 `29db64b90637fdd305a355907c06547c5f46d82f912ec95a88307d92f28b4dad`. `ArtSource/Live16x24/PARTS.md:28–40` records its approved identity, low horizontal bounds `(0,9)-(16,21)`, six colors, and real-owner-only placement. Existing use is the recoverable study (`Tools/Clearing16x24/build_unity.cs:9,68`), not live world binding. Its existing sprite pivot is bottom center (`03-ridge.png.meta:49–54`); a runtime Tile needs a full16×24 center-pivot Sprite.Create without cropping, padding, stretching or changing source bytes.
- `Objects.json:41592–41685`: Cindercaller is a narrow-capped/scorched marlback with ranged fire behavior and mutable cudgel/fire-moss loadout. No permanent flaming hands, mandatory held cudgel, or generic Gleaner medical silhouette is truthful. Existing 3D cindercaller assets are not native16×24 approval.
- Runtime entry is `FlatPixelGreyboxCatalog.TryResolvePresentation` (`:688–745`), after `FlatPixelFallbackCatalog` establishes current graph/cell/top-owner/custody/visibility. Subject route is `TryGetPresentationSubjectSprite` (`:750–769`), with external document authorization. Exact six-graph admission is `IsAdmitted` (`:163–179`). Existing companion pattern is `FlatPixelEastKitchenCatalog` (`:20–54`); resources cached/disposable, texture dependencies borrowed, no world scans. Preserve these authorities.

## Smallest implementation/design proposal

Use one small optional **static local-actor companion** with two independent borrowed16×24 textures: a new Farmer frame and a byte-identical Resource copy of approved ridge. Proposed API: `TryResolve(Entity owner, out Tile tile)`, default loading once, injectable `(Texture2D farmer, Texture2D grazer)` with missing/malformed groups independent. Connect at the existing Greybox world and exact-subject native selection seams; no thirteenth positional opening-atlas parameter, second renderer, animation view or per-frame watcher.

Initially preserve existing native-skin admission for the six current local graphs; do not change global flat projection or silently broaden all regional/native actors. A later deliberate portable-static policy can be tested separately if needed. Exact semantic identities are Farmer and ReedbackGrazer only, with owned visible Render/Physics, Creature and non-Item/non-Player/non-custody, canonical `@`/`g`, and no explicit VisualID/VisualVariant/GlyphVariants override. Verify Body/Brain/SpreadGrazer ownership where those source contracts are used; do not gate neutral body art on stock, conversation availability, HP threshold, faction, friendliness, Fed/Configured, or a court role. Keep existing broader/fallback semantics intact.

Farmer authoring follows the current actual capture→imagegen target/parts→deliberate editable native16×24 construction→import→ordinary verification pipeline. Use a civilian farming silhouette distinct from keeper hat, Cook cap/apron, player hood and Warden cap; no baked tool, crop bundle, sleeping posture or guaranteed trade stock. Static art must not be reported as animated. Grazer needs integration and visual review of unchanged pixels, not regeneration.

## Meaningful next RED and acceptance counters

Keep the new fixture bounded and factory-backed. Before production:

1. Actual Farmer and Grazer in current admitted east/kitchen graph must resolve **new/approved native textures** in both world and authorized examine, not merely any non-null fallback. Initial expected failures are missing companion/routes/resource. Pin native rectangle16×24,16PPU/center pivot and exact approved ridge hash separately.
2. Actual Farmer over Bed: submitted foreground is Farmer; ordinary move/removal repaints the old cell to the real bed without manually marking full dirty. Solid Farmer still blocks entry, non-solid bed does not. No service-state/stock/crop mutation.
3. Foreign/custom identity and explicit visual override/canonical glyph change fall back truthfully; inherited Villager, Cook, Cindercaller, corpse and unrelated `g` never receive either new frame. Existing Magpie/Viper/Scrabbler/Cook native results remain exact controls.
4. Hidden/unexplored/memory, foreign current graph/same-ID replacement, stale cell and carried owner cannot leak a body; world/reader visibility and current-subject lifetime agree. Existing lifecycle fixtures can supply harnesses rather than duplicating every global test.
5. Missing/malformed one texture leaves the other available; injected constructors never load Resources; repeated query returns cached Tile/Sprite; disposal releases only owned objects, not borrowed approved textures or peers. Warm selector stays allocation-free.
6. Healthy/lowHP/Calm or changed farmer stock and grazer Fed/Configured still resolve the same neutral body; no invented readiness state. Actual approved importer path must be exercised after a genuine missing-resource RED.

Relevant controls: `FlatPixelEastKitchenTests`, `FlatPixelGlobalPresentationTests`, `FlatPixelOpeningCastTests`, `FlatPixelNorthWorksTests`, `FlatPixelActorMotionTests`/catalog tests and `FlatPixelFallbackCatalogTests`. Final ordinary revisit should show Farmer at/near the real cot, correct exact subject reader and move-away bed restoration; show the existing real Grazer at1x/2x and its reader without spawning substitutes. No physical Deck, new animation, complete incidental bestiary or full six-zone journey acceptance follows from this small packet.

## Current production pass — 2026-10-24

Root read the source/capture audit before changes. Native initial behavior tests failed on both actual Farmer and ReedbackGrazer because `NativeForeground` was false (`incidental-actors-red.xml`,20:24:33Z). The earlier20:23 invocation raced script import and returned zero cases; it was rejected, not counted as RED or GREEN. No production behavior changed before the two real failures.

Built-in imagegen used the actual seed65 kitchen capture and approved cast contact as references. Exact prompt and concept are in `ArtSource/Live16x24/incidental-actors/`. The exposed head, short waistcoat/rolled sleeves, broad belt and separated trousers are selected for native reconstruction. The generated inset puts the Farmer where the Cook stood and retains the glyph: reject that placement; it is concept material, never gameplay evidence. Extra side/back concept poses do not establish animation. Production preserves actual owners and all other sprites.

Native Farmer authoring uses editable Blender flat pixel quads and a16×24 transparent render. Approved Reedback ridge is copied byte-for-byte with a fresh full-frame importer; its original cropped study importer is retained. The old importer crop must not be copied. Runtime tile construction still owns only cached Sprite/Tile objects, not the texture.

Current pass: native optional actor binding and counter-checks; then ordinary isolated65 Farmer and64 Grazer replay, exact-subject examine, native visual review, push verification and safe storage audit. No game, package or device acceptance yet.

## Runtime findings and implementation checkpoint — 2026-10-24 20:24 UTC

The root recorded actual initial behavioral RED in
`Verification/Live16x24/incidental-actors-red.xml`:2/2 failed at
`NativeForeground == false` for factory Farmer and ReedbackGrazer through the
existing world API. The earlier zero-case import-race invocation is not RED.
These two cases failed before their resource assertions; no independent
missing-resource acceptance run is claimed.

`FlatPixelIncidentalActorCatalog` now owns two independent cached static
Tile/Sprite pairs while borrowing `Sprites/Live16x24/farmer` and
`Sprites/Live16x24/reedback-grazer`. The Greybox world and subject routes consult
it only inside existing native admission; old injected constructors remain
resource-free. Approved grazer bytes/canvas are unchanged. Both new identities
remain outside the four-actor motion roster.

Source sweep confirmed that both factory identities inherit owned Body, Brain
and Examinable; grazer additionally owns SpreadGrazer. New guards reject explicit
visual overrides, changed canonical glyph, foreign/missing structural parts,
custody and expanded footprints. They do not gate neutral bodies on health,
AI mood, trade stock, forage progress or generation roles. No generation, AI,
collision, cot occupancy, save graph or gameplay mutation is introduced.

The focused fixture is now39 cases, including the original two plus new
synthetic semantic/lifetime counters, real submitted Farmer→move→Bed restoration,
authorized reader/fog retirement and actual-resource/hash assertions. Those
additional37 were source-reviewed by
`/root/projection_cold_review/look_focus_draft_review` but were not independently
run RED; native compilation/GREEN remains root-owned and pending. Child review
found no fixture compile/API/setup blocker, including the injected Bed atlas
needed by the no-resource paint harness. Source inspection is not compilation.

Independent settled source review by
`/root/projection_cold_review/look_focus_draft_review` found no actionable P1/P2:
new identity recognition is disjoint from boundary/east owners, so the shared
`out art` routing cannot overwrite another accepted family's tile. Current-owner
world authority and separate reader-document authority remain intact; both new
bodies stay local and static. Optional-resource loading is once per companion;
owned Tile/Sprite disposal leaves textures and peer catalogs alive. This was
read-only source review, not an additional Unity run. Root native and ordinary
Play results remain separate acceptance gates.

Focused native checkpoint: root's explicitly refreshed
`Verification/Live16x24/incidental-actors-green2.xml` passes39/39 with zero skips
at20:27:37UTC, including actual Resources and approved grazer byte/hash binding.
The first postimplementation attempt still discovered the old two-case assembly
and is retained as `green1`, not accepted as implementation evidence. Root
verified the new catalog/expanded fixture before rerunning. The source review
above remains clear; affected regression and ordinary world/reader capture are
separate pending gates at this checkpoint. Runtime/tests remain frozen.

## Completed bounded acceptance — 2026-10-24 20:46 UTC

**This packet is accepted for two static local identities.** The broader graphics
release remains incomplete. The initial proposal/checkpoint sections above are
historical; this section supersedes their pending test/play status.

### Verification and implementation

Native RED was2/2 actual assertions before production. After an explicit import
and assembly readiness check, focused GREEN was39/39. Final affected suites
passed311 and277 **distinct** native Unity cases (588 total,0fail,0skip); the39
focused cases occur within311 and are not counted twice. The extra37 counter
cases were written after the initial RED; no separate RED claim for them. The
zero-case import race and stale two-case `green1` are not successful verification.
See the `incidental-actors-*.xml`/job/summary records in `Verification/Live16x24`.

Implementation is a cached optional companion, consulted after existing world
and document authority; there is no new renderer or gameplay mutation:

```csharp
// FlatPixelIncidentalActorCatalog.cs:49–58
if (owner.BlueprintName == "Farmer") {
    if (render.RenderString != "@") return false;
    tile = _farmer;
} else {
    if (render.RenderString != "g" || Owned<SpreadGrazerPart>(owner) == null)
        return false;
    tile = _grazer;
}
```

The preceding guards retain exact structural ownership/custody/override rules.
Native tests exercise submitted world tiles, actual Resources, authorized reader,
real MovementSystem Farmer→Bed repaint, fog and resource lifetime; controls also
cover peer catalogs, malformed optional assets and unchanged motion admission.
Two16×24 textures share the existing Tilemap path; each companion caches its
Sprite/Tile once. No new atlas or per-frame scan was needed for this pair.

### Ordinary controls and capture sequence

Only normal keyboard gameplay changed the world. The read-only capture helper
recorded owners/inventory and `ScreenCapture` frames. Seed selection and saved
checkpoint setup used owned NativeSaveIsolation. Presentation mode had no saved
key and took the ordinary default flat mode; neither launch forced art mode.
Temporary presentation zoom exposed exact1x/2x pixels and was restored to1.2.
All PNGs are real1280×800 Unity output, not the concept inset or a staged render.

| Real run | Actions and observed result | Receipt/capture prefix |
| --- | --- | --- |
| Seed65 ordinary Continue | Continued preserved kitchen save at54,0/tick1440; Down5 to54,5. Farmer7139 at53,6 over Bed7197 now has its own native body beside Cook7190. | `incidental-actors-65-continued`, `-farmer-1x`, `-farmer-2x` |
| Seed65 exact subject | Look Left/Down, cell menu→everything here→Farmer→Examine. Title, FOCUS and portrait agree on Nerrel Wethand, farmer; current Sitting is text, not an invented pose. | `incidental-actors-65-farmer-subject` |
| Seed65 persistence | Four ordinary movement turns, F5 then F6, player54,5/tick1530/HP40. Same Farmer/Bed owner rows and8inventory rows; two other actors moved during those turns. | `incidental-actors-65-saved-loaded` |
| Seed64 fresh New | Ordinary Right40 crosses glade into east at0,12/tick410; Right45 approaches actual Grazer5036 at51,17. Native body replaces its canonical `g`. | `incidental-actors-64-east-entry`, `-grazer-1x`, `-grazer-2x` |
| Seed64 subject and flight | Look6right/5down→Examine shows matching approved low quadruped. Walk9right/4down to54,16; animal stays inside closed pen. Look gate53,17→Open advances990→1000 and real Grazer moves51,17→50,18. | `incidental-actors-64-grazer-subject`, `-grazer-approach`, `-gate-open` |
| Seed64 persistence | Escape, F5 then F6. All300recorded owner rows match byID,6inventory rows match; same tick1000/HP40 and moved Grazer body retained. | `incidental-actors-64-saved-loaded` |

`incidental-actors-65-farmer-examine.png` is an **aggregate Fieldnotes** view from
the initial cell menu, despite the historical filename. It is not the accepted
Farmer portrait; `-farmer-subject.png` is. The Farmer did not autonomously leave
the cot during this replay. Bed restoration is proved by the native movement
case, not falsely reported as ordinary movement evidence. The Grazer's single
actual escape step is static-body movement, not a walk animation or forage loop.

Both Play sessions stopped and restored scene/view/seed/presentation preferences.
Cleanup receipts confirm isolation=false, saveRoot=null and SampleScene active.
Native final console counts:0errors,2existing Metal depth warnings,95logs. The
connector mislabeled a normal Autosaved Debug.Log as Exception and the warnings
as errors; raw connector output and native counts are both retained. This is not
an empty-console claim.

### Visual review and performance bounds

Independent runtime/production source reviews found no actionable P1/P2. The
source-art author separately reviewed actual1x/2x/world/reader captures and found
no new identity/framing defect; this is disclosed as source-author visual review,
not blind second-artist approval. The Farmer is distinguishable from the Cook
and Player; approved Grazer keeps its full canvas, low silhouette and exact hash.
23preexisting approved art files remain unchanged.18Unity importer whitespace
changes and an ID-only test-generated census were backed up and restored; the two
unrelated notice PNG variants remain untouched and excluded from staging.

The206.37second editor sample includes UI/idle/ordinary movement and F6. Its
167003frames exceeded the131072sample ring; statistics cover only the retained
last131072. Renderer mean0.05170ms/p99 0.10325/max32.49; main-thread mean1.245ms/
p99 2.218/max1109.87(including load); maximum43draws. This is neither full-window
statistics nor before/after speedup, combat load or physical Deck evidence.

**Can verify:** native semantic/visibility/custody/lifetime assertions; the
pictured ordinary identities/readers; actual Grazer movement; bounded persistence;
editable source/export integrity; local editor frame measurements.

**Cannot verify from this packet:** all owners/seeds/states, sitting/walk animation,
full-scene aesthetic approval, physical controller/Deck feel, packaged activation
or performance, complete six-zone release. No new build was made.

### In-phase self-review and scope divergences

- 🔵 Fixed the two actual gaps: absent Farmer native identity and unconnected
  approved Grazer. No AI, generation, collision, faction, forage or cot rule changed.
- 🔵 Corrected source/capture assumptions: study importer crops Grazer to16×14;
  runtime instead uses full16×24. Fresh-run Grazer is5036, not prior5033. Continued
  seed65 starts54,0/tick1440, not the older screenshot's50,8/tick1310.
- 🔵 Rejected generated concept placement and extra poses. One new Farmer frame,
  one unchanged approved Grazer; all accepted peers preserved.
- 🧪 Ordinary Farmer move-away remains unobserved; native MovementSystem repaint
  covers it. Standing-shaped neutral body does not encode the Sitting condition.
- 🧪 Existing aggregate Look heading and intermittent upper-right court selection
  mark remain separate named observations; neither was concealed with new art.
- 🧪 Cindercaller and wider regional/bestiary gaps remain. Scope stays within the
  six admitted local graphs; global truthful flat fallback is unchanged.
- ⚪ No separate atlas or two-actor animation was introduced. Cached16×24 resources
  suffice for this bounded pair; full motion/release gates remain explicit.

Files: new runtime catalog/meta, new39-case fixture/meta, two Resource PNG/meta
pairs, editable Blender/source-cells/manifest/prompt/concept/verification under
`ArtSource/Live16x24/incidental-actors`, authoring script
`Tools/Live16x24/build_incidental_actors.py`; modified Greybox routing; named
capture/test/review/preservation records, this living doc, continuation docs and
coverage ledger. `GRAPHICS-G3-KITCHEN-SERVICE.md` is a source-checked next plan only.

**Next:** execute that ordinary ingredient→repair→paid-batch→pickup→meal-reader
route. Capture genuine states before deciding what additional kitchen feedback
is needed. Preserve this accepted art. Continue remaining G2 motion/defence,
missing identities, whole journey, package/device and regional gates.
