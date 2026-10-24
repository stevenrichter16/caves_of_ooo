# Independent works-products reader review — 2026-10-24

Read-only review of the five supplied actual Game captures and their matching observational JSONs. Read `CLAUDE.md`, the art-direction/graphics-delivery context, `Docs/LIVE-16X24-WORKS-PRODUCTS.md`, and the native source record. No Unity tools, ordinary input, repository writes, new art, capture regeneration, or evidence mutation were performed. This report is the only authored deliverable.

## Result

**No actionable P1/P2 visual defect found in these five item readers.** All five portraits preserve the approved source silhouette and exact RGB colors at an integer display scale. Titles, descriptive text, stack information, separators, page indicator, and Close/Back controls remain inside their popup without overlap or clipping. The low lichen crust, upright detached sprig, hooked burr, berry cluster, and small coin remain distinguishable. This accepts only the pictured reader presentation and the bounded custody observations below.

## Source and pixel comparison

Compared directly against `ArtSource/Live16x24/north-works-products/north-works-products.png`, not the enlarged contact as a substitute. Native dimensions: 112×24; seven 16×24 frames. Source PNG SHA256: `9a4e06fdbb55242a66101df145e83e034125f5fd9ce8b2f02f4907fe055250a9`. `Assets/Resources/Sprites/Live16x24/north-works-products.png` is byte-identical to that source.

Every reviewed PNG is actually 1280×800. The JSON's `editorReportedScreenSize: [2321,1139]` is a separate reported editor value, not the saved image dimensions.

Bounds below are zero-based, top-left, right/bottom exclusive, in saved PNG pixels. Exact comparisons include every rendered opaque pixel plus the source's transparent interior positions, which remain popup background RGB (13,13,13).

| Reader suffix | Native frame | Portrait occupied bounds | Integer scale | Opaque native/display pixels | RGB max delta / changed opaque pixels |
|---|---:|---|---:|---:|---:|
| `coin-reader` | 4 | (705,287)–(723,305) | 3× | 32 / 288 | 0 / 0 |
| `berries-reader` | 3 | (956,300)–(984,328) | 4× | 30 / 480 | 0 / 0 |
| `mendleaf-reader` | 2 | (952,320)–(984,364) | 4× | 30 / 480 | 0 / 0 |
| `stoneburr-reader` | 0 | (956,336)–(984,364) | 4× | 28 / 448 | 0 / 0 |
| `lichen-reader` | 1 | (956,273)–(984,293) | 4× | 25 / 400 | 0 / 0 |

All reader filenames have prefix `Docs/Verification/Live16x24/works-products-uses-` and extension `.png`; each has a same-name `.json` receipt.

The coin is also visible in the world at occupied bounds (342,177)–(348,183). Its 6×6 opaque silhouette exactly matches the native coin mask, including the four transparent corner pixels. World colors are scene-processed and differ from the atlas, so the exact-RGB conclusion applies to portraits, not world rendering. The small gold disk remains discernible from the green ground and neighboring actor. The four forage screenshots show inventory readers, so they do not establish those items' loose-ground contrast.

## Custody and readable semantics

All five receipts report seed 64, zone `Overworld.11.9.0`, flat presentation true, HP 40, and `observational: true`.

| Reader | Tick / position | Pictured and recorded custody | Visual assessment |
|---|---|---|---|
| Coin | 1840 / (20,5) | Popup says `gold coin (x4)`; visible ground `GoldCoin` owner at (21,5); no carried GoldCoin; drams 36. Log shows destroyed wooden crate and spilled contents. | Compact ochre disk with quiet inset; correctly a single illustrative coin while text carries quantity. |
| Berries | 2330 / (60,2) | Inventory reader title `wild berries (x2)` agrees with carried `WildBerries` count 2. | Muted red fruit lobes and dry stem remain clear. Reader exposes Eat and its 1d3 HP behavior. |
| Mendleaf | 2610 / (55,18) | Inventory reader title `mendleaf sprig (x2)` agrees with carried `MendleafSprig` count 2. | Open two-leaf silhouette and thin cut stem remain visible on the dark reader; not confused with the low lichen crust. |
| Stoneburr | 2660 / (59,18) | Inventory title `stoneburr seed` agrees with carried `StoneburrSeed` count 1. | Small angular ochre burr, distinct from the round coin. Pictured inventory action is Examine; no planting capability is implied by the UI. |
| Lichen | 2710 / (63,18) | Inventory title `frost lichen` agrees with carried `FrostLichen` count 1. | Low sage crust is visible without glow or crystal shape. Longer tactical-use prose wraps within the reader and keeps the footer legible. |

