# Grovelands actors — bounded source and native review

2026-10-27. **No significant source/runtime defect identified in this bounded review. Ready for actual gameplay comparison, not area or package acceptance.** The reviewer read the saved production catalog, Greybox routing, original Shambler builder/cells/import metadata, source verification receipts and retained native XML. No Unity, native test, Git, art-builder or game operation was performed by this review. The reviewer authored the new fixture; its assertions are self-review rather than an independently authored test suite. A separate peer cold-reviewed fixture APIs and identified the audio callback isolation correction below. Production and native execution belong to root.

## Exact behavior reviewed

- `FlatPixelGrovelandsActorsCatalog` recognizes only registered **Shambler, Mosshulk and ChoirTendril**, with canonical glyphs `z`, `M`, `F`. Shared display names, SporeShambler, columns and corpse provenance do not grant a living body. It requires owned unique Render/Physics/Body/Brain, positive owned HP, a supported single-cell body, visible render, no custody/takeable/reskin and a valid optional stack. Shambler additionally retains owned unique CommittedMelee; Choir retains owned unique Conversation/Trader/Inventory. Legitimate damage, commitment stages, status effects, disposition, renamed display label and changed merchant stock/purse do not invalidate species identity.
- One cached 16×24 Shambler pair and two cached 16×16 pairs use centered FullRect sprites at 16 PPU. The three injected textures are independently optional; explicit null/malformed injection does not reload Resources. Disposal destroys owned sprites/tiles only. Ordinary queries scan existing parts without creating new sprites, dispatching events or advancing combat/trade.
- Greybox applies the exact known-role decision after other candidate artwork with its own output variable. Refusal is authoritative, so old loose local selectors cannot restore unsupported art. Current fallback selection supplies current graph/disclosure/owner authority; the authorized document renderer remains responsible for subject currentness. The semantic helper is not itself a new stale-coordinate authorization API.
- Only successful native actor resolution opts into the already-shipped single Render-event cue. No extra Render event, palette/grading change, regional scenery admission, AI, anatomy, population, collision, merchant or committed-melee production code was added by this actor adapter.

## Source and pixel review

The reviewer actually opened `shambler.png`, the three-backing `shambler-contact.png`, the original Mosshulk/Choir/SporeShambler PNGs and the actual pre-art Shambler reader capture. The original Shambler reads as a small drooping head, crooked former-person trunk, separate planted legs and one attached tapering tendril. It does not reuse SporeShambler's compact silhouette, add a held hook/cane or copy a spore cloud. Muted mauve/gray-moss separates it from the two green borrowed bodies. The green and reader backings are clear enough for a trial; the skinny extremities and lower contrast on stone still need normal-zoom graded gameplay inspection. No source revision is demanded before that evidence.

Independent read-only pixel checks found:

- Exact **16×24**, **95 opaque pixels**, **7 colors**, bounds **[2,5,13,23]**, baseline row **22**; all **384 RGBA pixels** match authored `source-cells.json`.
- All **73,728** checked contact pixels match direct integer 8× instances and the specified backing colors. The imported PNG is byte-identical to the source PNG.
- All **1,656** protected input hashes match at review time, including both reused actors and excluded SporeShambler. The builder constructs named original integer-cell Blender geometry; no raster is sampled as a drawing input. Pillow is used only for verification.
- The retained Blender inspection receipt reports **38 named mesh clusters / 95 unit quad faces**, exactly matching source part/palette coordinates. Its recorded `.blend` hash matches the current file. The reviewer read this receipt and code; the reviewer did **not** independently reopen Blender or rerun its verifier/corruption tests.
- The new import uses Point/Clamp, no mipmaps, no compression, no NPOT scaling, 16 PPU and no generated collider. Runtime creates explicit centered FullRect geometry, so legacy importer subrects cannot stretch the borrowed square sources. Metadata currently includes Unity-emitted trailing spaces; any later whitespace canonicalization must be recorded separately from pixel changes.

## Native evidence — root executed, reviewer parsed

| Retained execution | Actual XML result | UTC interval | SHA256 |
| --- | --- | --- | --- |
| `grovelands-actors-red.xml` | 44 distinct, 0 passed / **44 failed**, no skips/inconclusives | 15:51:57–15:52:01 | `1012c5186b98d95111deaf0588360a226bc2628a72efdaf7e0b5619a2fde7957` |
| `grovelands-actors-green.xml` | **44 distinct passed**, 0 failed/skipped/inconclusive | 15:55:50–15:55:55 | `c5132632f1b697ea2cebf78a364c3d0d9ac47971e93bbf0defa5ea23dabdcaed` |
| `grovelands-actors-related-green.xml` | **363 distinct passed**, 0 failed/skipped/inconclusive | 15:56:17–15:56:41 | `a6e08ab1c60b2962572aca64c8b965806ca0774bd4d4af03f93f4d59c47ac4fe` |

