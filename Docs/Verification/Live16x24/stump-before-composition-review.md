# Stump: independent review of the actual before views

2026-10-27. **Baseline review only; no native Stump material acceptance.**
The reviewer directly viewed all six retained 1280×800 Unity screenshots listed
below. Root reports visiting the five formations through ordinary map controls
in the earned journey through checkpoint 6250. This review did not operate Unity
or independently replay those inputs. No runtime, art, test, save, build or Git
changes were made.

The five viewports already have different spatial structures. The most obvious
common defect is incomplete flat sprite coverage: pink stone is rendered as
small brown punctuation over black, large solid forms as repeated glyphs, and
water as rows of thin cyan marks. That presentation hides much of the existing
composition. These images do **not** justify indiscriminately adding more objects.
They also do not prove that the eventual native scenes will be sufficiently full.

## What the individual views establish

| Actual capture | Visible composition and useful existing structure | Coverage defect and subsequent comparison target |
| --- | --- | --- |
| `stump-before-01-cascade-gorge.png` | Several water expanses cross the disclosed corridor, separated by stone necks and ledges. The player is visibly standing in a pool; focus lists pink stone, spray pool and water underfoot. The water/stone alternation already gives this view a stronger identity than an open field. | Long cyan rows over black read as a diagram rather than bodies of water. Give the real pools a coherent wet body and the dry shoulders a readable continuous substrate. Preserve real necks, ledges and the player; do not imply an added bridge, simulated flowing river, or passability change. The large flat brown areas outside disclosure are not evidence of empty terrain. |
| `stump-before-02-grainfield.png` | Broad east–west bands with unequal breaks organize open stone spaces. Small green owners gather alongside some bands; a few other colored glyphs interrupt the regular ground. It is not an undifferentiated empty field. | Repeated equals signs read like rails, and the punctuated ground gives no pink-grey stone surface. Native ridge masses should retain the shared east–west grain and real openings, without becoming isolated fence posts or noisy per-cell stripes. Do not add more regular tree rows to solve a material problem. |
| `stump-before-03-buttress-ridge.png` | Thick, mostly vertical root-like fingers break the ground into open pockets. Their stepped outlines and taper distinguish this formation from Grainfield even before art. There are scattered vegetation and other glyph owners in the recesses. | The fingers still look like runs of text. Their connected stone silhouette needs to remain distinct from walkable floor and low ledges. Preserve the recesses and movement gaps. The flat brown region on the right is outside the disclosed geometry, not a demonstrated bare pocket. |
| `stump-before-04-summit-scrub.png` | Grouped dome glyphs sit around a broad open central clearing. Small green owners occur near several groups; the scene is visibly more exposed than RimForest. | This is the strongest **fullness risk** among the sampled views: the center is dominated by repetitive ground punctuation with weak material and landmark hierarchy. First render the real stone, dome masses and vegetation clearly. Then compare whether the clearing has enough readable edge structure at ordinary zoom. If it still feels empty, record that as a composition defect; do not dismiss it automatically as intentional quietness or fill routes with decorative blockers. |
| `stump-before-05-rim-forest.png` | This view has the most visible green clustering: tree-like glyph groups and smaller vegetation follow a loose winding band, with exposed stone and dome groups around it. There are meaningful openings rather than a uniform carpet. | The existing plant distribution does not yet read as a forest because it is mostly letters and symbols. Coherent tree/bush silhouettes, visible tank identities and continuous stone should make the planted pockets legible without enlarging collision footprints or covering the openings. Its density must remain distinguishable from SummitScrub. |
| `stump-before-06-tank-reader.png` | The real examine panel names tank-brocchinia and describes a rain-holding bromeliad; its portrait is still a bright green `u`. It establishes an encountered tank identity, not merely a planned source asset. | The world glyph and reader need the same recognizable, low rosette/cup identity. A tank is not a barrel prop, solid tree, harvest drop or generic cactus. This static reader does not establish stock, an attempted drink, depletion, or any new interaction. |