Supplemental read-only JSON checks: `coin-taken` at tick 1850 records drams 56, no remaining ground GoldCoin, and no carried GoldCoin, consistent with converting the four pictured coins into +20 drams. `berries-eaten` at tick 2340 records WildBerries count 1 versus count 2 in the reader/harvested receipt. HP stays 40, so this evidence establishes unit consumption, not a demonstrated healing increase. The `*-harvested` receipts agree with carried counts 2 berries, 2 sprigs, 1 burr, and 1 lichen at the corresponding reader ticks. These are corroborating snapshots, not a claim that this reviewer independently operated or watched the inputs.

## Honesty bounds and remaining gates

**Can verify (script-observable in saved evidence):** native/resource byte identity; actual PNG dimensions; five exact portrait RGB/shape comparisons; native world-coin silhouette; recorded location/tick/stock/drams; coin stock removal and +20 drams across the two supplied snapshots; one berry removed across its snapshots.

**Can verify visually from these saved images:** readable full reader layouts, distinguishable portraits, quantity rendered as text, ground coin visibility, and inventory-context forage readers. No significant portrait scaling, tint, clipping, or silhouette defect is shown.

**Cannot verify:** the continuous input sequence, runtime uniqueness of item IDs, concentration/preparation success, concentrate portrait, save/load correctness, stable identities across serialization, use of the newly brewed flask, ground contrast of the four forage products, motion/feel, active frame cost, full-route completion, normal-release activation, or physical Deck acceptance. These reader receipts contain blueprint/count summaries without entity IDs; they cannot establish unique identity or preparation addressability. The reported duplicate-loaded-ID blocker remains a separate gameplay issue; this art review does not accept or waive it. `ConcentratedMendleaf` is absent from the reviewed reader inventories, and no concentration or save/load gate is closed here.

## Image hashes

- coin: `015af272e93b575ab3d4b41f728d8beec97c014923c472c6a2da0cb20a032f47`
- berries: `98710b6c13e0e4bffd64de10d8908d045904f498c78daf3f3fbddab34a842e49`
- mendleaf: `1e6a58bc433b2d1ee5cbb162263dfec374b2a1f72fea39f966561ca1dead33a4`
- stoneburr: `43af0020cd65c727a328ea0db6be3761e776e59dee68a85c321414a5fc470511`
- lichen: `69afa9d8eb1e1af5725749469d7fc445ddc70df1b1f0a62eb5a4ab9f82209d6f`

## Post-fix concentration, mending, and save/reload addendum

Independently reviewed the later `works-products-uses-fixed-concentrate-{offered,reader}.png` and `works-products-uses-fixed-mending-{preview,reader}.png` Game captures, their JSONs, the intermediate `fixed-concentrated` and `fixed-mending-brewed` receipts, and `fixed-save-comparison.json`. Also read `fixed-mendleaf-harvested`, `fixed-saved`, and `fixed-reloaded` JSONs to independently check the transaction and comparison. No Unity, inputs, repository writes, test reruns, new art, or modified evidence.

**Pre-/post-fix separation:** the first five portraits above are from the earlier run before the save identity fix. Their exact-pixel and visible-layout results remain valid. They are not relabeled as post-fix transaction or save proof. The new `fixed-*` series is a separate continuation reported by the root agent as starting from the untouched baseline after the fix. This reviewer establishes what those supplied captures and receipts show, not the baseline's history or the fix's code correctness. The earlier unresolved concentration/save statements are superseded only by the bounded new evidence below; historical colliding saves are not declared repaired.

