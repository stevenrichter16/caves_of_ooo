# LongSword final preservation / cleanup inventory

Observed **2026-10-25 00:22:06 UTC** (2026-10-24 local). Read-only follow-up to `/tmp/coo-long-sword-storage-audit.md`. Root still owns the pending LongSword push and Unity session. Only this `/tmp` receipt was written; **no deletion, Unity control, staging, source modification or cleanup was performed**.

## Schedule discrepancy requiring root reconciliation

The current saved automation file `/Users/steven/.codex/automations/overnight-clearing-art-pipeline/automation.toml` says ACTIVE heartbeat, name “Caves of Ooo graphics completion”, **every10minutes**, updated_at1792887229560. Therefore the requested verification that it is currently every30minutes does **not** pass. Reported this factual mismatch to root; no scheduler mutation made. Later user steering or a newer update may explain it. This is a saved-configuration observation, not proof of scheduler execution timing.

## Disk and reclaim decision

Current filesystem total **994,610,155,520bytes**, used **988,889,661,440bytes**, free **5,720,494,080bytes (5.33GiB)**. Prior00:14:50 snapshot had1,316,909,056bytes free. The increase has an unverified external cause; it must not be credited to this audit or represented as our reclaimed bytes.

| Project path | Allocated KiB | Current directory mtime UTC | Assessment |
| --- | ---: | --- | --- |
| `Library/Bee` |211232|00:14:04|Active/useful import-build data, preserve|
| `Library/BurstCache` |117828|2026-10-24 13:02:18|Useful rebuilt cache, no exact unused subtree identified|
| `Library/Search` |61008|00:19:51|Active index, preserve|
| `Temp` |11768|00:18:43|Active Editor data, preserve|

Unity processes79930,80684,80687 remain open. No broad recursive project/Library scan was done. Directory age alone does not prove child files are unused.

**Confirmed safe worthwhile stale candidates: none. Recommended reclaim0bytes; actually removed0files /0bytes.** Root must push and verify the pending sword source/evidence before any later cleanup. With space improved and only active/useful candidates observed, no new purge is recommended.

## Preserved source, saves and current build

All source, authored assets, saves/checkpoints and evidence were untouched by this audit. This is a non-mutation guarantee, not an exhaustive all-source hash census; root owns remote blob verification.

Current delivered archive remains:
`Builds/SteamDeck/2026-10-11-fullscreen-skills-current/CavesOfOoo-SteamDeck-fullscreen-skills-current.tar.gz`

- Exact size **128,049,954bytes**.
- Recomputed SHA256 **`debc85bfacc1c018b24dc8c742b35956768c131571df0e0cb01221a9badf001b`**; unchanged.
- No other archive in bounded `Builds/SteamDeck/*/*.tar.gz` inspection. Seven obsolete archives listed in `recovery-cleanup-20261024.json` remain absent.
- Retained package is the previous delivered build, not a new LongSword/graphics package.

Rechecked present: October24 local source/evidence archive+manifest, October13 historical source archive, and `Builds/SteamDeck/2026-10-10-4c3b0c1d7/audit/Saves-before-build`. Full historical archive contents were not rehashed.

Two unrelated Notice variants remain unchanged:

| File under `Docs/Verification/AllotmentNotice/ControlledRender/` | Bytes | SHA256 |
| --- | ---: | --- |
|`notice-hidden-control.png`|120508|`b6b8a30da59ded391126a9f526052bb4da5acd98692b3587160be164b3a059db`|
|`notice-visible.png`|120837|`3fb8194882da152137cf2f8493b3800f8a3c937e138798e742ce44237af6b811`|

## Root schedule reconciliation

Root updated the automation through automation_update to preserve the observed every10minute interval and align its prompt wording. Saved ACTIVE configuration verified in `long-sword-schedule-receipt.json`. No cache/build deletion performed. Pending work must be verified on origin/main before any later cleanup.
