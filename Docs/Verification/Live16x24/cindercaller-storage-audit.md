# Cindercaller post-push storage audit

Observed **2026-10-24 22:46:59 UTC**. Bounded read-only audit; only this report was written. No Unity control, staging, push or deletion was performed.

## Push and protected data

Independent `git ls-remote origin refs/heads/main` and local HEAD both resolve to **`c76654faa17c0162bb6390007951ff37b14a657f`**. Root’s `/tmp/coo-cindercaller-push-result.json` records **59 owned files verified** against that tip. This pass rechecked remote identity, not all59 blobs again.

Retained current delivered package: `Builds/SteamDeck/2026-10-11-fullscreen-skills-current/CavesOfOoo-SteamDeck-fullscreen-skills-current.tar.gz`.

- Size **128,049,954 bytes**; recomputed SHA256 **`debc85bfacc1c018b24dc8c742b35956768c131571df0e0cb01221a9badf001b`**, unchanged.
- Only archive in inspected `Builds/SteamDeck/*/*.tar.gz` locations. All seven superseded archives named by `recovery-cleanup-20261024.json` remain absent. Retained archive is the previous delivered build, not a fresh Cindercaller build.
- October24 source/evidence archive and manifest, October13 historical source archive, and `Builds/SteamDeck/2026-10-10-4c3b0c1d7/audit/Saves-before-build` remain present. Presence was checked; archive contents were not exhaustively rehashed in this bounded pass. All source, saves, checkpoints and evidence are protected from cleanup.
- `cindercaller-native-cleanup.json` records root’s22:43:41 stop/restoration: Play=false, isolation=false, saveRoot=null, artPending=false, viewBorrowed=false and SampleScene restored. This audit did not invoke those actions.

Unrelated Notice files retain their exact bytes:

| Path under `Docs/Verification/AllotmentNotice/ControlledRender/` | Bytes | SHA256 |
| --- | ---: | --- |
| `notice-hidden-control.png` |120508|`b6b8a30da59ded391126a9f526052bb4da5acd98692b3587160be164b3a059db`|
| `notice-visible.png` |120837|`3fb8194882da152137cf2f8493b3800f8a3c937e138798e742ce44237af6b811`|

## Storage decision

Filesystem total **994,610,155,520 bytes**, used **992,776,761,344 bytes**, free **1,833,394,176 bytes (about1.71GiB)** at observation time. These values can change with ongoing system/Editor work.

Unity Editor/import processes remain active (PIDs79930,80684,80687), despite Play being stopped. Preserve these useful caches:

| Path | Allocated KiB (`du -sk`) | Directory mtime UTC |
| --- | ---: | --- |
| `Library/Bee` |210488|22:35:38|
| `Library/BurstCache` |117804|13:02:18|
| `Library/Search` |61004|22:43:46|
| `Temp` |11768|22:38:05|

Regenerability does not establish disuse; directory mtime does not establish child-file inactivity. No broad project/Library scan was run.

**No confirmed safe worthwhile stale candidate found. Recommend no additional deletion: proposed reclaim0bytes; actually deleted/freed0bytes.** Only the retained current package remains; active caches would cost another rebuild and cannot be treated as disposable while Editor processes are open. Any later cache purge is root’s separate decision after confirming the Editor is closed and idle.