### New native portraits

Compared again to the same accepted 112×24 atlas, SHA256 `9a4e06fdbb55242a66101df145e83e034125f5fd9ce8b2f02f4907fe055250a9`. Both screenshots are 1280×800. Zero-based occupied bounds, with right/bottom exclusive:

| Capture | Native frame | Portrait bounds | Scale | Native/display opaque pixels | RGB max delta / changed opaque pixels |
|---|---:|---|---:|---:|---:|
| `fixed-concentrate-reader.png` | 6, ConcentratedMendleaf | (956,340)–(984,364) | 4× | 32 / 512 | 0 / 0 |
| `fixed-mending-reader.png` | 5, BrewedTonic | (956,205)–(984,257) | 4× | 65 / 1040 | 0 / 0 |

The source's transparent interior positions remain RGB (13,13,13), so neither portrait has a stray fill or fringe. The concentrate is a compact matte folded olive wad, distinct from the earlier open sprig and low lichen. The tonic is the approved neutral opaque stoppered flask. The reader text carries its healing payload; the artwork adds no healing symbol or vessel contents. Neither title nor description, mechanical text, footer, nor portrait clips or overlaps. **No new actionable P1/P2 visual defect found.**

PNG SHA256 values:

- concentrate reader: `7dd7ef45275665292aec37d6b90df06a00dd60816ea48cb3157f2538a1306fd0`
- mending reader: `f8b2e094b8854a3af27be83e888546768e2c704db235907741809fcdc7d15414`

### Preparation and brewing evidence

All new reader/menu/preview receipts report seed 64, `Overworld.11.9.0`, flat true, player (62,18), HP 26 and 36 drams. They include carried entity IDs, unlike the earlier five reader receipts.

- `fixed-mendleaf-harvested.json` records two MendleafSprig units under ID 7202 at tick 2720. At tick 2790, `fixed-concentrate-offered.png` visibly presents `still: 2 mendleaf -> 1 vital:2` for `mendleaf sprig (x2)`. The matching receipt retains ID 7202 count 2.
- `fixed-concentrated.json` at tick 2800 removes the sprig entry and records one `ConcentratedMendleaf`, ID 7204. The `fixed-concentrate-reader` shows that named carried product, its native portrait, and prose explaining two sprigs reduced to one portable measure for a 2d4 healing draught. This supplies the previously missing actual concentrate output and reader evidence.
- `fixed-mending-preview.png` selects concentrated mendleaf in the brew mix and displays mending tonic, Healing 2, form tonic. The receipt still carries concentrate ID 7204 before the brew.
- `fixed-mending-brewed` and `fixed-mending-reader` remove the concentrate and carry one `BrewedTonic`, ID 7205, display name `mending tonic`, `healing: "2d4"`. Its brewForm and brewEffects are null, consistent with the documented healing-only path. The actual reader explicitly says 2d4 HP and exposes Drink/Throw actions. The reader is valid without an owned BrewItem form because the existing route is healing-only.

These observations support the ordinary menu offer, 2→1 preparation outcome, concentrate custody/portrait, concentrate-to-tonic conversion, and exact tonic reader payload in this new run. HP remains 26 in these snapshots; drinking/healing or throwing this tonic was not demonstrated here.

### Independent save comparison

Read both `fixed-saved.json` and `fixed-reloaded.json`, rather than accepting the summary booleans alone. Independently confirmed exact equality of seed, tick, drams, zone, flat flag, player position, HP, and all 12 carried inventory rows including their IDs, counts, display names and brew/healing fields. The retained new tonic is ID 7205, count 1, healing 2d4. Both snapshots remain tick 2800, position (62,18), HP 26, 36 drams.

