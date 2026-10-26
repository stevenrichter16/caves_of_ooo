# Fresh macOS package smoke test — 2026-10-26

**Source:** `c893ae58fea438d1d2bb6f803ace1e499df79acd`, as recorded by the root build operator.
**Result:** ordinary standalone Continue, current inventory, item details,
movement/save and a separate fresh Warden start were exercised. This is bounded
Mac/Metal gameplay evidence, not complete-opening or physical Deck acceptance.

The [build receipt](packaged-flat-20261026-mac-build.json) reports
`StandaloneOSX` success,0errors/4warnings,233.97MB and46.919501seconds,
03:49:33–03:50:20UTC. The output is
`Builds/Verification/2026-10-26-flat16x24-current/CavesOfOoo.app`.
Read-only binary inspection confirms universal x86_64+arm64; both captured
player logs show Unity6000.3.4f1,Mono and Metal on Apple M5. Build duration is
not a gameplay performance measurement.

## Actual input and pixels

All six screenshots are untouched original JPEGs,1280×856 including56pixels
of macOS titlebar/chrome. The game viewport is1280×800. File encoding,
dimensions,byte counts and SHA256 values were independently read and retained
in the [play summary](packaged-flat-20261026-mac-play-summary.json).

| Capture | Bounded observation |
| --- | --- |
| [Start](packaged-flat-20261026-mac-start.jpg) | The ordinary player offers C Continue/N new game. The displayed opening behind the prompt is not the resumed checkpoint. |
| [Continued](packaged-flat-20261026-mac-continued.jpg) | C resumes the earned north works at73,17,15/40HP,75XP and50drams. This is an existing injured character, not the separate40HP garden replay. |
| [Inventory](packaged-flat-20261026-mac-inventory.jpg) | I opens Pack & Equipment. `DMG / DEF` shows long sword `1d8` and buckler `1 AV +1 DV`; each equipped row has `[E]` at the far right. The anatomy pane correctly names left-hand sword/right-hand buckler. |
| [Reader](packaged-flat-20261026-mac-reader.jpg) | F1 opens long-sword details with its native portrait,1d8-per-penetration text and current left-hand slot. Page1/2 and its reading controls remain visible. The sampled backing is solid; this does not re-prove every popup variant. |
| [Moved](packaged-flat-20261026-mac-moved.jpg) | After closing the reader and inventory, ordinary movement changes73,17→72,17 with15/40HP unchanged. Ground loot appears in the current-cell readout. |
| [Fresh Warden](packaged-flat-20261026-mac-new-game.jpg) | A separate normal new game begins as Warden with sword,buckler and three skills. The screenshot shows41,12 after movement from40,12,40/40HP and50drams. The opening renders in flat16×24. |

Root also pressed F5 and quit normally. The retained continued-save metadata
records north zone`Overworld.11.9.0`,tick1450,15/40HP at03:53:03UTC. The
separate fresh save is`9d0c0b27d7754da8805eacb5839c7be7`,opening zone11,10,
tick20,40/40HP at03:54:54UTC. Checkpoint paths and hashes are in the summary.
These two short sessions are not a fresh packaged six-site journey.

The continued character`54bc59c0fe6243aaa949342f54273a84` came from the
already-earned `/tmp/coo-seed65-ground-verified` checkpoint, source save SHA256
`a3e56d62871831933e6c489fb5210c630fca9bb16a6f6186c76adbb2c38a8402`.
It was copied into the player's separate bundle save location for this test.
No movement,inventory,health or presentation overrides were injected to obtain
the captured play; placing an earned save is distinct from creating one.

## Log review and preservation

[Continued-player log](packaged-flat-20261026-mac-player.log) and
[new-player log](packaged-flat-20261026-mac-new-player.log) both complete
bootstrap/renderer/input wiring and show the normal Input System shutdown
sequence. Neither contains an explicit exception,error,failed or fatal line
in a case-insensitive keyword scan. That limited scan is not a guarantee of
zero defects.

**Both logs still contain six premature-thread-finalization notices and one
ComputeBuffer-disposal notice during shutdown.** This is the known unresolved
resource-lifecycle follow-up; do not call either shutdown warning-free.
The build's four warnings are reported by the receipt but not individually
identified in these player logs. No newly diagnosed runtime cause or fix is
claimed here.

The [restoration receipt](packaged-flat-20261026-mac-restored.json),03:55:35UTC,
records that the bundle save location did not originally exist and that both
preference domains were restored. Independent comparison of archived original
and restored plists confirms equal logical mappings for
`com.DefaultCompany.2D-URP` and`unity.DefaultCompany.caves-of-ooo`.
Temporary earned/fresh saves remain under
`/tmp/coo-packaged-20261026-protection`. This receipt covers the standalone
isolation; it does not independently audit every normal editor save or final
post-Linux editor/project restoration.

## Remaining acceptance

Can verify the successful Mac build,actual continued/new-game rendering,
sampled inventory/reader input,movement/save results and recorded restoration.
Cannot verify Linux execution,Vulkan on Linux,physical Steam Deck controls or
readability,all inventory cases,full packaged travel,regional art completeness,
audio quality or frame-time/loading/battery performance. Linux packaging is a
separate root-owned task and was pending editor reload recovery when this
Mac evidence review was written. No new native-test totals or performance
improvement claim are introduced by this report.
