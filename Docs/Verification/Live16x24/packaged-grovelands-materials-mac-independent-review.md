# Packaged Grovelands materials — independent macOS evidence review

Reviewed 2026-10-27 by the delegated `world_coverage_ledger` reviewer. This is a
read-only review of the root operator's packaged gameplay evidence. The reviewer
did not launch or control Unity/the player, change game state, or run Git.

**Verdict: PASS for the bounded packaged Fen crossing, truthful reader, player
foreground and persisted carried GroveRed presentation shown here.** No new
blocking visual finding was found in these captures. This does not accept the
whole Grovelands biome, all formations, chunk fullness, or the complete graphics
release.

## Inputs and integrity

- Receipt: `packaged-grovelands-materials-mac-play-summary.json`, declaring source
  `ab26e270cb376253c971a41c3449ecbbf4171444` and native macOS standalone.
- All eleven original JPEG captures `packaged-grovelands-materials-00-...` through
  `-10-...` were opened and visually inspected. They show the native application
  window, including its title bar, rather than a concept or Editor screenshot.
- Both complete logs were read:
  `packaged-grovelands-materials-mac-player.log` and
  `packaged-grovelands-materials-mac-restart-player.log`.
- Both files in `Fixtures/packaged-grovelands-water65` were checked against the
  receipt. The readable `Quick.json` records game
  `50e670c30b6d48feb82756df64bd1cbc`, zone `Overworld.0.8.0`, turn **4590**, and
  HP **38/40**, saved at `2026-10-27T10:07:02.7827610Z`.
- All **15** listed artifact byte lengths and SHA-256 hashes match: eleven
  captures, two player logs and two fixture files. This verifies receipt/file
  agreement; it is not an independent binary-to-source build attestation.

All paths above and below are relative to `Docs/Verification/Live16x24/` unless
otherwise stated.

## Observed gameplay

| Evidence | What the image establishes | Limit |
|---|---|---|
| `packaged-grovelands-materials-00-startup.jpg` | Ordinary native startup has flat 2D ground/actors and the save-detected Continue prompt. | The startup player is the initial preview state, not the earned Fen character. |
| `-01-fen-continue.jpg` | Fen terrain has connected teal water patches with restrained bank edges. The native player is visible at **40,15**, HP **38/40**. | The input turn **4510** comes from the receipt/earned fixture, not a visible turn counter in this frame. |
| `-02-water-reader.jpg` | With player focus **40,16**, Examine says **grass**, type **Terrain**, and twice reports **On the ground: water**, including water's electrical/fire consequences. | The small green glyph is the actual grass subject's fallback portrait. This is truthful terrain-plus-coating disclosure, not a newly invented water entity/portrait. |
| `-03-player-on-water.jpg` | The golden native player body remains visible above the teal water at **40,17**. Sidebar explicitly reports grass, player and water. | One covered water cell is inspected; this does not prove every actor/hazard combination. |
| `-04-opposite-bank.jpg` | The player has reached dry grass at **40,19**; the focus no longer reports water. Water remains visually continuous behind the player. | The receipt supplies the complete intervening input sequence; still frames do not measure movement feel. |
| `-05-return-saved.jpg` | Return to **40,15**, HP **38/40**, with **Game saved** visible. | Saved turn **4590** is independently corroborated by `Quick.json`, not printed in the screenshot. |
| `-06-inventory.jpg` | Inventory has **DMG / DEF**, long sword **1d8**, buckler **1 AV +1 DV**, and right-aligned **[E]** marks. The selected sword has a native sprite. | This reviews the visible page only, not all inventory actions or equipment combinations. |
| `-07-grove-red-inventory.jpg` | Selected **grove-red** has a native red product silhouette; quantity **1**, weight **1 lb**. Visible rows also show pale-salt **1** and gold coin **3**. | This is the carried result earned earlier. No fresh package harvest or product drop/pickup is depicted in this set. |
| `-08-grove-red-reader.jpg` | Full Examine retains the same native GroveRed subject image and shows **1 unit**, item type, weight and carry/throw details. | This does not verify every harvested-product identity. |
| `-09-restart-fen.jpg` | The post-restart world capture again shows Fen **40,15**, HP **38/40**, the native player, connected water and prior log state. | The fresh-process chronology relies on the operator receipt and the separately recorded second process log. |
| `-10-restart-grove-red.jpg` | The post-restart inventory again displays GroveRed **1**, PaleSalt **1**, GoldCoin **3**, and the native GroveRed image. | This image is byte-identical to `-07`; equality supports the same visible UI state, but alone cannot prove that a second capture occurred after restart. |

Taken together, the receipt, separate process log and post-restart world image
support the recorded cold-restart check. No exact runtime entity-ID persistence
claim is made from the packaged UI; those IDs were established by separate Editor
evidence. The receipt describes ordinary Continue, walking, context/Examine,
inventory/details, F5 and full process quit/relaunch. This review found no
contradiction in the supplied artifacts but did not independently observe every
input or process transition.

## Logs and remaining limits

Each log points to the same
`Builds/Verification/2026-10-27-grovelands-materials-ab26e270c-current/CavesOfOoo.app`
and records Unity **6000.3.4f1**, Apple M5/Metal initialization, a complete
GameBootstrap sequence, and Input System shutdown. Both load **712** blueprints,
**26** liquid definitions and **9** gas definitions. The two bootstrap records
have different generated-start-zone entity counts, consistent with separate
initializations before Continue.

No `Error` or `Exception` lines were found. The logs are **not warning-free**:
each contains six `Thread ... may have been prematurely finalized` messages and
one `GarbageCollector disposing of ComputeBuffer` message during shutdown. These
are retained as a lifecycle follow-up; this evidence does not establish their
origin, a leak magnitude, or a sustained-performance result. A bounded comparison
also confirmed the same six thread notices and one ComputeBuffer notice in the
earlier `packaged-harvest-products-mac-player.log`; these are not newly observed
with this delivery. They do not negate the visibly completed
crossing/save/restart sequence.

The visible Fen has recognizable water routes, flora clusters and borders rather
than an empty single-color chunk. Nevertheless, repetitive ground coverage,
remaining glyph actors/props and a blue-wave fallback patch are still visible.
This finite viewport does not accept whole-chunk density or completeness. It
does not demonstrate every material owner, all four formations, all **37**
ordinary Grovelands sites, underground/special sites, physical liquid-source
states, mixed/frozen/finite water, combat, or broader biome coverage. Source-wave
and other guarded fallbacks remain part of the documented incomplete scope.

No Linux execution, physical Steam Deck check, sustained profiling, frame-time
or zero-allocation claim follows from this review. Build/archive and normal-save
restoration verification are separate root-owned records.

## Review disposition

- No unresolved Yellow-or-higher finding in this bounded visual evidence review.
- Documented: duplicate inventory image bytes, truthfully limited reader/body
  coverage, nonempty shutdown notices, and incomplete regional/fullness coverage.
- Files changed by this reviewer: **this review only**. Runtime, tests, source
  artwork, the five frozen living records and build/restoration inputs were not
  edited.
