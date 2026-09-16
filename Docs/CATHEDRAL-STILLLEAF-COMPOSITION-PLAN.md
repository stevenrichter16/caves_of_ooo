# Deepest Cathedral and Stillleaf — native voxel composition

Status: baseline verified; initial rendering RED captured; native/art specifications in progress.

## Purpose and scope

Extend the established native composition system to two distinct lore places:
the Deepest Cathedral at `Overworld.5.4.0` through `.2`, and Stillleaf at
`Overworld.2.4.0` through `.2`. These six fresh-generated chunks form two
legible journeys. Native cells, entities, stairs and interactions remain the
authority; voxel meshes express them rather than introducing visual collision.

**Deepest Cathedral:** a green pilgrimage mouth, descending stone shelves with
expedition supplies, then a grown memory nave with broad fungal ribs, side
alcoves, encased conversational elders and the native Choir node. Pale organic
structure, muted violet memory threads and dark floors distinguish this from
Ginmere's watery basin. Large grouped silhouettes and open processional space
replace uniform prop scatter. The cathedral's native trader remains a creature.

**Stillleaf:** a wind-cut Stump approach, austere archive descent and an intact
sealed three-material enclosure inside an eroded outer chamber. Tepuibone,
memory marble and Choir iron have visibly distinct coarse silhouettes and
restrained color pairs. The route bends around the seal; the interior remains
deliberately inaccessible while locked. Sealed shelves show bundles and clay
seals, not invented loot containers. Quiet forecourts and heavy thresholds
communicate absence and exclusion.

The existing camera, 1.2x view, full reveal and spawn stay configured as they
are. Existing saved zones are not regenerated; no migrations are requested.
Depth 3+ and all other named places retain their existing pipelines.

## Verification sweep and corrections before production

| Verified source | Correction / implementation consequence |
|---|---|
| `SinkholeSites`, `SinkholeArchetypes`, `WorldMapAuthoring` | Cathedral is `(5,4)`, Stillleaf `(2,4)`; Olderdeep is `(4,6)`. Stillleaf is profile-authoritative, including renamed POIs; exact finite scope also checks current map identity. |
| `Docs/FELLING-WORLD-DESIGN.md` §4.1, `Lore/Factions/01_RotChoir.md`, `Lore/10_Bible.md` | Grown memory architecture is canon. Wedded/Selen audience and Wall-Catching travel are future work, not implemented by today's ChoirNode. Do not manufacture those mechanics or a god encounter. |
| `ChoirCathedralBuilder`, native blueprints | Vaults and node are solid native terrain; elders have conversation, tendrils have real trader/conversation/combat Parts. Node supplies light but has no travel Part. Existing vaults are not destructible; preserve that native exception rather than claiming universal destructibility. |
| `OverworldZoneManager` | Cathedral stamp priority 3100 precedes stairs 3500 and connector 3600. Sealed library priority 3650 follows them. Existing parent-first generation already handles lower-first travel; verify without duplicating it. |
| `SealedLibraryBuilder`, `SealedLibraryBarrierPart`, `Docs/FELLING-W6-PLAN.md` W6.6 | The archive chooses a stair-free enclosure, builds exterior approaches and reserves its aisle. Never repair connectivity through the sealed interior. Walls, door and shelves are intentionally indestructible; floor excludes zone arrival. No quest key currently ships. |
| `Objects.json`: archive and Choir definitions | Archive shelves are examinable fixed objects, not Container/Readable content. Door has a native Lock with key ID `coo.sealed-library.stillleaf`; rendered door state must follow the actual lock/physics state. Synthetic test keys are not new game content. |
| `FormationReachability`, `SinkholeMouthBuilder`, `SinkholeDescentBuilder` | Mouths and ledges use native walkable terrain and stairs, not simulated vertical falling/climbing. Do not create an apparent abyss where cells remain ordinary walkable ground. |
| Existing area kits, recipes, `AreaCompositionScope` | Reuse combined mesh assets, exact native owner reconciliation and weak zone-local map authority. Four shape variants per repeated family, at most two swatches per model. No per-voxel GameObjects, art colliders or saved renderer state. |

## Milestones and verification gates

1. Capture untouched mixed-file snapshots and a full-suite baseline in a
   persistent isolated project. Preserve the user's running Unity editor.
2. Write and run actual REDs for pure plans, native realization, scope and
   model families before corresponding production changes.
3. Implement separate deterministic semantic plans and staged native builders.
   Retain existing floor stamps, native populations, stairs and sealed barriers.
   Preflight exact required Parts before mutation; reject malformed content.
4. Author both reproducible voxel kits and integrate current-owner recipes.
   Reuse shared terrain/fauna art where appropriate; give defining architecture
   and inhabitants their own strong silhouettes. Preserve portable body identity.
