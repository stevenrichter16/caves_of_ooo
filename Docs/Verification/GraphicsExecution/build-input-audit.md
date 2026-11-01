# Bounded build-input audit

Audit: 2026-11-01T21:15:54.120711+00:00. Authoritative source: `0a2cc9baebfeedf8491db1c4e5aa5f77f9fd4cd1`. Exact paths, sizes, hashes and classifications are in `build-input-audit.json`.

No runtime contamination was identified in the unrelated overlay: **73 metadata files** (62 trailing-whitespace normalizations, 11 empty default TextScriptImporter additions with unchanged GUIDs), **two Docs PNGs**, **six untracked build-root files**, and **339 ignored build-root files**. Preserve these files and keep unrelated work unstaged. This conclusion does not mean the working tree is clean.

The six untracked files are two generated test-runner scenes with metadata, an empty Editor folder's metadata, and ProBuilder editor preferences. The ignored files comprise 158 screenshot files/metas, 164 recovery-scene files/metas, four bridge-log files/metas and 13 Finder metadata files. None adds scripts, DLLs or runtime Resources content. All **166 GUIDs** from ignored/untracked metadata have zero references in tracked Assets, Packages or ProjectSettings. Only `Assets/Scenes/Main/SampleScene.unity` is enabled for builds.

## Mandatory release gate

Root temporarily changed `FlatPixelGreyboxCatalog.cs` and `FlatPixelStumpMaterialsCatalog.cs` to the `27445b20d` performance control. These are **owned transient controls, not foreign overlay**. **Do not build until both are restored byte-exact to `0a2cc9bae`** and their hashes match `temporaryOwnedControls[].expectedPublishedSha256`. Their audit-time hashes are recorded separately.

After restoration, use the existing build workflow with SampleScene and a fresh output directory. Disclose this neutral import/editor overlay in the release receipt, preserve prior builds/saves, and re-check the inventory if inputs change. This audit did not operate Unity, mutate Git, build a player or establish packaged gameplay/Deck acceptance. It does not certify Library cache contents or all dynamic file-loading paths.

Restoration verified2026-11-01T21:23Z: both catalogs match the published SHA-256 values. Current code compiled and ran before the isolated launcher restored idle SampleScene and normal save-root/preferences. Historical audit-time control hashes above remain unchanged.
