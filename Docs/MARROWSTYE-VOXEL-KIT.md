# Marrowstye voxel kit

Status: complete and installed, 24 combined-mesh voxel variants. WI13 passes all99 imported-art checks within the311-case focused run. WI15 reproduces all214 combined asset/metadata files byte for byte. All six WI10 native captures have zero missing meshes and zero unmodeled owners.

CoO-original art with at most two swatches and240 vertices per model. [Complete contracts and verification](DROWNED-LEDGER-MARROWSTYE-COMPOSITION-PLAN.md).

## Verified native identity

| Source | Verified contract | Art constraint |
|---|---|---|
| Current WorldMapAuthoring / historical geography | Marrowstye is currently Spread tier 1 with a road, no river. Historical prose calls it tier 2. | Local quiet dry intake identity; preserve current balance and ordinary town scope. |
| CurationIntake / proposed native plan | Preserve two StoneCoffer and one FilerClerk; add two existing ordinary SaltCuredBody owners as intake cargo. | Distinct heavy box, cured human and conversational actor models. No deeper PaleCurator, Catcher procedure or new lore revelation. |
| StoneCoffer / HaulableContentTests | Solid weight 110, Handling, not carryable/throwable; haulable at Strength 14. No Container or Destructible Part. | Broad stone block and lid, not a chest with hardware or an opening animation. Existing ring coffer has one beveled 180-triangle model; replace locally with four quiet coarse variants. |
| SaltCuredBody / HaulableContentTests | Solid ordinary body, weight 90, Handling; haulable at Strength 12 but not inventory-carryable/throwable. No corpse or courier-item identity. | Mineral-pale human with head and separate limbs; no sealed packaging or ancient-witness tag. |
| FilerClerk / FilerClerk_1 | Canonical `@` conversational actor, not a trader. Accepts the carried SealedBogTakenBody only with the active courier quest, then consumes it and completes native rewards once. | Held ledger and raised stamp, no attached desk and no automatic delivery effect. |
| WI08 native camera review | The pale intake/cargo is legible, but four detached rectangles surround an undifferentiated broad forecourt. | Native agent adds receiving routes, bay partitions, limited edge vegetation and a real disused breach; new RoadStone art is a quiet continuous 106 plane against ground 64. Existing 20 models stay unchanged. |
| Floor / StoneFloor / SandstoneWall | New native outdoor Floor, existing StoneFloor interiors and real sandstone walls. | Full-cell muted earth, quiet low cutaway masonry. Do not use special Sealed Library material identities or fake salt-curing mechanics. |

## Library contract and art direction

`MarrowstyeVoxelKitLibrary.ResourcePath = "MarrowstyeVoxel3D/Library"`.
The strict `Load`/`Find`/`Validate`/`Family`/`ModelId` contract matches other native
kits. Unknown aliases return null; malformed families/variants throw instead of
borrowing a plausible but incorrect model.

| Ordered family / offset | Exact alias | Palette and silhouette |
|---|---|---|
| ground / 0 | Floor | Muted packed-stone 64; one full-cell top at zero, buried thickness variation only. |
| wall / 4 | SandstoneWall | Two full-cell strata 13/35, constant cutaway height 1.05; quiet pale masonry. |
| coffer / 8 | StoneCoffer | Dark stone 106 / broad pale lid 35, three boxes, height .66. |
| cured / 12 | SaltCuredBody | Pale mineral body 95 / darker extremities and crust 106, distinct head and separated feet, height .35. |
| clerk / 16 | FilerClerk | Gray clothing/stamp 13 / face and held paper 19, standing figure facing +Z. |
| path / 20 | RoadStone | Quiet stone 106, a continuous full-cell top distinct from muted forecourt 64; buried variation only. |

IDs are `marrowstye-{family}-{0…3}`, 24 entries. Ground and path have metadata kind
`ground`; all other entries are native entity models. StoneFloor interiors and
common village furniture/residents/services reuse established kits.
The sealed courier parcel uses the distinct Drowned Ledger parcel family in
both areas. No body blueprint silently aliases a different body category.

Haulable coffers and cured bodies are movable even though Takeable and Creature
are false. Parent presentation must retain their owner-based variant and update
old/new membership as the native haul system moves them. The art library adds no
Handling, container, collision, destruction or quest behavior.

## Offline generation and limits

Call `CavesOfOoo.Editor.MarrowstyeVoxelKitBuilder.Run()` in the isolated editor.
Assets rebuild in place under `Assets/Resources/MarrowstyeVoxel3D`, retaining
GUIDs, then validate before saving. No scene is authored by this builder.

