# Parry and guard auditions

This folder preserves the audition sources. The user selected **Parry 3 + Guard 4** on 2026-10-13; byte-identical WAV masters are now imported under `Assets/Resources/Audio/World/Defense/` and connected to real defense outcomes. Other options remain auditions. See `Docs/DEFENSE-AUDIO.md`.

The two MP3 reels play options **1, 2, 3, 4**, with one second of silence between clips and no voice-over. Individual 48 kHz mono, 24-bit WAV masters and 160 kbps MP3 previews are alongside them. Each individual master has a -6 dBFS four-times-oversampled peak estimate; guards use less high-frequency energy instead of simply lowering a parry's pitch.

| Parry option | File prefix | Direction | Length |
| --- | --- | --- | --- |
| 1 | `parry_01_clean_kachink` | Original dry contact and clear steel kachink | 0.658 s |
| 2 | `parry_02_broad_clash` | Original heavier blade clash with a broader ring | 0.798 s |
| 3 | `parry_03_dry_edge` | Recorded metal contact, short and bright | 0.458 s |
| 4 | `parry_04_rasped_catch` | Recorded contact with a rougher, warmer ring | 0.618 s |

| Guard option | File prefix | Direction | Length |
| --- | --- | --- | --- |
| 1 | `guard_01_padded_catch` | Original compact, muffled thud | 0.418 s |
| 2 | `guard_02_leather_wrap` | Recorded soft contact with a dry wrapping texture | 0.478 s |
| 3 | `guard_03_wood_brace` | Original dull wooden brace/knock alternative | 0.498 s |
| 4 | `guard_04_damped_steel` | Recorded soft contact under a short, damped metal knock | 0.578 s |

Reels: `parry_options_01_to_04.mp3` (6.132 s) and `guard_options_01_to_04.mp3` (5.572 s). Exact option offsets are in `manifest.json`.

For the current long-blade guard, the damped-metal option is a more literal material match than the wooden-brace option. The latter is offered for comparison or future material-specific defense feedback, not as evidence that a shield blocked the attack.

Original layers use short contact noise, inharmonic damped metal modes, rebounds and filtered body/friction textures. Four options incorporate the CC0 recordings credited in [SOURCES.md](SOURCES.md). Initial recordings had strong high-frequency energy; the final recorded parry options include declared low-pass filtering and a modal body layer. No Qud recordings or soundtrack material is used.

Rebuild and verify from this directory with `python3 create.py` then `python3 verify.py`. Source download hashes are pinned in `sources/downloads.json`; no download or Unity operation is performed by those commands. Keep `create.py`, the source recordings and manifests to reproduce the masters.

Automated checks decode all 20 output files, check headroom, finite samples, low DC, format and eight distinct clip identities. They do not establish subjective quality, audible fatigue, final mix level, integration correctness or physical Steam Deck sound. These need the user's audition and a later in-game mix pass.
