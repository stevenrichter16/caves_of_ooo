# Six original creature native art demonstration — native acceptance harness

This is a staged visual/animation acceptance harness, not encounter or combat
balance verification. It uses an isolated ordinary new game in the unchanged
SampleScene, then creates only six explicitly allowlisted factory entities,
one at a time, on a verified empty two-cell run near the player. Existing
entities, equipment, HP, factions and AI configuration remain untouched. The
staged owner is not scheduled for AI turns; no player turns are issued during
inspection. Each real loadout is observed as modelled gear or an explicit native
fallback. No new gear is granted to make an image look complete.

The actual ordinary-zone presenter, adopted voxel bodies, rig/controllers,
materials, shadow surface and picking operate normally. The launcher enables
labelled full reveal and a temporary camera inspection zoom, then restores both.
Idle captures show the actual body. A real Zone move plus an explicitly emitted
presentation Moved hook shows the existing Walk route; this is script-controlled
placement, not player input. Explicit presentation Attack hooks for the five
Marlbacks demonstrate animation wiring without inventing actual damage. The
passive moth is exercised only through Idle/Walk and is never made hostile. Its
shared art-export template retains unused extra clips; those are not gameplay
attack abilities. Bone samples prove
motion within the existing controller, while screenshots still require human
inspection for acceptable appearance and deformation.

For each owner: correct model identity/voxel binding, visible skinned renderer,
actual gear references or declared fallbacks, pickability, changing Walk bones,
Marlback Attack pose/bone change, hidden-owner suppression, fresh visible recovery,
and removed-owner disappearance. All six must finish. After cleanup, compare
preexisting entity identities/positions/equipment/HP and the native turn count.

Preflight/counterchecks: refuse non-isolated launcher, non-Normal/new-game state,
unsupported ordinary map, absent prefab/rig/clip, blocked or populated stage cells,
or damaged/replaced player. No broad entity deletion. Clean up only explicitly
created owners, restore input settings/background/camera/reveal, and let the
existing NativeSaveIsolation plus scene/GameView restorer finish. Parent verifies
the actual open Editor log byte range and settings/save isolation after Play.

Current status: the first native run completed its 75 scripted checks, but
visual clarity required a new separated staging lane and the moth still needs
an evidence-led voxel silhouette review. The native receipt and launcher recovery
are recorded below. Parent owns all Unity calls; numerical completion does not
close the visual acceptance gate.

Each of the five Marlbacks must also keep its real blueprint loadout: Scrabbler
has one Dagger/Hatchet/Cudgel; Gleaner Dagger/ShortSword; Tunnelguard Spear;
Wallkeeper LongSword, LeatherArmor and LeatherCap; Breacher BreacherCleaver,
LeatherArmor, IronHelmet and IronshodBoots. The moth has no gear, remains passive
and has no gameplay LightSource. The observed gear list cannot pass vacuously.

Invoke `CavesOfOoo.Editor.OriginalEnemyNativeBatch.Launch()` from clean Edit mode.
Reports and screenshots go under
`Docs/Verification/DensityCompletion/OriginalEnemies/Native/<runId>/`.
The editor remains open. The batch-only Run method exits the editor and should
not be used for interactive acceptance.

The stage check now also compares its live body mesh by reference with the exact
voxel catalog replacement for the current source prefab skin. It requires a
distinct adopted mesh and matching bindpose count, so model-name/rig presence
alone cannot claim voxel adoption. This adds no transforms or equipment.


## First native run and cleanup repair

Run `238dc69e867c473bb07c8286c911f14d` completes all75 scripted checks,
zero subscribed errors, with23 actual captures. This proves adopted live mesh
identity, the exact real loadouts, rig motion, picking, visibility and removal,
and preserved preexisting world/turn state. It does not certify visual quality:
inspected images put each staged body too close behind the player, and the moth
loses wing separation in its coarse adopted voxel silhouette. Stage separation
and an evidence-led moth review remain open; the first images are retained.

Post-run inspection found the original scene snapshot was correct but remained
pending after Play ended. The one-shot delay could run before teardown settled
and return without another retry. This is distinct from save-root/preferences,
which restored correctly. `NativeDensitySceneRestorationTests` reproduces all
six retry/cleanup cases before the repair; an earlier discovery attempt ran
zero cases and is not counted as RED. Three current launchers now keep exactly
one editor-update retry until settled, then unsubscribe on success or when no
restoration is pending. Native GREEN and a complete follow-up Play restoration
are required before this cleanup correction is accepted.


Restoration follow-up: six new regression checks plus14 existing save-isolation
controls pass20/20 natively (`Integration/native-scene-restoration-green.*`).
The subsequent thermal Play run aborted its gameplay workload but restored the
exact prior ReferenceGlade scene/start scene, seed, save override, preference and
background/input settings. This closes the observed launcher retry defect;
the creature image-clarity and moth silhouette gates remain separate.


The revised stage selector now requires an unobstructed two-cell lane three to
five columns beside the untouched player, with a clear one-cell surrounding
margin. It fails explicitly if the generated native map lacks that lane; it does
not move/remove the player, clear real terrain or hide other owners. The camera
still follows the staged owner and the actual idle/walk/attack/adopted-mesh/gear
checks are unchanged. The first overlapping native images are the failing visual
evidence for this correction. Offline compilation of this narrow player change
against actual imported Unity assemblies passed with zero errors; a repeated live
capture remains required. The root-owned launcher retry was not overwritten.


The moth-only source/density correction is now published after actual native
RED. Its separate living doc is `Docs/DENSITY-GROVE-MOTH-ART.md`; source preview,
source/rig preservation and explicit scoped import commands are recorded there.
Native import, seven-case GREEN and separated live appearance are still pending.

## Actual imported and native visual acceptance

The exact one-source and one-mesh import passed with preserved borrowed assets
and GUIDs. The moth has816 voxel triangles, five bones, two paint colors and
.0625 world pitch. All7 new native cases pass, and the broader actual native
density/palette/original-art selection passes134/134 (MothRefinement/native-related-green).

Native/600fc89c6b1843efb4f399b603f962b7 completes75/75 checks,0 subscribed errors,
23 captures in8.436s. Root viewed all six separated world-idle images and the
moth walking frame. Owners no longer overlap the player; real carried gear is
visible. The moth now shows separate wing lobes with a contrasting body rather
than the previous solid pale square. These limited visual corrections are
accepted; the broader reference-glade humanoid/ground pass remains separate.
The live model/bindpose/animation/picking/visibility checks still use native
owners and the original loadouts. This staged harness is not combat or encounter
balance evidence. Exact editor/save/input restoration and actual log byte range
are archived beside the report.
