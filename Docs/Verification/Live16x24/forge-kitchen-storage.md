# Forge / kitchen post-push storage audit

Observed **2026-10-25 01:09:03 UTC**. Bounded read-only audit; only this `/tmp` receipt was written. No Unity commands, deletions, cache changes, source edits or staging.

## Preservation and push

Local HEAD is `a5cd732c881f33adcb3f6bf7ad86cc87593e6c81`. Root's `/tmp/coo-forge-kitchen-push-result.json` records the identical remote main and127 verified owned files. This pass read that receipt and independently checked local HEAD; it did not repeat root's server/blob comparison.

Current delivered package retained:
`Builds/SteamDeck/2026-10-11-fullscreen-skills-current/CavesOfOoo-SteamDeck-fullscreen-skills-current.tar.gz`

- **128,049,954bytes**.
- Recomputed SHA256 **`debc85bfacc1c018b24dc8c742b35956768c131571df0e0cb01221a9badf001b`**, unchanged.
- No other `Builds/SteamDeck/*/*.tar.gz` archive found. This is the previous delivered package, not a newly packaged graphics build.

All three new `/tmp` checkpoint trees exist, each retaining its real nonempty Quick save under `70a87e9febf34b499e8aaf47a44601ed`:

| Checkpoint | Quick.sav.gz bytes |
| --- | ---: |
| `/tmp/coo-forge-prepared-checkpoint` |187380|
| `/tmp/coo-forge-tempered-checkpoint` |234233|
| `/tmp/coo-forge-kitchen-final-checkpoint` |289265|

Zero-byte ForgeRouteMarker saves are known boot markers and remain preserved with their owning trees. The ten previously known Cindercaller/kitchen/court/landscape checkpoint trees remain present. October24 local source/evidence archive+manifest, October13 historical source archive and `Builds/SteamDeck/2026-10-10-4c3b0c1d7/audit/Saves-before-build` remain present. Presence was checked, not exhaustive historical-content rehash. All source, assets, saves, checkpoints and evidence were untouched.

Unrelated Notice variants remain byte-identical:

- `notice-hidden-control.png`:120508bytes, SHA256 `b6b8a30da59ded391126a9f526052bb4da5acd98692b3587160be164b3a059db`.
- `notice-visible.png`:120837bytes, SHA256 `3fb8194882da152137cf2f8493b3800f8a3c937e138798e742ce44237af6b811`.

Both are under `Docs/Verification/AllotmentNotice/ControlledRender/`.

## Space and decision

Total **994,610,155,520bytes**; used **989,234,823,168bytes**; free **5,375,332,352bytes (about5.01GiB)**. Values vary with ongoing Editor/system activity.

| Path | Allocated KiB | Directory mtime UTC | Decision |
| --- | ---: | --- | --- |
| `Library/Bee` |211232|2026-10-25 00:14:04|Useful current compilation/import data; preserve|
| `Library/BurstCache` |117828|2026-10-24 13:02:18|No confirmed unused subtree; preserve|
| `Library/Search` |61008|2026-10-25 01:03:51|Active index; preserve|
| `Temp` |11768|2026-10-25 00:33:14|Active Editor data; preserve|

Unity processes79930,80684,80687 remain active. Root reports Play stopped/restored; stopped Play does not make Editor caches unused. No broad recursive Library/build/source scan was run.

**No confirmed safe worthwhile stale candidate. Recommend reclaim0bytes; actually removed0files /0bytes.** Keep active caches and current package. A later deletion requires an exact unused candidate and root's decision; regenerability alone is not disuse.
