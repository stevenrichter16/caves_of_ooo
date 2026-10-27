# Packaged limestone caves — independent bounded delivery review

Reviewed 2026-10-27. **Bounded delivery PASS** for source `7f49fb70054b7b29957dacd17c999ae4b521c9fd`. This accepts the reviewed package integrity and limited Mac smoke journey, not complete cave coverage or area polish.

Root executed builds, native tests, ordinary Mac controls and restoration. This reviewer independently opened all seven Mac captures, read and hash-checked their receipts, streamed the Linux archive and inspected the restoration evidence. No Unity/Git/build/player/helper/restore execution occurred in this review. The new helper adaptation received a separate source cold review before execution; its successful execution does not itself certify gameplay.

## Build and archive integrity

Both build receipts bind to the exact source and start after the 18:48:27 UTC freeze. Mac succeeded in 59.143178 seconds, 344.39 MB; Linux in 64.449915 seconds, 324.85 MB. Each reports **0 errors / 4 warnings**. The reported destinations are the fresh `2026-10-27-limestone-caves-7f49fb700-current` Mac and Steam Deck output folders. The source-to-build association is supported by root's freeze/build receipts; this is not independent bit-reproducible compilation proof.

Independently streamed the exact archive at:
`Builds/SteamDeck/2026-10-27-limestone-caves-7f49fb700-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`.

- **128,802,831 compressed bytes**, SHA256 `19b8248db537956f7cd76dc58d21e92123adc9cef80cea268c3d23989120fdd4`; sibling checksum matches.
- **202 members: 189 regular files and 13 directories**, with exact complete manifest coverage, path safety, no duplicates/links/special files, and matching modes, sizes and streamed file hashes against the unpacked tree.
- **341,122,323 unpacked file bytes**; 188 internal checksum entries cover every file except the checksum list itself.
- A separate bounded gzip read reached EOF and verified its CRC/ISIZE/footer. Build-info, copied build reports, source revision, fresh output paths/timestamps and ELF64/Mach-O header identities agree. Neither executable was launched by this reviewer.
- Six credit sections, five licenses, aggregated audio credits, provenance files, StarterSpells sources, launcher and README match their source inputs. The previous shallow-caves archive still matches its retained hash.

Independently rechecked the two exact native XML hashes and full test identities: 36 limestone plus 395 related = **431 distinct Passed**, no overlap/failures/skips/inconclusives. These are the retained Editor tests; they were not rerun by packaging, and the earlier failed related attempt is not counted.

## Seven actual Mac views and persistence

Opened `packaged-limestone-caves-01` through `07` JPGs. All are 1280×856, including a 56-pixel window titlebar; all seven hashes match the Mac-play receipt and all seven files are distinct. The game view is 1280×800.

01 continues the earlier earned wreckage89 fixture at 44,15, HP 33, with rubble beneath the player.02 shows the actual replacement's readable native rubble portrait and Terrain identity after stepping aside.03 shows the chamber: quieter continuous floor, lighter planar solid walls and low debris remain readable; no new pit-like backing defect or obvious UI clipping appears. Repeated wall fissures, scattered rubble, sparse furnishing and separate uncovered glyph owners remain visible polish/coverage debt.

04 returns normally to depth 3 at 40,12; 05 reenters depth 4 at 40,12. Native material/debris/stair scale remains consistent across that observed roundtrip.06 saves at 40,13, HP 33; 07 shows the same position/health after a fresh ordinary Continue. Independent decoded RGB comparison of 06/07 world rectangle `[0,96,940,696]` found **zero changed pixels across 564,000 pixels**. Window chrome, sidebar, log and footer are excluded from that comparison.

Root's process receipt records exact-app PID 45475, full quit and confirmed absence, then fresh PID 45876; both owned processes were subsequently closed. The images alone do not prove process identity. Earned fixture91 save/metadata hashes match the receipt; metadata records `Overworld.16.13.4`, turn 12390, HP 33/40. The starting wreckage89 fixture also still matches its frozen hashes. This smoke used the earlier HP 33 fixture, not the later dangerous Editor roundtrip90 at HP 6.

The displayed wall-destruction log is inherited from that save. There was **no new packaged destruction, combat or hazard application**. Reader coverage here is the rubble interaction and visible travel/terrain presentation, not a fresh packaged interaction with every kit owner. The later normal depth 5 shale entry/return and fixture92 are separate next-phase fallback baselines, not native shale acceptance. Editor and Mac grading differ; no cross-environment pixel identity is asserted.

## Logs and preservation

Both retained player-log byte counts/hashes match the Mac receipt. Current/previous logs contain **4/6 premature-thread-finalization warnings**, respectively, and each contains **one ComputeBuffer disposal warning**. A bounded line search found no `Exception` line. These shutdown/resource-lifetime warnings remain explicit; this is not a warning-free-runtime claim.

Root's completed 18:54:49 UTC restoration receipt reports **51,190 tracked snapshot objects equal**, **46 pre-existing dirty tracked files preserved**, normal saves/preferences restored, prior builds preserved and the originally absent standalone save root archived. This reviewer verified the receipt's reviewed-manifest hash and the retained test-save archive's existence, rather than rerunning the full restoration/private-preference audit.

The four manifest-listed restorations are the owned limestone PNG metadata, URP settings, URP global settings and ProjectSettings. One additional prior-owned shallow-caves PNG metadata restoration was separately recorded and backed up by root, outside the limestone helper allowlist. This reviewer independently checked its before/restored hashes and verified that its only difference is six trailing-whitespace lines. The exact introduced preference key remains `CavesOfOoo.LastGameID`; no broad preference permission was added. No cache cleanup/deletion occurred.

The package delivers the bounded depth 3/4 limestone six-owner kit and guarded ambient backing. All-column/whole-chunk composition, remaining actors/props/hazards, special sites, shale/deeper bands, sustained performance, Linux execution and physical Steam Deck acceptance remain open.
