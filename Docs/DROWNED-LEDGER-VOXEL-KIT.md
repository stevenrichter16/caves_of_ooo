# Drowned Ledger voxel kit

Status: complete and installed, 28 combined-mesh voxel variants. WI13 passes all99 imported-art checks within the311-case focused run. WI15 reproduces all214 combined asset/metadata files byte for byte. All six WI10 native captures have zero missing meshes and zero unmodeled owners.

CoO-original art with at most two swatches and240 vertices per model. [Complete contracts and verification](DROWNED-LEDGER-MARROWSTYE-COMPOSITION-PLAN.md).

## Verification sweep before production

| Source | Verified native contract | Art consequence |
|---|---|---|
| WorldMapAuthoring / current Bible | Ledger is currently Sodden tier 2, with no mapped road or river. The faction's current public name is Recension; its internal tag remains Palimpsest. | Borrow appropriate existing bog terrain; do not alter balance or rename native faction IDs. |
| LandmarkBuilder.ExcavationCamp | Exactly three PreFellingBody owners, three stakes, one table, one scribe and one sorter. The stamp comment says a body is on the table, but actual `R` and `b` cells are adjacent. | The table has a genuinely bare central surface. No fourth body is hidden in its mesh, and native owners are never merged. |
| PreFellingBody | Complete preserved human, non-solid and non-takeable, examination only. No Handling, Destructible or body-reading Part. | Intact low prone figure with a small tag; no ordinary bog-body alias, exposed wounds, movement, or magical reading effect. |
| ReadingTable / SurveyStake | Solid examination table, non-solid examination stake. No container, reading, excavation or surveying verb. | Straps, pads and folio stand belong to the table's silhouette; numbering remains in the native description. |
| RecensionScribe / CurationSorter | Separate native conversational creatures, each canonical `@`, with their own loadouts and factions. Neither is a trader. | Teal reader with open folio versus pale gloved sorter with tucked ledger; no attached furniture or invented equipment actions. |
| SealedBogTakenBody / BodyCourier | Separate takeable weight-30 NoTrade parcel. Sorter grants it through the existing quest; clerk consumes the real carried item under native delivery gates. | A fully wrapped and corded portable model, stable by owner in both towns. It is neither an ancient witness nor ordinary cured cargo. |
| WI08 first native camera review | Preserved clothing and StoneFloor both use slot 12, hiding torso/limbs; the inherited yellow board route dominates the camp. Outdoor cut-bank placement also partially hides two witnesses. | Correction after WI09 RED: clothing 13 with head/tag 79; low-chroma 64/106 boards. Native generation owns shorter destination branches and witness-side bank clearance. |
| Native water | Real WaterPuddle owners project water coatings through Zone.ProjectPool; the last owner's removal clears them through UnprojectPool. | Reused water art follows existing owners and never writes liquid or coating state. |

## Exact API and visual families

`DrownedLedgerVoxelKitLibrary.ResourcePath = "DrownedLedgerVoxel3D/Library"`.
`Load`, `Find`, `Validate`, `Family`, and `ModelId` follow the established strict
kit contract. Exact aliases only; foreign owners return null and unknown family
or out-of-range variant inputs throw. IDs are `drownedledger-{family}-{0…3}`.

| Ordered family / offset | Exact alias | Palette and shape |
|---|---|---|
| preserved / 0 | PreFellingBody | Muted clothing 13 / intact head and tag 79. Long Z axis, distinct head and separated feet, maximum height .25. |
| stake / 4 | SurveyStake | Wood 8 / worn gray paint 13. Narrow post, broad readable painted head, no invented text. |
| table / 8 | ReadingTable | Support/straps 8 / scrubbed surface and pads 19. Four legs, bare center, small folio stand at an edge. |
| scribe / 12 | RecensionScribe | Teal 22 / face and open paper 19. Held folio, separate legs, face +Z. |
| sorter / 16 | CurationSorter | Pale coat/gloves 35 / dark boots and ledger 12. Separate standing actor, face +Z. |
| parcel / 20 | SealedBogTakenBody | Dark waxed wrapping 12 / broad cord and seal 19. Closed lumpy bundle, no exposed face, maximum height .3325. |
| boards / 24 | Duckboard | Weathered broad planks 64 / lower sleepers 106. Three planks with real gaps, maximum height .17, dry native access only. |

All 28 entries have metadata kind `entity`. Terrain and canvas reuse appropriate
Sodden ground/peat/snags, Spread reeds, Sumphold native water and Wellmeet
canvas/corner/furniture assets. Source aliases do not claim those terrain owners.
The parent integration owns exact area/profile scope, canonical actor glyphs,
stable moving-owner variants, palette materials and live removal/movement.

## Reproduction and performance

