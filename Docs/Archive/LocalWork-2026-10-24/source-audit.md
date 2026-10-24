# Recovery source-preservation audit — 2026-10-24

Read-only audit. No Unity, deletions, Git mutations, or repository edits. Local refs were inspected without fetching; root must confirm the remote after the final push.

## Result

- `main` / local `origin/main` both point to `dd1d1179809ae009dcf5f07d785eb73fd2c57154` (0 ahead / 0 behind). Recent grouped readout and TagManager source is committed. No tracked code/configuration changes remain at this snapshot.
- Full `-uall` status: 9,201 untracked paths, plus two modified historical PNGs. Do not stage all of Docs/Verification: it is predominantly captures, compressed native diagnostics and historical model snapshots.
- New authored design omission: `Docs/Design/EXPERIENCE-BASED-EXAMINE.md` and the entire `Docs/Design/ExamineDiscoveryMockups/` set. Preserve as historical design/concepts, not runtime implementation or acceptance. All ten PNGs, prompts, gallery and manifest are meaningful source; ZIP members are byte-identical to the ten loose PNGs plus four text/JSON gallery inputs. Keeping ZIP preserves existing README download link, though it duplicates ~14 MB.
- Changed local ProBuilder preference adds only `lightmapping.autoUnwrapLightmapUV=true`. Preserve a dated inert snapshot if satisfying all local authored/settings work; no need to apply it as a game feature.
- Three ignored build-audit inputs (two scripts and one launcher) have no exact blob in origin/main ancestry; preserve them as inert historical inputs before deleting their build containers.

## Exact meaningful preservation candidates

| Repository-relative path | Bytes | SHA-256 |
| --- | ---: | --- |
| `Docs/Design/EXPERIENCE-BASED-EXAMINE.md` | 30781 | `1a8b376469e71f010ab45e3609c9d7b7ea0396deccc4f2367ca6e63c76fa319b` |
| `Docs/Design/ExamineDiscoveryMockups/01-viper.png` | 1554142 | `e70a76f21f397ba1341d56dd5bdb525799a314f19f48479fb091dabb94ccba9e` |
| `Docs/Design/ExamineDiscoveryMockups/02-cave-bear.png` | 1525642 | `9742a77bc7dbef9b560941d5b8664b5d4c7d80fa1d0deb21649d3b39fba2d32a` |
| `Docs/Design/ExamineDiscoveryMockups/03-glowmaw.png` | 1645118 | `c1d859741c24c3edd248202bc142d91a3ba5f999636914accf14769f1109d58c` |
| `Docs/Design/ExamineDiscoveryMockups/04-spore-shambler.png` | 1612526 | `3b5941f6fde3f742ec21caf45aaf233e06ab43b22600f4df2dc49da09420f21f` |
| `Docs/Design/ExamineDiscoveryMockups/05-scorpion.png` | 1560770 | `ec9f6e29375d774455a6ccd25d4e17def01b31a1b7570b9f73a75fb4f5caf3d4` |
| `Docs/Design/ExamineDiscoveryMockups/06-fire-clay.png` | 1453340 | `ce1e520d44300a4047635d03f60ccfc9639ee0ff978c8257b3833c8727219842` |
| `Docs/Design/ExamineDiscoveryMockups/07-clay-lined-well.png` | 1572508 | `9b51f4f0f3842c9153e55b15d763d756fb811d06ba3ff9c3ca180dd47448bc38` |
| `Docs/Design/ExamineDiscoveryMockups/08-seamleaf-crop.png` | 1562283 | `0abf5a82954033483d3964601b3e19e612c474d3e698eebae4826bd76cfee0ee` |
| `Docs/Design/ExamineDiscoveryMockups/09-salt-cured-body.png` | 1596103 | `64cfd10a93e5fe817cf00680b83d377480e79cd549187fb0d31d068dadd91c3a` |
| `Docs/Design/ExamineDiscoveryMockups/10-grooved-dagger.png` | 1467254 | `da498014f73b4e711ad40af7cc7949f9855b7b926aba7aa8dcd2cb54e58e035f` |
| `Docs/Design/ExamineDiscoveryMockups/README.md` | 2414 | `2ac60a34b98af46e1def9466e3393c0eb471a04f0e32f5932c8542e88a755702` |
| `Docs/Design/ExamineDiscoveryMockups/examine-discovery-10-images.zip` | 14164593 | `af3a2c45131bfd6499230eb82111c51ddeec46e47543d7f25cb81db366e436d8` |
| `Docs/Design/ExamineDiscoveryMockups/index.html` | 3567 | `a8c4330e15dbcbaf315c672452e41fb46e2dd4991220a38c8137561f3cd6eb62` |
| `Docs/Design/ExamineDiscoveryMockups/manifest.json` | 3891 | `ecf3d0ee15de1f1f3f37cac8d5b4696eee80bdc78ce374c49ff9b1e70c8efc1e` |
| `Docs/Design/ExamineDiscoveryMockups/prompts.json` | 11103 | `205a031ff08a8ef487b8725d1627dc4ec4c231bbe0349a174df0adec6cc1a835` |
| `ProjectSettings/Packages/com.unity.probuilder/Settings.json` | 2251 | `282259a7410fa8e15b36a4e882960e5480c4a09f2f297768ab4887aaecea686f` |
| `Builds/SteamDeck/2026-10-10-system-depth-ii-current/audit/package-deck-system-depth-ii.py` | 16764 | `4635312fc988f15e8af7adc9d5e1a945e7172b514cffa4d6da2d6430bd91877d` |
| `Builds/SteamDeck/BlackWorldDiagnosis/LinuxAudit/start.sh` | 864 | `0f67c0c16e31895f47e0bafc2e1b492f5f9c32b1a3731456ad4afed845b0c9c9` |
| `Builds/SteamDeck/2026-10-10-4c3b0c1d7/audit/Launch-CavesOfOoo.sh` | 275 | `447436c8e2e1cb9fa14d1641b9953730fff2f0279bf7e5d6e1f23e0d5fc975ec` |