The 253 current-zone owner rows are **equal as canonical row multisets**. Their raw list order changes after reload; there are no changed row values, missing IDs, or additional IDs. IDs are unique within each current-zone owner list, within the 12 carried rows, and across the combined captured current-zone-plus-carried set. This corroborates every bounded check in `fixed-save-comparison.json` while explaining why an order-sensitive comparison of the raw owners arrays returns false.

**Updated can verify (saved evidence):** the new native concentrate and flask portraits; preparation offer; the observed 2 sprigs → 1 concentrate → 1 mending-tonic stock/ID transitions; healing 2d4 metadata and visible reader text; and equality of the captured current-zone/carried fields across this reported F5/F6 checkpoint. No new native test count is claimed by this reviewer.

**Still cannot verify:** complete save-file byte equality; unrecorded fields or other loaded/unloaded zones; a globally collision-free graph; repair of previously colliding historical saves; every future load path; the continuous input sequence; tonic consumption/healing/throwing; ground contrast of carried products; active timing or physical Deck behavior. The supplied save comparison expressly has the same current-zone/carried-field scope. This addendum closes the earlier missing concentration evidence and supports this bounded in-session checkpoint, not blanket save-system or whole-route acceptance.

## Final independent consistency review

Read the two production diffs (`EntityFactory.cs`, `SaveSystem.cs`), the focused fixture and existing fixture cleanup, `Docs/SAVE-ENTITY-IDENTITY-RESTORE.md`, the latest works/art/completion continuations, release coverage, capture index, native XMLs, both profiler receipts, cleanup, checkpoint, and mending-use evidence. No repo writes, Unity calls, input, or tests were run by this reviewer.

- All **37** indexed image/receipt pairs exist. Independently recomputed PNG and JSON SHA256 values match the index; every image dimension, zone, and tick matches its indexed metadata. Images are unique and actual dimensions are 1280×800.
- Native XML hashes match the test summary. RED is 12 cases with 6 passes/6 failures; focused GREEN is 12/12; regression is 220/220. The two passing case-name sets have zero overlap: **232 distinct passing cases**. Zero-discovery and connector timeout responses are correctly excluded from success evidence.
- The short profile is 22.851847 seconds, 22,141 recorded samples, capacity 32,768, unwrapped. Documented renderer/main-thread/draw-call summaries match unit conversion and rounding; the 944.56 ms main-thread maximum remains disclosed. The 129.391008-second earlier recorder wrapped, and docs correctly limit its statistics to the retained tail. Neither is presented as Deck performance, a whole-window no-stall result, or a controlled speedup.
- All Markdown reference targets in the four reviewed living documents and all direct Docs/ArtSource paths in release coverage exist. Historical pre-fix identity/transaction evidence remains separate from post-fix concentration/save proof. The current-zone owner multiset and carried-field save bounds are preserved, including historical duplicate-save non-migration.
- No material production code mismatch found: loaded positive numeric identities reserve the existing decoded graph before load hooks; reservation is monotonic and occurs after parser/plan validation; saved IDs are not rewritten; saturation uses GUID-N allocation. The focused fixture's borrowed SeedPart factory is restored by its existing HotbarSaveFixture wrapper.
- Also inspected `fixed-mending-used.png` and its receipt: the screenshot log says “You heal 7 HP” and “You drink mending tonic”; HP is 33 versus 26 at reload, tick is 2810, and the ID 7205 tonic is absent from inventory. This adds bounded actual consumption/healing evidence to the earlier review, which had stopped before use. It does not establish throwing or other applications.

The initial stale coverage entry for FrostLichen/Mendleaf harvest was removed and normal-build wording was qualified as the initial packet's historical behavior. The remaining ambiguous sentence “No ... world mutations” was identified because ordinary combat/harvest/brewing do mutate game state; root applied the precise correction to “No grants, teleports, forced ticks or injected world mutations,” verified in the final source read. With that wording correction, **no material overclaim, evidence-path error, count mismatch, or unresolved visual defect remains in the reviewed completion scope**. Full saves, all products/applications, all-seed routes, the complete six-site release, packaged activation, and physical device acceptance remain outside the stated bounds.