One combined readable mesh and one shared-material identity-transform prefab
per variant; maximum two swatches, ten boxes / 240 vertices, one-cell X/Z bounds.
No colliders, MonoBehaviours, Lights, rigs, clips or sockets. Existing runtime
batching borrows these coarse meshes directly. No per-frame/turn loop is added.

## Verification and cold-eye bounds

- Actual WI01 census preceded the exact family budget. Native Floor alias matches
  the new plan; unsupported StoneFloor, Chest, StrongBox, PaleCurator and foreign
  body identities have explicit negative alias controls.
- Contract and anatomy tests were written before production. Root captured
  WI02's actual missing-library RED before geometry/library implementation.
- Tests now pin 24 complete entries, four distinct variants, full-cell quiet floor,
  two-color/bounds/budget limits and prefab/material/metadata consistency. Malformed
  copies must fail without damaging source assets; extra Lights are rejected.
- Ray tests contrast a filled stone coffer with a human silhouette, verify a real
  head and separate feet, and distinguish a clerk's legs from an attached desk.
  Raised-stamp/held-ledger tests preserve role readability. Wall tests require
  continuous full-cell strata and a low profile with open space above.
- Current paired source audit: 52 models, at most 240 vertices/two swatches, valid one-cell
  bounds, six collision-free copied source metas and zero findings.
- WI05 exported the 44 pair models with zero C# errors and exit 0. WI06 passed
  all 85 imported-art cases; root retained the 182 generated asset/metadata files
  as a rebuild comparison baseline. WI10 exported all 52 refined pair models
  with zero C# errors and six native views.
- Independent WI10 inspection of both areas at seeds 64 and 1729 accepted the
  two intake bays, separate clerk frontage, quiet receiving paths, limited edge
  plant groups and disused breach. Cargo remains visible and circulation reads
  more clearly. No further material defect was found.
- Independent generated-byte check confirms 214 final asset/metadata files. Of
  182 original files, 176 stayed byte-identical; only the four Ledger preserved
  mesh UV updates and two expanded libraries changed. Every original prefab/GUID
  and all 20 original Marrowstye model assets remained unchanged.
- 🧪 One-cell faces, hand tools and cargo detail remain small at full-chunk
  framing. Static screenshots cannot prove input feel, animation quality or
  sustained FPS; imported-art and byte-audit results are separate claims.
- ⚪ Ordinary preservation remains descriptive. No curing station, fee system,
  cooling, Catcher procedure, deeper curator shop or new faction reward is added.

## Implementation log

- 2026-09-15: Read CLAUDE.md, paired plan, current map/Bible, native intake stamp,
  exact blueprints, Handling/haul tests and courier conversation. Distinguished
  coffers from containers and hauling from inventory carrying before production.
- 2026-09-15: Authored RED contracts and geometry assertions; root confirmed WI02
  missing types. Wrote five four-variant families, strict validation and copied
  source metas. Paired source audit passes; export/integration remain parent-owned.

- 2026-09-15: WI05 export succeeded; WI06 imported-art contracts passed 85/85
  with zero C# errors. No art repair was needed after the first actual export.
  Root owns subsequent native preview and integration/full-suite verification.
- 2026-09-15: Independently inspected WI08 previews at seeds 64 and 1729.
  Recommended an articulated receiving apron and grouped edge vegetation while
  preserving wide haul aisles. Added new RoadStone family/count/quiet-surface
  tests before source; planned Marrowstye count is 24, production still 20 until
  actual RED. Root/native agents own the receiving routes and bay partitions.

- 2026-09-15: WI09 captured missing path-family/count/alias RED before source.
  Appended four RoadStone variants with quiet 106 tops and ground-kind metadata.
  Existing 20 models are unchanged. Constant-surface, exact-alias and outside-cell
  counterchecks protect mapped receiving routes; the 52-model source audit passes.

- 2026-09-15: WI10 exported 52 paired models and six native views with zero C#
  errors. Independent four-image review accepts the targeted spatial/readability
  refinements. Actual hash comparison confirms all original Marrowstye model
  assets unchanged; the final test receipts remain parent-owned.

Files: MarrowstyeVoxelKitLibrary.cs, MarrowstyeVoxelKitBuilder.cs,
MarrowstyeVoxelKitTests.cs, their three copied metas, this document and resources
exported by the parent. Native/shared integration source is outside this kit.
