# Tween readout post-push storage audit

Observed **2026-10-25 01:27:05 UTC**. Bounded read-only inspection. Only this `/tmp` report written; no Unity actions, repo/source edits, staging, cache changes or deletion.

Local HEAD=`43b43c6a6164faa23edf70da93e3f3d5b3abb119`. Root's `/tmp/coo-tween-push-result.json` records the identical remote main and82 exact owned files verified. This pass checked local HEAD/read that receipt; it did not repeat root's server/blob verification. Root reports Play stopped/restored at01:24:42; the Editor remains active.

## Disk and candidates

Total994,610,155,520bytes; used989,288,124,416bytes; **free5,322,031,104bytes (about4.96GiB)**. Compared with prior forge/kitchen audit, free decreased53,301,248bytes. This is filesystem activity, not reclaimed space or proven cache growth.

| Path | Allocated KiB | Prior KiB | Directory mtime UTC | Decision |
| --- | ---: | ---: | --- | --- |
| `Library/Bee` |211804|211232|01:14:28|Recent compilation/import data; preserve|
| `Library/BurstCache` |117828|117828|2026-10-24 13:02:18|Useful current cache, no exact unused candidate|
| `Library/Search` |61008|61008|01:24:47|Active index; preserve|
| `Temp` |11768|11768|01:18:03|Active Editor data; preserve|

Unity processes79930,80684,80687 remain active. No broad recursive project/Library scan. Directory mtime cannot establish child-file inactivity.

**No exact safe worthwhile stale candidate identified. Recommend0bytes additional cleanup; actually deleted0files /0bytes.** Keep useful caches; stopped Play does not mean the Editor has released them.

## Retained current build and sources

`Builds/SteamDeck/2026-10-11-fullscreen-skills-current/CavesOfOoo-SteamDeck-fullscreen-skills-current.tar.gz`: **128,049,954bytes**, freshly recomputed SHA256 **`debc85bfacc1c018b24dc8c742b35956768c131571df0e0cb01221a9badf001b`**, unchanged. No other `Builds/SteamDeck/*/*.tar.gz` archive exists in this bounded search. It is the previous delivered package, not a new tween build.

October24 local source/evidence archive+manifest, October13 historical source archive and `Builds/SteamDeck/2026-10-10-4c3b0c1d7/audit/Saves-before-build` remain present. Source/assets/saves/evidence were all untouched; historical source archives were presence-checked, not exhaustively rehashed.

Both unrelated Notice PNG hashes remain exact:
- `notice-hidden-control.png`:120508bytes, `b6b8a30da59ded391126a9f526052bb4da5acd98692b3587160be164b3a059db`.
- `notice-visible.png`:120837bytes, `3fb8194882da152137cf2f8493b3800f8a3c937e138798e742ce44237af6b811`.

Paths are under `Docs/Verification/AllotmentNotice/ControlledRender/`.

## New replay checkpoint verified

Compared every file in `Docs/Verification/Live16x24/tween-readout-checkpoint.json` against `/tmp/coo-tween-replay-checkpoint`: **5/5 paths, sizes and SHA256 values match**, including both live and backup save/metadata pairs and the intentional zero-byte boot marker.

- Current real save `70a87e9febf34b499e8aaf47a44601ed/Quick.sav.gz`:245468bytes, SHA256 `f899774f043016ad8fed0de4ca8c7a3ce0d87170c8d7f0ca189151ec568f4dc6`.
- Backup `Quick.sav.gz.bak`:244432bytes, SHA256 `8af30c1e4703538825afd49a95fb423b4c1a1bf1923188a6523a9e19c5ea5c41`.
- Both metadata files411bytes; exact receipt hashes match.
- `TweenReadoutMarker/Quick.sav.gz` is0bytes; preserve the marker but skip it when resuming gameplay.

All13 previously known forge/kitchen/Cindercaller/court/landscape checkpoint trees remain present. This replay branch does not replace the separately retained prepared, tempered or accepted crop branches. Receipt byte verification does not itself establish save gameplay validity; root owns actual reload evidence.