Run `CavesOfOoo.Editor.DrownedLedgerVoxelKitBuilder.Run()` in the isolated editor.
It creates or updates meshes, prefabs and Library.asset under
`Assets/Resources/DrownedLedgerVoxel3D`, preserving existing GUIDs on rebuild.
It never writes a scene or manually positions a demonstration settlement.

Every model is a single readable combined mesh with the existing ring material,
identity transform, at most two swatches and ten boxes / 240 vertices, with X/Z
bounds within one cell. No colliders, MonoBehaviours, Lights, rigs, clips or
sockets. Geometry is already coarse and should be borrowed without a second
voxel-conversion pass. This kit adds no per-frame or per-turn work.

## Tests and self-review

- WI01's actual three-seed census and native blueprint/conversation/haul sources
  preceded the final 24-model contract.
- Tests were saved before production. Root captured WI02 missing types for both
  new libraries (78 C# error lines); stale XML was not accepted as a test result.
- Tests require four distinct shapes per family, exact aliases with foreign-body
  countercontrols, complete asset references, palette/vertex/bounds budgets,
  strict ModelId inputs, and corruption rejection including extra Lights.
- Anatomy rays require a real head and two separated feet, with empty space
  between them; the parcel instead closes its wrapping across its foot end.
  Table tests require a bare center and distinct supports, not an attached body
  or filled stone box. Actor tests require held records and separate legs.
- Current source-only audit of both new kits: 52 models, maximum 240 vertices, maximum
  two swatches, one-cell horizontal bounds, six unique copied source metas and
  zero findings. This is not generated-asset or camera proof.
- WI05 exported all 44 pair models with exit 0 and zero C# errors. WI06 passed
  all 85 imported-art cases; root captured the first 182 asset/metadata files
  before that gate for rebuild comparison. WI10 subsequently exported 52 models
  with zero C# errors and rendered six complete native views.
- Independent WI10 review inspected both areas at seeds 64 and 1729. The indoor
  witness outline now contrasts with the floor, and the two outdoor witnesses
  are visible through the bank breaks. Short gray board branches and lower camp
  walls improve the functional hierarchy. No further material defect was found.
- Independent rebuilt-byte audit: 214 final asset/metadata files, including
  directory metas. Of the original 182 files, 176 are byte-identical; only the
  four preserved mesh assets and two expanded Library.asset files changed. All
  original prefabs/GUIDs and the other 40 model assets remain unchanged.
- 🧪 One-cell faces, tools and body details remain small at full-chunk framing.
  Static previews cannot establish input feel, animation quality or sustained
  FPS. These claims remain separate from the imported-art and byte-audit results.
- ⚪ Native description and conversation attribution remain authoritative. Art
  supplies no memory revelation, preservation procedure or destruction policy.

## Implementation log

- 2026-09-15: Read CLAUDE.md, paired plan, Bible, current map, exact native profile,
  body/table/NPC blueprints and courier conversations. Corrected the adjacent
  table/body premise and distinguished all three body identities before code.
- 2026-09-15: Authored contract and anatomy tests; root captured WI02 actual
  missing-library RED. Created the strict library and offline six-family source.
  The 44-model paired source audit passes; root owns export and integration.

- 2026-09-15: WI05 export succeeded; WI06 imported-art contracts passed 85/85
  with zero C# errors. No art repair was needed after the first actual export.
  Root owns subsequent native preview and integration/full-suite verification.
- 2026-09-15: Independently inspected WI08 Ledger and Marrowstye at seeds 64 and
  1729. Found the shared-floor camouflage and overly bright, long board spine.
  Added sampled-palette contrast and new quiet-board family tests before any
  refinement source. Planned Ledger count is 28; production remains 24 until RED.
  Root/native agents own bay staggering, clearance and shorter board routes.

- 2026-09-15: WI09 captured expected new board/count and witness-contrast RED
  before refinement. Appended four five-box board variants and changed only
  preserved clothing 12→13; every original non-preserved method is unchanged.
  Sampled-palette tests require the whole body silhouette to contrast with its
  actual interior floor, and require muted plank color plus real support/gap
  countercontrols. Paired source audit now covers 52 models with zero findings.

- 2026-09-15: WI10 rebuilt 52 paired models and six native previews with zero C#
  errors. Independently reviewed Ledger/Marrowstye at seeds 64 and 1729 and
  accepted the targeted improvements within static-camera bounds. Independent
  asset hashing confirms exactly the six intended changed old files; final tests
  remain parent-owned.

Files: DrownedLedgerVoxelKitLibrary.cs, DrownedLedgerVoxelKitBuilder.cs,
DrownedLedgerVoxelKitTests.cs, their three copied metas, this document and the
parent-exported resources. No native/shared integration source is edited here.