## Mechanical truth and source corrections

- Current `Objects.json` defines `TepuiStone` as the pink-stone ground; its
  punctuation variants explain the widespread black-backed surface. The kit
  should represent the existing floor rather than manufacture new terrain.
- Current `GrainRidge` and `StoneDome` have solid physics. `TepuiWall` inherits
  `Wall`. Do not paint these as indistinguishable walkable floor or turn the low
  `DescentLedge` scenery into a visual cliff that promises new elevation rules.
- **The historical `Docs/STUMP-COMPOSITION.md` verification table is stale on
  two mechanics.** Current `SprayPool` has a water `LiquidPool` with blueprint
  volume 40, and current `TankBrocchinia` has `Well`. The pool's water focus is
  also visible in capture 01. Preserve current liquid/source behavior and
  essential states; do not repeat the old claims that pools have no liquid
  part or tanks have no drinking function. `WellPart` delegates availability and
  drinking to the real service, so the reader alone does not prove a successful
  action or warrant drawing a fixed stock indicator.
- The composition builder places the actual ground and separate owner objects.
  Its current plans deliberately use continuous ribs, tapering fingers,
  dome clearings and a humid forest crack. Preserve those owner shapes,
  removals, damage, harvest and source states. Do not add rendered-only flora,
  fake water or reconstructed objects to make screenshots fuller.
- Colored glyphs other than the examined tank are not identified here solely
  by their shape. Actors, crops, resources and hazards must retain their real
  identities and remain visible when the native substrate is introduced.
  The sidebar contains older Overwrit log entries; those entries are not
  evidence of Stump content in the current view.

## Bounded next gate

Prioritize quiet continuous pink-grey ground and truthful pool bodies, then
connected ridge/outcrop/dome silhouettes, then the existing vegetation and tank
owners. Keep actor and remaining prop/crop gaps explicit. Revisit the same five
camera views after integration, with the player and hazard markings intact;
compare the isolated tank reader as well. Avoid per-cell decorative saturation
that makes material repetition more obvious than the landforms.

SummitScrub's open center and the coherence of RimForest's winding vegetation
need particular attention in that actual comparison. CascadeGorge also needs
its dry/wet boundaries checked while walking. Neither the viewport nor the
source plan proves every edge exit, all 80×25 cells, all ordinary Stump
addresses, special sites, underground areas, or the complete region. Fog and
undisclosed cells are excluded from the fullness judgment. No animation,
performance, physical Deck or packaged-play claim is made.

## Evidence identity

All files are under `Docs/Verification/Live16x24/`; none were edited.

| File | SHA-256 |
| --- | --- |
| `stump-before-01-cascade-gorge.png` | `432b67e7307edc83daa0c63b0da7d0f937f9557d38407c85a6c5f5121bf3e963` |
| `stump-before-02-grainfield.png` | `583a779dd1147b1a39798bbe8b1978a224d2da8b8a6ab772e2fd1a1ae00335a1` |
| `stump-before-03-buttress-ridge.png` | `af7520eae3defcf061ce42696f5e94cf3dde71d0839bfcad594e54a46de2fcba` |
| `stump-before-04-summit-scrub.png` | `951c7f12e6dd2ceae73a186e1ffb0e4e2765325d5ae30806f4650b11b288b95d` |
| `stump-before-05-rim-forest.png` | `1e5f50f839f3f82637b5335a45f9fd3a6acd476fe12fd0322a8529b7fa47a09f` |
| `stump-before-06-tank-reader.png` | `3ec594bc9ff39806de9dab3bb694123d099177e5d1df4531dc2e484e64572e84` |

Source read: current exact blueprint definitions in
`Assets/Resources/Content/Blueprints/Objects.json`, `WellPart.cs`,
`LiquidPoolPart.cs`, `StumpCompositionPlan.cs`, `StumpCompositionBuilder.cs`,
and the historical `STUMP-COMPOSITION.md`. No source-level completion statement
in the older voxel record is treated as acceptance of the new flat 16×24 kit.
