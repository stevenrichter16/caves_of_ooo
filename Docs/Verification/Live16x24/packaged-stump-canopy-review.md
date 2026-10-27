# Stump canopy package — independent visual and evidence review

Reviewed 2026-10-27. Source receipt: `3bafb1ee3445fdbf777a9ac3f51f831d407c4d0c`.

**Bounded pass.** All seven actual Mac window captures were opened and inspected. The three native species and their reader portraits remain recognizable, the sheltered Sentinel remains selected above its Tank, and the saved/restarted world view is unchanged. No significant new visual or receipt-consistency defect was found in this reviewed scope. This is not whole-area polish or all-world coverage acceptance.

## Actual Mac views

The JPEGs are 1280×856, including the 56-pixel window title bar; they are not 1280×800 game-only PNGs. Root reports launching the exact newly built Mac executable as PID 97709, continuing earned `stump-canopy-after74`, then fully quitting and launching again for capture 07. Process identity and control history are root observations; this reviewer inspected the resulting images and records without operating the app.

| Capture | Visible evidence and judgment |
|---|---|
| 01 Continue | Summit player focus `(50,2)`, HP 29/40, the native Sentinel near the solid mass and the three compact Singer bodies are visible. This matches the supplied earned starting state without a new layout or palette discontinuity. |
| 02 sheltered Sentinel | Reader selects `brocchinia-sentinel` at `(54,5)`, HP 10/10, Hostile. Contents include Pink stone, tank-brocchinia and the creature. The selected world body remains a lizard; the native portrait has an intact tail, legs and yellow eye. It is not replaced by the underlying plant. This verifies this instance of selection priority, not simultaneous display of every underlying Tank pixel. |
| 03 Rim travel | Following reported normal world-map travel from Summit `(3,4)` to RimForest `(2,3)`, player focus is `(40,12)`, HP 29/40. Singer, Sentinel and pale-winged Sky-Sari bodies are visible on the existing floor. The smaller foliage-colored bodies remain compact and distinct from the round neighboring vegetation. |
| 04 Singer reader | Reader identifies Summit Singer at `(25,13)`, HP 8/8, Hostile. Its squat silhouette and dark folded W agree with the body visible in capture 03. The enlarged portrait makes that marking clearer than the world-size body. No text/portrait collision is visible. |
| 05 Sky reader | Reader identifies Sky-Sari at `(64,16)`, HP 58/58, Hostile, while the corresponding selected world body remains visible to the right of the panel. Pale wings, dark interiors and narrow central body agree with capture 03. The full silhouette fits the portrait without clipping or tall-cell stretching. |
| 06 saved / 07 restarted | Player focus remains `(40,12)`, HP 29/40, with the same visible creatures, vegetation and terrain. Independently decoded and compared RGB rectangle `[0,96,940,696]`: **all 564,000 world pixels are identical**. The title bar, hotbar and sidebar/log are excluded from this equality claim. |

The retained `Fixtures/packaged-stump-canopy75/Quick.json` independently records `Overworld.2.3.0`, turn **9100**, HP **29/40**. Root reports ordinary F5 followed by full quit and fresh Continue. The image sequence supports the visual result; it does not independently establish every serialized field or every input action.

The native bodies and square portraits show no obvious new black tile backing, displacement, stretching or UI overlap. JPEG capture can soften edges and cannot establish byte-exact source-pixel preservation. Large plain floor intervals, repeated small vegetation, restrained solid contrast and existing uncovered glyph/black-backed props remain visible. These are still separate composition and coverage debts; this package does not make RimForest or Summit fully polished. Unseen/fogged cells are not counted as bare terrain.

## Build and archive evidence

Read the Mac/Linux build reports, Linux build-info and archive receipt. Independently checked that both build-report hashes and embedded summaries match the files, that separate build-info equals the archive's embedded build-info, and that source hashes agree with the restoration record.

- Mac `StandaloneOSX`: succeeded, **56.207 seconds**, zero errors, four warnings; started `14:40:49.559712Z`, completed `14:41:45.766725Z`.
- Linux `StandaloneLinux64`: succeeded, **44.341 seconds**, zero errors, four warnings; started `14:41:56.073673Z`, completed `14:42:40.414736Z`.
- Both outputs identify fresh `2026-10-27-stump-canopy-3bafb1ee3-current` directories, and their starts follow the recorded `14:40:43Z` source/build boundary. This is receipt consistency, not independent source-to-binary reproducibility or Git verification.
- The actual archive was read to recompute its size and SHA-256: **128,790,824 bytes**, `ed1fefa8d88d2837746f57443444a64c61dd04985596d9a25091605eebe37ab7`, both matching the receipt.
- The archive receipt records **189 regular files / 202 members / 13 directories**, complete hash/size/mode coverage, safe paths, internal checksums and gzip footer/CRC verification through EOF. Root reports a separate independent archive PASS. This reviewer checked the receipt and actual archive digest, and did not repeat the separate member-by-member stream audit.
- The pinned native receipts are the already reviewed **19 focused + 266 related = 285 distinct passed identities**, with no overlap, skips, failures or inconclusives. Earlier material/first-five execution totals are not added. Tests were not rerun by this review.

## Restoration evidence and limits

Read `packaged-stump-canopy-restoration.json`. It records **50,825 tracked files** and **44 preexisting dirty files** preserved, with tracked bytes/modes restored to the prebuild snapshot. Only four named build-generated settings were restored. It reports normal Editor and standalone save roots equal, both logical preference domains equal, and the previous current Mac tree, Linux tree and archive preserved. Test session data was archived; no storage deletion was performed. This reviewer checked the record and its consistent source binding, not a fresh independent reread of all 50,825 files or private preference values.

This actual Mac sequence covers the three new species' native world/readers, the selected Sentinel/Tank case, normal map travel and this saved/restarted presentation. It includes **no new actual damage, Wet or kill trial**; those cues remain separate native-test/earlier evidence. No Linux execution, physical Steam Deck play, sustained performance, complete Stump fauna, all-seed/all-area coverage or whole-chunk fullness is certified. MawToad, corpses, other world families and the dedicated area polish pass remain open.

## Inspected image hashes

| Capture | SHA-256 |
|---|---|
| `packaged-stump-canopy-01-continue.jpg` | `ca2c3b05f6590890ae96ada5e8cf8560888361c08dba23388de43afbe3937240` |
| `packaged-stump-canopy-02-sheltered-sentinel.jpg` | `368a49add7897d10932cacfa2362bd544410a7aa79bf6c2ac997002c845a2649` |
| `packaged-stump-canopy-03-rim-travel.jpg` | `96b21d437a6e46e6ecc8d6e698e3ba26028a23c7c9177d914eb1193e558917ce` |
| `packaged-stump-canopy-04-singer-reader.jpg` | `c8268fd9a4c69bdbcf1c3332780467b3c1b5b523b8c1c0543ce23bb6a8664422` |
| `packaged-stump-canopy-05-sky-reader.jpg` | `b15daeac1777a0e31131a37a51a289c2e387a48e0242c9a9baa61ab8c97fb02f` |
| `packaged-stump-canopy-06-saved.jpg` | `7f345af77174c14d19fdd5e9565b9c4b9545a7eb254a32ffb702317c51cf5593` |
| `packaged-stump-canopy-07-restarted.jpg` | `53618a50e894c834a8fbc6df392832d0bca8707f71917e11c9c7653393c568d5` |