The two final GREEN groups have **zero identity overlap: 407 distinct passing cases**. Earlier executions are not added. The original failing fixture is preserved as `grovelands-actors-tests-before-implementation.cs.txt`, SHA256 `18ed6d0d7bb4b1bb15845fbb18408f3f45f711e61a3e7fc9904d830202977055`. The reviewer compared it with the current fixture: the only post-RED test edit captures, nulls and restores `WorldAudioCombat.Callback`, protecting a borrowed audio observer during real committed melee. No assertion or case count was weakened or changed after RED.

The 44 cases cover real factory owners; all three submitted bodies and authorized readers; portable mutable states; exact service/body ownership; semantic impostors; fog/current graph/custody/footprint/selection; movement/removal over nonnull ground; independent missing/malformed resources and owned-pair disposal; actual damage and applied Wet with single-event cues; three actual deaths with separate corpse identities; genuine Shambler `TryBegin`, fixed-ray resolution/recovery, blocked-ray refusal, displacement cancellation and hidden-ray cues; and real Choir Chat, hostile-speech permission, StartTrade choice and stock/purse transfer. They load actual RotChoir/ChoirStock and restore the scoped globals. A programmatic trade with test currency is not ordinary acquisition or actual UI acceptance; native cue geometry is not a visual-readability verdict.

## Reviewed file identity

| File | SHA256 |
| --- | --- |
| `Assets/Scripts/Presentation/Rendering/FlatPixelGrovelandsActorsCatalog.cs` | `b07794afb94090ed51f8ff976c5b034cedffc90f0ea7ad274dea440a4f98fda5` |
| `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs` | `0227ce4f65a4fbb06295d35cc86a16159032d87be4957305b794456eee541587` |
| `Assets/Tests/EditMode/FlatPixelGrovelandsActorsTests.cs` | `26cfdf74e7f816c38625d3a1dab00adc96ee167c9e26220cecce1b1ffb38f58f` |
| Source/imported `shambler.png` | `e0d8be101e58460fb1a2e45503a2f7b1a729be838466a7468ec8d488a95e3287` |
| `Assets/Resources/Sprites/Live16x24/shambler.png.meta` | `9b0dcb42d6614fb41467209ec1e9867cff25055901aba7a914e8ec4cd3fcb8dc` |
| Original `mosshulk.png` | `b638b82cbcc405c27dbb470e61ed8597ecc32480daa21d09de354e527c4bc031` |
| Original `choir_tendril.png` | `0e7f78d44986d35fd34c46e6c163664afb53c2fb72b492ae7677a1e0b9551fe6` |
| Excluded original `spore_shambler.png` | `42c502b18880355f39df513248b17ab26ad61cff36d42f65f6832a283d715e42` |
| `Tools/Live16x24/build_shambler.py` | `7ee4fbd4ef5d967c26ed63abcb34859bc6388e7e2b18a350adaa10f3d7ce3084` |
| `source-cells.json` | `84811c1e063ca516c1f0fc3954c7a9597729d90dcbb1662c450cc09339d461b9` |
| `manifest.json` | `e3fad9c91334e67ff71d9c0cb5dde28a9b97b58d309f57b7f8ab21a9c2077f8e` |
| `source-verification.json` | `02bac28a403aa9bda117c3220bdf9d66ab102cfb6e56cc4ccfe742b1e1afc2b0` |
| `blend-geometry-verification.json` | `8e1a2af5f1988b6939b0d9f64eaa568645de78011c7626164431a28b16214f44` |
| `shambler.blend` | `502a8a2da0f873d473b403ff58c414026ed1bc020eb93793360f3dcc0962f247` |
| `shambler-contact.png` | `cd10c4c200720cccf040df3223e3ae4af1e882d9fc119a437e81103d22235105` |

## Pending acceptance

Actual post-art world/readers, Shambler warning/recovery visibility, normal Choir interaction, travel/restart and packaged execution have not been reviewed here. Root is performing those separately. Source/contact/native success does not accept all Grovelands actors, any full area, whole-chunk fullness, special sites, sustained performance or physical Deck behavior. Existing unmapped bodies/props/crops/corpses and the later serious biome/area polish phase remain explicit work. No new after-gameplay or package acceptance is inferred from this report.

## Actual Editor comparison — final bounded review, 2026-10-27

**The three-body integration passes this bounded actual-gameplay checkpoint. This does not accept regional completeness or whole-area polish.** The reviewer opened all 15 `grovelands-actors-after-01` through `15` captures, the updated after-receipt and profile record. Root performed the ordinary controls, state inspection, restart and profiling; this reviewer inspected the retained evidence without operating Unity. This section supersedes the actual-Editor items listed as pending above; fresh packages remain pending.

