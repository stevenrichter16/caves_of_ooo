# Native direction audit draft

Status: review-only drafts outside Assets, 2026-09-11. No Unity run, installation, native image claim or gameplay change was performed by this subtask. Root/pipeline own the RED, GREEN and native runs. This is CoO-specific verification infrastructure; no Qud parity claim.

## Scope and source verification

The existing `StarterSpell3DNativeAudit.CommandCase` routes `(1,0)` at `Assets/Scripts/Scenarios/Custom/StarterSpell3DNativeAudit.cs:179`, and its dummy reset uses an east-only offset at `:321`. The original seven showcases, actual keyboard Flaming Hands cast, empty Rain, rejected Rime and peaceful Calm controls all remain. `KeyboardCast`, `ProfilePair` and the classic `FindLane` method bodies are byte-for-byte unchanged in the draft. The four measured 20-second profile phases run before any new directional work.

After those phases, the draft adds Ember Spit, Jet Blast, Ground Surge, Rime Grip and Calm in north `(0,-1)`, south `(0,1)` and northeast `(1,-1)` directions. These are 15 labelled real `SkillsPart.TryRouteSkillCommand` cases in the authored south chunk. `CommandCase` still observes the copied queue synchronously without draining it; the real world coordinator remains the sole consumer. No editor `execute_code` or synthetic playback substitutes for a spell command.

| Verified issue | Source / evidence | Draft consequence |
|---|---|---|
| Simulation north is negative Y, native ground is XZ | `Village3DProjection.cs:7–30` | Keep existing projection and camera; record dx/dy and assert source/contact/final cell projection round trips. |
| Jet's side cells widen perpendicular to facing, including diagonals | `SpellTargeting.cs:267–313` | Require a complete ray with a one-cell margin. Reject unrelated elemental recipients through `Cell.Occupants`, not canonical anchors alone. |
| AffectedCells is broader than cone geometry | `SpellFxSequence.cs:253–274`; actual `SSN-f83f27a1475f4e11866f7dde09c33331-native.json` Jet showcase: four cone cells plus `43,8` | Corrected an initial draft false premise before test installation. Expect the four-cell cone **union** the actual final wet-ground cell. Do not remove the landing write or widen the cone. |
| Ground Surge writes all four cells and pushes the dummy from step3 to step4 | `Galvanism_GroundSurge.cs:84–91,157–162` | Select a clear push destination, require exact four-cell copied path and final contact one step beyond the initial contact. |
| Current fixture removal discards the baseline without restoring old writing | `StarterSpell3DNativeAudit.cs:347–351` | Before changing each directional lane, restore the previous bounded ground while the old lane/snapshot still match, then remove only owned dummy/crop entities. Snapshot the new lane after placement. |
| Native receipt acceptance requires real files and four paired profiles | `StarterSpell3DNativeAuditBatch.cs:230–310` | Preserve the existing metadata/file/hash/profile gates. Add one final-report requirement for the direction matrix; old metadata unit fixtures remain valid but cannot masquerade as the new complete native run. |

## Implementation shape

The draft scenario adds `directionX=1,directionY=0`; default east cases retain their existing coordinates. New direction cases rotate the owned dummy and the off-ray crop. A distinct `FindDirectionalLane` keeps the classic `FindLane` intact and fails explicitly when no clear in-bounds corridor exists. Search is read-only; it never deletes authored scenery, moves unrelated actors or clears ground to manufacture a lane. It checks actual occupancy across the cone/ray margin and retains the existing crop-isolation radius.

Each new command requires its source and initial recipient to be visible and explored after ordinary placement/FOV refresh. The copied sequence must hit exactly the owned dummy, follow the expected path and preserve actual damage, Wet/push, Frozen or Pacified outcomes. Actual conditional meshes and evaluated caster-bone motion continue through the existing `CommandCase` observation path. Fifteen direction rows include dx/dy, intended and final coordinates and live FOV flags. The strict `HasDirectionalEvidence` gate checks those independently of the friendly mode/pass labels.

The shared ground-restoration helper keeps the original east bounds exactly `x[-3,+5], y[-3,+3]`; north/south/diagonal bounds include their own four-cell writing. It preserves all entities and all ground outside the old lane. Profile phases continue to accumulate ordinary spell writing because their `CommandCase` branch skips per-cast restoration. The normal shutdown, scene, preferences, input and private-save isolation contract is unchanged.

## TDD installation order

1. Root copies only `StarterSpell3DNativeDirectionTests.cs` and its `.meta` into `Assets/Tests/EditMode/Presentation/Rendering/` (the actual installed location). The test uses reflection for the new scenario methods, so API absence should yield meaningful assertion RED rather than a compile failure. Filter: `StarterSpell3DNativeDirectionTests` (15 initial cases).
2. Root/pipeline records the fresh RED with zero C# errors. No production/scenario implementation before that run.
3. Review source hashes in `direction-source-manifest.json`. Apply `directional-audit.patch` to the current scenario/launcher after resolving any concurrent source changes; never blindly overwrite a changed file. Full draft copies are provided for review.
4. Run the direction guard tests plus the existing native audit contract tests. Only after GREEN should the native run be used as direction evidence. Inspect the new frames for geometry, visibility and facing at the actual game camera.

The 15 guards cover three real non-solid side-cone recipient pairs, three edge/interior pairs, authored crop preservation, old-lane ground restoration plus entity/outside preservation, a valid complete matrix, and six concrete falsifications: missing direction, relabelled east path, hidden recipient, unpushed Surge, center-only Jet, and neutral-only Rime art. Meta GUID `323f8383b0a0448e89cbe58f0822c798` was checked collision-free against current Assets before installation.

## Cold-eye review / honesty bounds

- Fixed before handoff: the initial cone-only AffectedCells expectation contradicted the shipped push landing write. Both the predicate and positive fixture now retain that fifth affected cell.
- Preserved: old keyboard proof, east showcases, empty Rain, explicit reduced counter-outcomes, the two-zone four-phase profile loop, clip/controller restoration, no simulation turns during fixture playback, and scene/input/private-save teardown.
- Cannot verify yet: whether the authored south chunk offers all three clear corridors under the actual native run, whether every new screenshot shows comfortable/readable art, or fresh runtime timings. The scenario fails visibly rather than removing scenery if the corridor/FOV preconditions do not hold. Guard tests and metadata cannot replace those native observations.
- Deferred by scope: all-eight-direction native game captures, new gameplay targeting tests, camera changes, asset geometry changes, broad fixture rewrites, save migration work and long-session performance claims.

Files: `StarterSpell3DNativeAudit.cs` and `StarterSpell3DNativeAuditBatch.cs` draft copies; `directional-audit.patch`; 15-case `StarterSpell3DNativeDirectionTests.cs` + copied metadata; source baselines/hash manifest; reproducible draft builder. No Assets file was edited by the drafting subtask.

Post-installation review: R10b confirmed 15/15 assertion RED with zero C# errors; R11 then confirmed 421/421 GREEN (the original 406 plus these 15). These cases establish missing verification infrastructure, not gameplay bug fixes. Root installed the tests under `Presentation/Rendering`. One further concrete receipt gap was found: `peakMeshes` can come from uncaptured polling even if every saved capture has zero meshes. A sixteenth `missed-visual-capture` mutation was added to the installed test file only, retaining its positive complete-matrix control; the guard remains unchanged pending R12 assertion RED. Historical draft copies retain the initial 15-case implementation and must not overwrite this later test.
