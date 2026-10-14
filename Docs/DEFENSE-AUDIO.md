# Parry and guard audio

User selection (2026-10-13): **Parry 3** (`parry_03_dry_edge`) and **Guard 4** (`guard_04_damped_steel`). Preserve the selected master recordings; no rerolling or pitch randomization. This integrates the previously reviewed auditions, not new Qud assets or changes to defense balance.

## Pre-implementation sweep

| Reference | Verified contract |
| --- | --- |
| `CombatSystem.PerformSingleAttack` | One weapon attempt is captured before accuracy. `BladeDefense.TryParry` resolves after accuracy; a full parry returns before penetration/HP damage and before riposte. Guard fallback continues through normal reduced-damage resolution. |
| `WorldAudioCombat` | Typed receipts already carry the copied contact, participants and original target visibility. Observer failures are caught; no simulation RNG. Extend this seam, not log scraping or general damage callbacks. |
| `WorldAudioEventRouter` | Rejects hidden/off-zone actors, old contact, muted/hidden world via downstream player, and disposes additive subscriptions. Generic hooks must refuse the two new combat-only cue IDs. |
| `WorldAudioPlayer` | Existing 12 short-cue voices, per-cue cooldown, SFX preference and combat-focus envelope can handle defense without a new source or subsystem. Pitch stays 1. |
| `WorldAudioLibrary` and catalogs | Resources load at preparation time. Append cue enum IDs; add a small catalog with the exact two selected WAV masters and import metadata. |
| Audition manifest / `SOURCES.md` | Parry 3 includes CYBERDEViL's CC0 machete contact; Guard 4 includes StarNinjas' CC0 soft impact. Both include original processing/layers. Preserve source provenance for shipped assets. |

## Plan

1. RED tests for real full-parry/fallback/ordinary/miss paths; resource bindings; perception/mute/disposal/semantic-hook counter-checks; observer isolation.
2. Append Parry/Guard cue IDs and emit a typed defense receipt immediately after resolution and before riposte. Full parry emits Parry only; fallback emits Guard only. Ordinary misses/hits and generic damage cannot emit defense sounds.
3. Import unchanged selected short mono WAVs with preload and PCM decompression; bind to existing SFX bus with conservative gains. Preserve licenses/credits and SHA256 master equality. No new settings, saves, or input.
4. Native targeted regression, independent review, real Play-mode signal check with private save isolation. Report script-observable playback separately from subjective listening and physical Deck testing. Fetch/rebase/push main.

## Results

Implemented through the existing typed combat-audio path. Approved masters are byte-identical; PCM/preloaded imports retain pitch 1 and use SFX gain 0.48. CC0 credits are included in Resources.

- Initial native RED: 18 tests, seven expected missing-cue/binding failures, 11 controls passing.
- Independent review found no production blocker and added passive-failure, armor-stop and lethal/nonlethal riposte counter-checks. The first broader run exposed two test maintenance issues: its existing combat-cue allowlist needed Parry/Guard (9 → 11), and the new armor-stop fixture had added a second natural armor part instead of modifying the first. Both fixtures were corrected; no balance changes.
- A retry was required when Unity's test job failed to initialize after compilation; that timeout ran zero cases and is not test evidence.
- Final native GREEN: **225/225**, zero failed/skipped, across 11 defense, combat, mix, routing and floating-text fixtures (22 new defense cases). No production changes were needed after that final run.
- Native Play-mode check in the isolated starting-zone fixture used actual `PerformSingleAttack`, not direct cue playback. FullParry selected `parry_03_dry_edge` with zero damage and two accepted cues (swing + parry); guarded Hit selected `guard_04_damped_steel`, dealt two damage and accepted three cues (swing + guard + damage impact). Both AudioSources reported playing. Scheduled frame sampling captured nonzero output peaks of 0.06907652 and 0.09588927 respectively, at temporary SFX volume 0.7. The selected WAVs and pitch were unchanged.
- Initial immediate tool-triggered sampling measured zero output despite a playing source; scheduling the strikes after the tool call returned avoided the inspection call consuming the short clip's sampling window. Only the scheduled peaks establish nonzero output. This is an instrumentation correction, not a gameplay change.
- Private save-root, last-game preference and prior SFX level restored after the live run; no scene asset or personal save was changed. Evidence is under `Docs/Verification/DefenseAudio/`.

## Review / honesty bounds

🔵 The typed receipt is emitted before riposte so a lethal counter cannot erase the original parry sound. A guarded parry emits only Parry; failed guard then armor stop emits Guard without an invented HP impact. Cue IDs are appended, generic sound hooks refuse them, and existing visibility/contact checks, observer exception containment, per-cue cooldowns, mute, focus and disposal apply.

🧪 Existing short-cue capacity/cooldowns can coalesce dense simultaneous contacts, as for other attack cues. No new voice pool, settings, gameplay RNG, costs or save state. Physical Steam Deck listening and subjective in-mix loudness are not established by script checks. No distribution build is part of this selection change.