- **Shambler:** the two Grove owners read as slim, crooked mauve bodies against the green field, distinct from columns and the two green species. The reader shows the same attached-tendril silhouette without a held hook. Captures 03–05 retain the existing warning text/intent marks, the normal sidestep and explicit recovery description. The receipt records two real windups, then recovery with the player outside both threats and HP still 23. The static body remains unchanged across these states; this is not evidence of a new windup animation.
- **Mosshulk and Choir:** Fen world bodies match their readers. Mosshulk is a broad, low green mass with bright eyes; Choir is a narrow branching form. Their original square geometry remains square in the reader. Both are less conspicuous on green terrain than the Shambler; their small world silhouettes and foliage-colored details remain a later contrast/polish concern, not an identity or geometry failure in these views.
- **Actual services:** capture 09 shows normal Choir conversation with the native portrait and Trade choice. Captures 10/11 show player 50 / merchant 100 and three stock entries; capture 12 shows player 32 / merchant 118, the venom gland transferred and two merchant entries remaining. These support the root-recorded ordinary 18-dram purchase. Capture **11 is fully readable and byte-identical to 10**, SHA256 `78ea57b772b05701114fc392ff91034926ce9222f96db5a487065ad7a1e505d7`: it is duplicate unchanged-UI evidence after cancellation, not a distinct transaction view. An earlier reviewer report that 11 was black was a tool-display error; reopening the exact file and checking its bytes corrected that finding. The control history of opening then cancelling confirmation is root's observation, not something the duplicate alone proves.
- **Named UI debt:** the left faction label is clipped in the conversation panel and the greeting/choice layout is cramped. The trade list also truncates the long codex label. These are recorded presentation limitations; this actor slice does not repair them or claim the interface is fully polished.

The reviewer independently verified all **15 recorded capture hashes and lengths**. There are 14 files through the successful restart, with **13 unique file contents** because 10/11 are identical; capture 15 is separate later fatal-route evidence. Captures 13/14 have exactly identical decoded RGB pixels across `[0,0,940,680]`: **639,200 pixels**, excluding the log/sidebar and hotbar. `Fixtures/grovelands-actors-after81/Quick.json` records **Overworld.0.8.0, turn 10290, HP 23/40**. The restart receipt retains player `(20,16)`, Choir `51747` at `(19,17)` with HP 80, merchant purse 118, player purse 32, and InkVial/Codex11 stock. This is a successful saved purchase/restart checkpoint, not a new grant or reset.

The additional 12-key profiling route occurred **after** that checkpoint. Capture 15 visibly shows HP 0, the death prompt and a Mosshulk right-hand hit for **23 damage** after a left-hand miss. The receipt places it at turn **10398**; the living after81 checkpoint remains turn 10290. The fatal route is not hidden or represented as a successful traversal. It does not prove new actor-death, Wet or damage-flash interactions for these three bodies; those remain native-test coverage. The 60.091-second Editor profile contains 15,990 samples, including this combat/death transition, idle time and instrumentation. Recorded ZoneRenderer mean is about 0.152 ms, p95 0.161 ms and maximum 391.947 ms; the spike and sampling context prevent a quiet-route, sustained-FPS or performance-improvement claim. No matched pre-change profile, Linux play or physical Deck result exists here.

Whole-view composition is still incomplete. The Grove and Fen preserve useful open movement space, readable continuous water and local shoreline/column clusters, but also broad repetitive dotted ground, quiet underfurnished patches and remaining glyph-backed props/source cells. These captures establish viewport observations only; fog and undisclosed black regions are not counted as bare terrain. No full 80×25 census, all Grovelands areas, special sites or world-wide fullness promotion follows from this review.

Post-play read-only checks found the production catalog, Greybox routing, 44-case test file, imported/source Shambler PNG and import metadata still match the exact hashes above. **All 1,656 protected art-input hashes match** at this check. The source body and borrowed pixels were not revised during actual acceptance. The after-receipt reports clean stopped Edit mode, null save-root override, unchanged normal Editor/standalone saves and all 44 foreign files, with no storage cleanup; these are root's preservation checks rather than a new independent filesystem-baseline audit by this reviewer. No new significant source/runtime or actor-body defect was identified. Fresh packaged execution and the separate broader area-polish work remain open.

### Importer finalization after the checks above

Before source commit, root removed exactly six trailing spaces from the new `shambler.png.meta`. Its canonical SHA256 is now `7b40a20334c0218be0e51b743a58d60a10daa8c7bf86711b02390fccbf74155b`; the table above intentionally retains the native-tested Unity-emitted hash `9b0dcb42d6614fb41467209ec1e9867cff25055901aba7a914e8ec4cd3fcb8dc`. The reviewer independently reconstructed the old hash exactly by restoring one trailing space to each of the six empty `customData`, `indices`, `mipmapLimitGroupName`, `userData`, `assetBundleName` and `assetBundleVariant` lines. Thus no other byte change is hidden in this reconciliation; GUID, importer settings and PNG pixels are unchanged. The after-receipt records both hashes and now correctly classifies 11 as readable duplicate unchanged-state evidence. The appendix is frozen for the root-owned source commit.