5. Exercise final generated chunks across seeds, stair reciprocity/lower-first
   access, interaction frontage, library exterior and seal, actual owner removal,
   native light/material/trade contracts and runtime POI countercontrols.
6. Render native manager-generated previews at the gameplay camera. Review
   composition, scale, color restraint and every visible owner's model coverage;
   correct generation rules rather than manually arranging demonstration chunks.
7. Independent cold-eye review, separate adversarial tests and player-flow
   hypotheses; fix material findings before close-out. Full regression compares
   exact failures against this turn's baseline. Audit source/art and metadata.
8. Update living docs and commit only task-owned files and clean shared deltas.
   Record exact patches for already-mixed integration files without absorbing
   unrelated pre-existing changes.

## Readiness, performance and honesty bounds

🟢 Lore, exact map addresses, floor stamps and native owner contracts verified.
🟢 Plans, builders, 72 model variants, native routing and previews pass their RED → GREEN gates.
🟢 SC17 full regression: 12,825 passes, exactly 32 unchanged baseline failures, zero C# errors and no new failures.
⚪ CoO-original generation/art; no Qud code-parity claim. No new god audience,
travel network, archive key/quest, reading system or save migration.
🧪 Native headless renders establish static composition and model coverage;
they cannot establish live input feel or sustained gameplay FPS.

Plans run once at generation. Presentation uses cached area/family checks and
existing dirty-cell reconciliation/static batching. Preview receipts report
generation time, native census and missing sources without claiming live FPS.

## Implementation log (newest at bottom)

- 2026-09-15: Selected the two contrasting memory architectures after independent
  lore and native-pipeline reviews. Captured ten anticipated mixed integration
  files and complete initial working-tree status before changes. SC00 baseline
  runs in `/Users/steven/.cache/caves-of-ooo-validation/sanctums/project`.

- SC00: 12,570 total / 12,538 passing / 32 existing failures / zero C# errors, matching the preceding feature baseline. SC01: actual rendering RED, 25 failing cases and six unchanged-scope controls passing; zero C# errors.

- SC02/SC03: actual missing-type REDs captured for both native plans/builders and both voxel libraries before implementation. Initial art contract is 52 models across thirteen families, four shape variants each; shared Ginmere/Stump assets cover native cliffs, ledges and supplies. SC04 will independently exercise the added Stillleaf descent and elder-facing specifications before their bounded implementation.

- SC04: both original kits exported successfully, zero C# errors. SC05:
  354/366 targeted cases passed. Native-owner enumeration exposed CaveBear,
  CaveSlime, ConvalescencePool and IronshodBoots without models; the Stillleaf
  kit gains sixteen models for those actual owners. Elders' north-facing pose
  and pending Stillleaf shelves failed their specifications as intended.
- SC06: 105/107 native cases passed; the two remaining failures proved detached
  grown buttresses and a real non-villager trader restock exclusion. Joined
  native rib rules and a narrow explicit-Trader eligibility fix followed, with
  forged-shop, legacy-villager and timing/factory/currency countercontrols.
- SC07: eighteen manager-created native renders across six chunks and three
  seeds; zero missing source meshes and zero unmodeled visible owners.
  Static review identifies the shared Stump wall grooves, empty Stillleaf lip
  and overly similar descent rhythms for refinement of generators and kits.
- SC08: 506/514 expanded cases pass, including the merchant and all native
  navigation/seal checks. Four actual carried-key/bone variant failures and
  four over-broad bear muzzle assertions drive bounded corrections. Live input
  feel and sustained performance remain outside this headless evidence.

- SC09: 503/527 expanded cases pass; zero C# errors. All four carried-key/bone
  cases are now green. Actual refinement REDs confirm the bare Stillleaf lip,
  equal shelf rhythm, empty outer archive and missing-Bush preflight; the art
  REDs confirm busy pink walls, occluded elder faces and an undersized node.
  Bear assets still require rebuilding from their corrected source. Refinement
  remains in pure generation rules and reusable meshes, never manual scene edits.

- SC10: both kits rebuilt to 72 models; eighteen refined native manager renders
  complete with zero missing meshes and zero unmodeled visible owners. Original
  project receives the exact 294 generated asset/metadata files; task GUID audit
  finds no collision across 5,732 metadata files.
- SC11: 527/527 targeted cases GREEN, zero C# errors. Static review confirms the
  distinct Stillleaf colony/shelf/bay rules and quieter wall forms. A subsequent
  cross-feature review finds that a finished hit gesture can leave an unrigged
  elder facing into its wall; explicit damage/expiry player-flow cases precede
  the final bounded presentation correction.
