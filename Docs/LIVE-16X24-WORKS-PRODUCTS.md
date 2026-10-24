# Live16×24 — loose works products

## Status and bounded plan

**Current schedule,2026-10-24:** Unity and ordinary input have recovered.
Plot products now have partial harvest/pickup/carried-Examine acceptance;
the remaining plot loop comes before this packet's new-art forage/preparation/
brew/coin route. Works-product ordinary acceptance is still pending. See the
[scheduling continuation](#scheduling-continuation--2026-10-24) below.

Initial implementation record,2026-10-23: seven native sprites and runtime integration implemented; focused
native Unity GREEN, affected regression and independent cold review
completed. Ordinary Play remains unaccepted. The Mac remains
locked. Advance this independent packet from the already accepted actual
works harvest/brewing captures while retaining **both plot-product and
works-product ordinary-play gates as pending**. This changes scheduling,
not the requirement for real gameplay evidence. Do not call the north route
complete or add more unverified areas until these two live gates close.

Seven original native frames cover the six remaining identities in the
14-item direct-product ledger plus the previously omitted preparation
outcome. This is CoO-original art; Qud is a readability reference, with no
copied assets or implementation-parity claim. Normal builds remain unchanged.

## Verification sweep and corrections before production

| Assumption | Verified correction and consequence |
|---|---|
| Six remaining products total | Six of the original14 remain after the eight plot goods, **plus ConcentratedMendleaf**. WildBerries must not disappear from the accounting. Seven frames. |
| StoneburrSeed can be planted | Objects.json5932 has Reagent(binding), no SeedPart. Draw a hooked dry burr; no planting promise. |
| Harvest always drops items | HarvestablePart normally packs actual outputs into inventory; only overflow reaches ground. Tests must exercise the real path; no fabricated live pile. |
| FrostLichen is a crystal or standing plant | Exact loose Reagent(cold), distinct from FrostLichenPatch. Low detached crust; no glow/ice effect. |
| MendleafSprig and concentrate are tonics | Raw sprig is vital reagent; actual concentrate recipe consumes2 sprigs near a still and produces1 vital reagent with higher authored potency. Neither is a drink. |
| WildBerries is prepared food | Owned Food only; Cooking=Meal is flavor, no Cookable. Small raw cluster; no healing-strength promise. |
| GoldCoin pickup creates carried stock | Ordinary PickupCommand converts quantity to5 drams/unit and removes the coin, independent of mutable Commerce.Value. Art must vanish with the actual source, not invent a carried coin. |
| Every BrewedTonic has BrewItem/Form | Healing-only ConfigureBrewItem in BrewingService320–365 adds no BrewItem. Exact owned Tonic is enough for a neutral flask when that part is absent. Do not require healing or Drink to select it. |
| All explicit brew forms are flasks | Existing SpreadPortableRecipes352 distinguishes Food. With an owned BrewItem, only canonical Tonic/Coating/Throwable use this flask; explicit Food/unknown/foreign parts fall back. No effect/safety/color inference. |
| Healing Food always retains form | Existing healing-only Food outcome also loses Form at configuration. Record this pre-existing gameplay metadata gap; do not redesign brewing during an art packet or claim complete food coverage. |

Reviewed exact blueprints in Objects.json; HarvestablePart; ordinary
PickupCommand; ReagentPart and its cached parsed properties; TonicPart;
BrewItemPart; BrewingService; Preparation recipes and existing portable
visual mapping. Static source art follows only actual identity/semantics.
No new inventory, material, brew recipe or world mutation is introduced.

## Frame and runtime contract

One independently optional borrowed atlas `Sprites/Live16x24/north-works-products`,
112×24, seven16×24 frames in this exact order:

|Frame|Exact owner|Owned semantic check|Native silhouette|
|---|---|---|---|
|0|StoneburrSeed|Reagent with positive binding|Compact hooked ochre burr|
|1|FrostLichen|Reagent with positive cold|Low irregular cold-sage crust|
|2|MendleafSprig|Reagent with positive vital|Short detached broad-leaf sprig|
|3|WildBerries|Food|Small muted red fruit cluster|
|4|GoldCoin|Commerce, no fixed-price check|One small ochre coin with quiet square inset|
|5|BrewedTonic|Tonic; optional owned known non-Food BrewItem|Opaque neutral stoppered flask|
|6|ConcentratedMendleaf|Reagent with positive vital|Compact matte folded leaf wad|

All require owned visible Render, no explicit VisualID/VisualVariant/
GlyphVariants, owned takeable nonsolid Physics, Item without actor/terrain/
crop roles, and a valid owned positive stack when present. Current world
cell/custody and carried document authority remain in existing callers.
Raw string order/potency tuning and mutable price must not change identity.
Missing/malformed resources retain truthful fallback and every prior group.
Earlier1–11 texture injection overloads must not fetch the new resource.
Sprites/tiles are cached and disposed independently of borrowed textures.

## Pipeline and test gates

1. Reuse actual unchanged works geometry and ordinary captures
   `north-works-stoneburr-harvest.png` and `north-works-batch-brew.png`.
   These show real source removal and finite brew output, **before new art**.
2. Imagegen reinterpretation: `concepts/works-products-v1.png` plus exact
   prompt; accepted `items/opening-items-contact.png` supplies scale/palette.
   Reject generated shading/fringes, the cross-like coin inset and oversized
   detail. Native parts use3–6 flat colors and compact connected silhouettes.
3. Independent source verifier RED before original integer-cell editable
   Blender geometry; direct native/contact render. No generated pixels imported.
4. Native selector/resource/custody/use fixture RED before runtime edits.
   Root owns all Unity runs. Actual harvest→pickup, currency conversion,
   and brewing tests; counter-check foreign/missing/form/state/ownership.
5. Minimal twelfth independent atlas integration; import exact source bytes
   with Point/Clamp, no mips/compression/resizing. GREEN, bounded disjoint
   regression, independent cold review and warmed selector observation.
6. When ordinary input is available, exercise both pending product packets
   through normal game controls. Capture new actual Game images, compare,
   inspect native portraits, measure a bounded active frame window, then
   stop/restore. Never relabel old screenshots or source contacts as new Play.
7. Update living record, exact-owned commit, fetch/rebase and push main.
   Do not pause the seven-milestone heartbeat as complete.

## Evidence and review

Native source verifier failed before authoring for the five missing
production outputs (`source-red.txt`); final verification passes seven
connected, distinct silhouettes, binary alpha,3–4 flat colors, exact
112×24 cells and zero source/native/contact mismatches. Prior atlas bytes
remain unchanged. Native PNG SHA256:
`9a4e06fdbb55242a66101df145e83e034125f5fd9ce8b2f02f4907fe055250a9`.
`Tools/Live16x24/import_north_works_products.cs` imported those exact bytes
with Point/Clamp, no mipmaps/compression/resizing; receipt
`Verification/Live16x24/works-products-import.json`.

| Native Unity result | Evidence |
| --- | --- |
| RED:58 cases,4 passes/54 expected failures | `works-products-red.xml`:53 missing twelfth-constructor assertions and1 missing default resource. The4 old world-custody controls already pass. |
| GREEN:58/58,0 skipped | `works-products-green.xml`; real harvest overflow→pickup, ordinary coin conversion, six actual brewing outcomes and near-still preparation included. |
| Regression:338/338,0 skipped | `works-products-regression.xml`; plot products65, opening items46, works69, vegetation49, world catalog33, examine42+layout10+GPU color9, quiet-state cadence15. |
| **396 distinct passing cases** | `works-products-distinct-test-count.json` checks the final case-name sets are disjoint. RED is not added to this total. |

Before RED execution, independent review caught an unadmitted detached
north graph in the new fixture; it now uses the already admitted glade to
test location-independent item identity. That fixture correction is not
a production RED claim. The RED MCP job prematurely reported an
initialization timeout, but a later fresh matching58-case native XML
completed before production edits (22:13:10–22:13:12 UTC). That actual XML
is the RED evidence. During regression one MCP polling call timed out;
the native run subsequently completed338/338 (22:36:19–22:37:07 UTC).
No failure was removed from the recorded case set.

### Initial measurement and editor connection limit (recovered below)

The stopped-editor warmed selector probe in
`Tools/Live16x24/profile_north_works_products.cs` was requested twice after
the native suites completed. Both calls lost the Unity plugin connection;
`works-products-selector-disconnected.json` and
`works-products-selector-profile.json` preserve those failures. **At that initial checkpoint, no timing
or allocation result existed.** The snippet was retained to
rerun after reconnection; it would measure70,000 detached warmed selector
calls, not real rendering, draw calls, a speedup or Deck performance.

Final read-only editor-state and console queries were also unavailable.
`works-products-editor-state.json` records the disconnected response, not
a successful cleanup assertion. Unity's process remained alive and its
log showed reconnect attempts; no crash or completed probe is inferred.
This packet never launched Play or an isolated game. Source import and
final native XML completed before the transport failure. The next agent
must reread editor state before entering Play; do not inherit the previous
packet's cleanup receipt as a fresh result.

Twelve old import metas were restored only after proving linewise trailing-
whitespace equivalence to HEAD; see `works-products-import-meta-cleanup.json`.
Unrelated untracked/recovery files were preserved.

### Reconnected editor evidence —2026-10-23 22:47 heartbeat

The Mac remains locked, but the Unity tool connection recovered. Re-ran
only the saved stopped-editor selector probe; no new test suite, source
change or Play session. [The separate result](Verification/Live16x24/works-products-selector-reconnected.json)
records70,000/70,000 cached matches,655.6434ms total and0managed bytes,
after100 warmup passes over seven detached fixture owners. No owners were
placed in the live world. This closes the missing bounded selector
observation only: no before/after comparison, whole-frame improvement,
draw-call measurement, active action timing or Deck result is claimed.

[Fresh editor state](Verification/Live16x24/works-products-editor-state-reconnected.json)
confirms Play/pending/compilation/isolation/view flags false and original
SampleScene. Earlier failed receipts remain unchanged.
[The five-entry console sample](Verification/Live16x24/works-products-console-reconnected.json)
contains warnings in existing gas/plasma effects, an equipment audit,
GameBootstrap and Backstab; it is neither empty-console nor exhaustive
error evidence. Both new-item ordinary-gameplay gates remain pending.
Independent read-only probe review confirmed its isolation and measurement
bounds. This continuation changes evidence and docs only; the prior396 native
passes are not a newly run or additive count.

## Implementation-stage review and scope divergence — 2026-10-23

- 🔵 Independent source/art review and settled production cold review
  found no actionable P1/P2. The reused portable-item guard is equivalent
  to the prior plot guard; all65 plot cases still pass. Missing/malformed
  new resources preserve older groups, and disposal keeps borrowed
  textures alive. Explicit Food/unknown/foreign form counters remain.
- 🔵 One112×24 atlas and seven cached tile/sprite pairs; no gameplay edits,
  per-owner GameObjects, new observers or whole-zone scans. Reagent parsing
  uses its existing owner-local cache. Native source is editable Blender
  integer geometry; no generated image pixels were imported.
- 🔵 First native art pass accepted after root and independent visual
  review. Hooked burr, low lichen, open sprig, raw berries, small coin,
  neutral flask and compact concentrate remain distinct. No invented
  animation, vessel contents or readiness marker. Live contrast remains
  a separate check, especially lichen/concentrate.
- 🧪 Scheduling divergence: because the Mac remained locked, this
  independent packet advanced from accepted historical works captures
  before the previous plot-product ordinary gate closed. Both live gates
  remain pending. No new area is to advance before they close.
- ⚪ Existing healing-only Food loses its form in
  `BrewingService.ConfigureBrewItem` (320–365). This art patch records it,
  tests the actual outcome and keeps explicit Food fallback. Correcting
  upstream brewing metadata needs a separate behavior-first packet.

**Can verify:** exact native source, actual Unity import/default binding,
396 native cases including real transactions, independent code/art
review, the bounded warmed selector observation and recovered editor state. **Cannot verify:** new ordinary-input readability, new Game
screenshots, whole-frame/draw-call effect, physical Deck, release activation
or whole-route/whole-biome completeness. No new Play session was launched;
contacts and reused captures are not new gameplay acceptance.

## Next action

Ordinary input is available. Finish the remaining plot-product ground
Examine/prepare/plant route in [the plot-product record](LIVE-16X24-PLOT-PRODUCTS.md),
then the works forage/preparation/brew/coin route. Use the existing isolated seed64
launcher and ordinary controls, including normal drop and exact-subject
examine where a harvested item goes directly into inventory. Do not
inject items, fill inventories, teleport, or relabel historical captures.
Capture `works-products-*` Game PNGs with matching read-only receipts;
include a bounded active frame window and restore the stopped editor.
Only claim items actually acquired and exercised in that run. No additional
unverified art areas before these two live gates close.
Purchased crop families, grimoire, incidental surroundings, north actor
motion, east/kitchen, normal release activation and physical Deck acceptance
remain separate unfinished coverage.


## Files and implementation summary

- Two rendering catalogs: optional twelfth borrowed group, exact seven
  identities, unchanged shared item guards, cached frames and owned disposal.
- `FlatPixelWorksProductTests.cs` with fresh meta:58 native cases and
  paired state/ownership/form/visibility/resource counters.
- `ArtSource/Live16x24/north-works-products`: editable original source,
  native/contact outputs, manifest, verifier and source RED/GREEN evidence.
- Source generator, import and optional profile snippets under
  `Tools/Live16x24`; native resource PNG/meta; saved imagegen target/prompt.
- This record, main art brief, north-plot/plot-product handoffs and parts
  ledger; `Verification/Live16x24/works-products-*` native/import evidence.
  No new gameplay PNG, scene, blueprint or release-default change.

## Scheduling continuation — 2026-10-24

The editor recovery supersedes the locked-machine scheduling blocker above.
Source, saves and the selected current build were preserved before selective
cleanup. Official package-signature warnings in6000.3.4f1 remain an editor-patch
follow-up; they did not prevent the recovered ordinary route. Current stopped
[cleanup](Verification/Live16x24/g2-attribution-launch-cleanup.json) confirms
Play/isolation/artPending/viewBorrowed false and saveRoot null.

The new plot visit demonstrates paid rights, Pitchpod harvest yielding2resin
and1seed, pickup and a carried resin portrait. Its ground-examine filename
contains only a pickup list, and planting/clod preparation remain pending.
This does **not** supply ordinary evidence for any of the seven works-product
frames. Their earlier native396-case/source/import/profile evidence stays
valid within its original limits; no new test count is added by this update.

Continue the actual remaining plot loop, then acquire and exercise the works
goods through their existing source and preparation/brewing/coin mechanics.
Capture only actual outcomes and exact-subject ground/carried Examine where
appropriate. Do not add another unverified area or treat the north route,
six-site journey, packaged build or physical Steam Deck as accepted.

## First ordinary product evidence — 2026-10-24 15:00 UTC

The real plot journey produced one burning coating from harvested Pitchpod
resin through Crafting→Brew. [Actual carried reader](Verification/Live16x24/plot-products-uses-coating-reader.png)
shows this packet's native stoppered flask, independently compared against
its source pixels. The final F5/F6 receipts retain `BrewedTonic`, `Coating`,
`Burning:1`. See [plot uses review](Verification/Live16x24/plot-products-uses-independent-review.md).
This moves the works packet to **ordinary_partial** only. Its six other
product identities, actual forage/preparation, coin discovery/conversion and
application remain pending. No new art, runtime code or native suite rerun.
The checkpoint in `plot-products-uses-local-checkpoint.json` can continue
south to the existing works without replaying the accepted farming loop.
