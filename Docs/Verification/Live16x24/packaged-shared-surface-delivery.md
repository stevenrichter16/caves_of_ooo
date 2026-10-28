# Frogs, shared vines and hollow logs: packaged delivery

Runtime source `fa553ca364a092a515e4837aca7859066c7a426d` is verified on origin/main and includes frog source `0f24f4942d0ff18616ccdcb94d5d62607ad05252`. Original native16×24 Reedfrog/Bandfrog bodies, approved16×16 GinFrog/HollowLog art and approved connected vine frames now ship together. This is bounded body/reader coverage, not accepted biome polish.

## Evidence and builds

One final complete native JSON job passed336 distinct cases, zero failures/skips; earlier subset runs are not added. Mac job `build-c43956d046` succeeded in79.745179s; Linux `build-ada86ac4fb` in79.831465s, each zero errors/four warnings. Both follow the recorded source boundary `2026-10-28T10:29:01.737362+00:00`, use SampleScene/GameBootstrap and detailed_report. No additional tests were needed for packaging.

Mac: `Builds/Verification/2026-10-28-shared-surface-fa553ca36-current/CavesOfOoo.app`.
Linux: `Builds/SteamDeck/2026-10-28-shared-surface-fa553ca36-current/CavesOfOoo-current`.
Archive: sibling `CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`,128,827,356bytes, SHA256 `6384bf00b5e20d33e1e21f20b929c1f83441256882403ce981fc2710ad4cba68`. All190 files,13 directories, modes, internal checksums and gzip EOF/CRC verified. Original raw executable was named `CavesOfOoo`; it is preserved, with a byte-identical0755 copy named `CavesOfOoo.x86_64` for the launcher. The unchanged raw build receipt and explicit copy provenance are retained. Linux and physical Steam Deck were not executed.

## Actual Mac observations

Nine unretouched1280×856 window captures include56px title bar above1280×800 gameplay. All actions used normal keyboard/UI; no simulation state injection or merged checkpoints.

| Process | Earned input | Observations |
|---|---|---|
|28703|sodden-ordinary-before-open-mire65|OpenMire world, Bandfrog40,1, GinFrog20,22, HollowLog35,23 and VineWall66,15 exact readers; F5 checkpoint160 at40,12,HP38,turn2330|
|30439|shared-surface-packaged160|Fresh Continue; capture06→07 world rectangle `(0,96)-(940,696)` matches all564,000 pixels|
|30688|sodden-ordinary-before-reed-maze65|Separate ReedMaze world and Reedfrog24,11 reader;HP38, no gameplay movement|

Captures01–05 are world/readers;06–07 save/Continue;08–09 ReedMaze/Reedfrog. In process2 root initially pressed ineffective U, then used the displayed C command; the startup trial image is retained separately, not used as acceptance. A CUA timeout interrupted the process3 Look-cursor sequence at25,12; root reacquired actual UI state and finished the remaining one-left/one-up/Return/X. No gameplay turn was advanced by these reader actions. Command-Q ended each process, and root avoided querying closed app bindings.

This package pass adds no actual combat, wall destruction, storage transfer, movement or cross-zone travel claim. Prior Editor south travel, real Open/Take, damaged HP branches and save persistence remain separately documented in `Docs/GRAPHICS-SHARED-VINEWALL.md`; frog Editor observations remain in `Docs/GRAPHICS-SODDEN-FROGS.md`. The three Mac logs retain5/6/6 premature-thread-finalization notices and one ComputeBuffer-disposal warning each. No clean-log, sustained performance or physical Deck claim.

## Preservation and review

The source-bound snapshot `/private/tmp/coo-shared-surface-build-protection-fa553ca36` protects52,905 tracked objects,49 foreign dirty paths, normal saves/preferences and prior packages. The exact reviewed restoration receipt records the applied result; ten generated files comprise six whitespace-only metadata rewrites plus four build-serialized settings. Their pre-restoration bytes are backed up. Owned hourly-report edits in three living records were temporarily held outside the repository to verify the unchanged source snapshot, then recovered afterward. No unrelated file is restored or staged.

Checkpoint160 Quick pair is preserved under `Fixtures/shared-surface-packaged160`. The first two owned standalone branches are archived at `/private/tmp/coo-shared-surface-packaged-openmire-preserved`; the final ReedMaze branch is archived by the restorer. Normal standalone saves were originally absent and are restored to absence by archiving, not deleting. Original preferences and prior builds remain protected. Storage is inspection only; no cache deletion or reclaimed-byte claim.

Reproduce by copying the named earned Quick pair into an isolated owned save folder, launching the named app and pressing C. Use L, period to center Look, single directional taps with observations, Return and X for the selected owner's reader. Keep the input branches separate. Do not hold movement keys: the earlier Editor deviation and real bogmire damage are retained in the source record.

Independent package/capture/restoration review is in `packaged-shared-surface-independent-review.md`. Coverage leaves broad quiet patches, subtle log scale, mixed glyphs and dark mire hazard salience unresolved. All55+5 area-polish scopes remain open; nuanced animation is deferred. The next family is selected by whole-game visible impact, not another cave queue.

## Next visual choice

Greatdew is the next bounded missing body: current OpenMire25501 at24,21 remains a cyan glyph; healthy earned frog157 retains player40,12HP38. Compare shared equipment (Battleaxe disclosed but109drams versus22 and no exact approved art), settlement roles (Armorer/Apothecary lack actual disclosed baselines), mire (already-native material; polish debt), and TarSeep/Husk (reusable art but another cave queue). Greatdew offers immediate surface/hazard identity. Its non-solid Plant10HP, repeatable55% step-snare/Rooted4/Acidic1 contract needs actual reader, imagegen/native art and bounded integration/play gates; no aura, harvest or guaranteed proc. Source Objects.json34711 and TriggerOnStepPart.cs440. This selection is planned work, not new coverage acceptance.