## Modified evidence variants requested by root

Archive these byte-exact before any restore; not code or new passing-test evidence.

| Path | Bytes | SHA-256 |
| --- | ---: | --- |
| `Docs/Verification/AllotmentNotice/ControlledRender/notice-hidden-control.png` | 120508 | `b6b8a30da59ded391126a9f526052bb4da5acd98692b3587160be164b3a059db` |
| `Docs/Verification/AllotmentNotice/ControlledRender/notice-visible.png` | 120837 | `3fb8194882da152137cf2f8493b3800f8a3c937e138798e742ce44237af6b811` |

## Already safely preserved / duplicates

- All 269 members of `Docs/Archive/LocalWork-2026-10-13/historical-source-snapshots.tar.gz` were re-read and matched manifest lengths/SHA256. All 269 originals still exist; 268 are byte-identical, only the ProBuilder setting differs. All archive files exist in origin/main. Seven Blender `.blend1` backups and historical `.fbx/.asset/.patch/.py` files in Verification are already included; do not reimport them.
- Both untracked `Docs/RELEASE-CANDIDATE-2026-09-18T...md` reports still match archived manifest hashes exactly.
- Untracked `Docs/Verification/EmberAudioIntegration/EmberSpitAudioTests.cs.pending` is byte-identical to an origin/main ancestral `Assets/Tests/EditMode/Presentation/Rendering/EmberSpitAudioTests.cs` blob `301c0170cb57b229f59dac8a4739b19f62e4be32`.
- The two untracked preserved `SampleScene.unity` files under EmberAudioIntegration and StarterSpellAudioIntegration are identical to origin/main ancestral `Assets/Scenes/Main/SampleScene.unity` blob `f79aa18c200ee583eb05291aaf7951dc1ecf0ad7`.
- `Assets/Scripts/Editor.meta` describes an empty folder; no tracked/unique script is omitted there. Two `Assets/InitTestScene*.unity` pairs are generated test scaffolding, not actual gameplay scenes. Preserve recovery scenes separately while editor recovery is active; ignored `Assets/_Recovery/` is not cleared by this audit.
- All 138 files under `_to_delete/git-stale-locks/tmp_obj` decompress to valid Git objects (52 trees,66 blobs,20 commits), and each object ID already exists in the repository object DB. Three other stale lock files are zero bytes. Not needed for source export; no deletion performed.
- Existing archive also preserves the four ignored SteamDeckDiagnosticCompiler source/config/meta inputs; do not confuse ignored Builds with exclusively generated data.

## Branches and worktrees

- Exactly 26 commits remain absent local fetched remote refs, exactly the same four branch tips covered by the prior `local-only-commits.bundle`. `git bundle verify` passes. No newly unbacked local branch commit found.
- All six secondary worktrees checked. Four clean. Friendly-robinson retains only the exact archived comment patch; vibrant-mirzakhani retains only the two byte-identical archived Qud reference docs. No new source omission in these worktrees.
- Keep the separate remote-backed `qud` reference branch separate; no merge of old release branch or archived drafts is warranted.

## Scope limits

- Archive verification demonstrates recoverability, not runtime validation or that historical drafts still compile.
- Current Deck build selection/deletion safety, Unity recovery and actual screenshots belong to root. No build or cache was changed here.
- Ignored paths inspected for authored script/model inputs under Builds and recovery presence; this is not a full bytewise audit of every ignored/generated editor file.
- Remote freshness requires root fetch/push verification; local `origin/main` equality alone is not a new network acknowledgement.