- SC12: four actual elder hit-expiry failures, 32 controls passing, zero C#
  errors. Native damage and forced movement run before expiry through either
  ordinary frame update or an empty-dirty refresh; ordinary animated players
  retain their own native facing in all four countercontrols.
- The correction caches whether a view has the explicit elder rest pose, then
  restores its current authored quarter-turn in the shared Idle path before
  the no-Animator early return. It neither changes native facing nor replaces
  an entity or model when a gesture finishes.
- SC13: rebuilding both kits reproduces all 294 asset/metadata files byte for
  byte, including GUIDs. SC14: **537/537 focused tests pass**, zero C# errors.
- SC15 full regression: 12,857 total / 12,824 pass / 33 fail / zero C# errors.
  All 287 new cases pass. The exact 32 baseline failures retain their messages;
  the sole additional failure is an old `VoxelWorldIntegrationTests` exclusion
  for Stillleaf's now-supported surface. It moves to the unsupported depth 3,
  retaining all negative assertions. SC17 reruns the full suite after that
  test-only contract correction; production source is unchanged from SC15.

## Cold-eye review and delivered changes

| Finding | Resolution and evidence |
|---|---|
| 🟡 Detached grown Cathedral ribs | Join additions to the native vault stamp; SC06 RED, SC14 native connectivity/frontage GREEN. |
| 🟡 Choir elders could trade initially but never restock | Explicit native Trader eligibility now joins the existing Villagers rule. Real stock, currency, factory and strict time gates remain required; forged/empty trader and legacy controls pass. |
| 🟡 Four real population/loot identities lacked models | Cave bear, cave slime, convalescence pool and ironshod boots receive sixteen own models after the native census RED. |
| 🟡 Carried Stillleaf keys/bones changed shape after movement | Portable variants now derive from the owner, independent of cell and depth. All four observed SC08 failures pass. |
| 🟡 Repetitive Stillleaf composition and busy cliff caps | Three native plant colonies, three unequal attached shelves, two outer stone bays and four quiet pink-wall variants; actual SC09 RED → SC14 GREEN plus nine refined Stillleaf views. |
| 🟡 Vault height hid elders; Choir Node lacked visual weight | Cutaway vaults expose heads and upper casings; the node has a broader, taller crown. Southern elder faces correctly point away from the fixed camera. |
| 🟡 Hit expiry left encased elders facing into walls | SC12 native damage/expiry RED → SC14 GREEN with both refresh paths and ordinary-player facing controls. |
| 🔵 Reused Bush forms still show cell rows | Native contiguous colonies work; a later foliage art pass can soften their cultivated appearance without adding gameplay objects. |
| ⚪ Archive seal and some grown architecture resist destruction | Preserve the explicit native indestructible exceptions. No invented key, readable archive content, divine audience or climb network. |
| 🧪 Live input, animation feel and sustained FPS | Static camera renders and headless native flow tests do not establish these. The running user editor was preserved. |

The final shared-source review also checked the two pipeline orders, exact POI
and depth gates, restored-world authority, native owner removal and model
fallback. The sealed library still stamps after stairs, while the Cathedral
stamp precedes stairs. Neither renderer recreates removed terrain or opens a
seal. These are CoO-original composition/art rules, not a Qud parity claim.

The reusable native preview entry point is **Caves of Ooo → Composition →
Render Cathedral and Stillleaf previews**. The checked 18-image gallery is
`Docs/Verification/VoxelWorld/SC10-refined-preview/index.html`; regenerate its
viewer with `python3 Tools/sanctum_gallery.py <preview-directory>`.

## Closed-out result

SC17: **12,857 total / 12,825 passing / 32 unchanged baseline failures / zero
C# errors**. All 287 added cases pass. The comparison checks exact failure
names and messages, not just totals. SC14's 537 focused checks all pass.

Six native levels and 72 coarse voxel model variants are installed. SC10
contains eighteen actual manager-generated views with no missing mesh sources
or unmodeled visible owners. Rebuilding reproduces all 294 artifact/metadata
files byte for byte. Final metadata audit: 165 task metadata files against
5,735 project metadata files, no task GUID collision. All 47 final source/input
files match the isolated project used for SC17.

`SC16-closeout/implementation.patch` records only this phase's shared-source
changes against captured starting contents, and passes reverse-apply checking.
New phase-owned files and the three initially clean shared files are committed
normally. The eight already-mixed integration files remain installed in the
workspace without absorbing unrelated prior work into this commit. No blueprint
JSON, scene, camera, spawn or saved graph was rewritten by this phase. Layouts
apply on fresh generation; current native owners continue to control their art.

The next user-authorized pair is a separate development phase after this
close-out. This document closes only the Cathedral and Stillleaf pair.
