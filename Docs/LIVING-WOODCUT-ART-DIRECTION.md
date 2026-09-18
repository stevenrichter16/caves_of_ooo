# Living Woodcut — Caves of Ooo

Status: lore-grounded concept exploration. The approved first image establishes
the carved 3D style; these studies develop a proposed visual language and do not
replace the current Blender assets or Unity presentation. The town is outside
this exploration. No gameplay/source/palette assets are being changed.

## Core idea: grain, thread, erasure

The world carries the consequences of the Felling and of Naming. Its visual
patterns should express those consequences through material and form. Keep
chunky sculpted volumes, broad two-or-three-tone shading, colored occlusion,
limited palettes, sparse etched detail and a strict overhead camera.

- **Grain:** Stump geology exposes a common fossilized wood structure. Separate
  rocks can suggest fragments of one immense body. Pink-grey sandstone and
  narrow pale tepuibone seams retain the actual region's identity.
- **Thread:** Choir growth connects across roots and damp banks. Pale ochre,
  violet and restrained green-white living light suggest Selen's inclusive joy.
  The land looks inviting; biological unease arises from connection and offered
  shelter. Manufactured objects do not become Choir material culture.
- **Erasure:** The Overwrit's loss of detail is intentional. Reserve scraped
  surfaces, absent expected structures and authored threshold reveals for their
  actual world states. Never scatter procedural pseudo-writing across biomes.

These are proposed art translations. They do not add an explanatory cosmology,
new faction symbols, secret answers, or new native hazards.

## Verification sweep before images

| Source fact | Correction to the broad first concept |
| --- | --- |
| Bible canon: the Tree bound an older world; it did not create everything | Do not present all geology/life as literally created from the Tree, or make ordinary ground an explained cosmic diagram. Stump/tepui anatomy is the specific fossilized inheritance. |
| Selen's emotional key is joy; Grovelands are kind and inviting | Avoid standard sinister tentacle-swamp imagery and indiscriminate gore. |
| Grovelands render contract: near-black loam, pale ochre growth, violet accents | Move the first study's extensive mustard soil into the growth palette; keep shaded soil visibly textured and readable. |
| FruitingBody description: shelves and closed fists, violet over ochre | Use that silhouette and material structure instead of arbitrary coral mushroom caps. |
| GroveRedGrowth and WineLeafSundew have distinct warnings and shapes | Reserve strong red patches for actual red growth; sundews remain wine-dark sticky leaf rosettes. Color alone never replaces silhouette. |
| Captured fen has 3 ChoirTendril, 65 FruitingBody, 6 red growth, 1 sundew, 6 CopperPipe; no MycelialColumn or HelmwoodFrog | Keep the braided fen recognizable; do not add columns/frogs from neighboring ecosystems or depict the central pipe remnants as new Choir-built wooden bridges. Images remain approximate concepts, not counted placement receipts. |
| Buttress ridge paths radiate, while Stump material grain runs east–west | Apply coherent surface grain independently of ridge path direction. Preserve actual walkable gaps. |
| Urqu is pressure, never an intentional agent; manifestations are state-specific | No evil god face, eyes in all trees, or permanent glitch wallpaper. None is added to these two ordinary chunk studies. |
| The Mystery Ledger reserves singular phenomena and ambiguity | No duplicated doll-in-wall, mapped under-text, or literal depiction proving an ending or Naro reading. |

## Biome expression

| Region | Proposed treatment within its canon |
| --- | --- |
| Spread | Warm greens, worked earth, ordinary hedges and worn human carvings. Folk magic can be small and lovely where actually present. Familiarity gives later strangeness weight. |
| Sodden | Tea-black water, olive reeds, ochre tussocks and horizontal peat cuts. Damp compressed shapes convey flood history; specific exposed remains belong to their actual sites. |
| Beating | Chalk-white salt planes and hard raw-sun shadows; dark cloth and shade make inhabited shelter visually distinct. No blanket fungal growth. |
| Grovelands | Dark moist loam, pale ochre/violet growth, connected mycelium and localized green-white light. Clear beetle/walking paths persist between lush forms. |
| Overwrit | Uncomfortably smooth, sparse, even surfaces. Authored bleed scenes appear only under their intended conditions; extra decorative variety would weaken this biome. |
| Stump | Pink-grey fossil woodstone, shared grain direction, narrow ivory tepuibone and restrained growth. Warm lower bands progress toward the cooler summit. |

## Implementation-facing art rules for a later asset pass

The user's 2026-09-10 mechanics requirement takes priority over concept-image
geometry: large continuous artwork must preserve target ownership and normal
destruction. See the [multi-cell feasibility audit](MULTI-CELL-ENTITY-FEASIBILITY.md).
True large objects need one canonical owner and a shared gameplay footprint;
the current Morrowfast implementation is only a partial precedent. Keep ordinary
cell entities intact until that infrastructure is verified. Continuous ridges
and walls may use joined-looking, independently destructible sections. Never
replace them with an indivisible chunk mesh that loses native interactions.
Canopy overhang must remain visually distinct from physical occupied cells.

Keep this as a practical mesh/material direction: large silhouettes before
surface marks; four real variants for repeated families; shared palettes;
shallow relief and broad shading instead of thousands of noisy leaf edges.
Native entity references, movement openings, water masks, lighting, fog and
conditional population stay authoritative. No material change implies a new
interactability, faction affiliation, light source or collision rule.

Living light follows native emitters. A future effect showing the same pulse
traveling through connected growth is a proposed animation treatment, not a
claim about a shipped network simulation. Reference illumination is artistic
and does not prove runtime exposure, performance or shader behavior.

## Sources and authority

- [Lore Bible](../Lore/10_Bible.md), canon lock and tonal register: current authority.
- [Second Spine](../Lore/11_SecondSpine.md), C1: Urqu as pressure.
- [Mystery Ledger](../Lore/MYSTERY-LEDGER.md), permanent ambiguity and singular phenomena.
- [Felling world design](FELLING-WORLD-DESIGN.md), §§3.1–3.6 and render contract.
- [Native ring survey](Verification/SpawnRing3D/native-survey.md), actual pipelines/state limits.
- [Objects blueprints](../Assets/Resources/Content/Blueprints/Objects.json), current descriptions and identities.
- [Fen manifest](../ArtSource/SpawnRing3D/scenes/Overworld.3.7.0/manifest.json) and [ridge manifest](../ArtSource/SpawnRing3D/scenes/Overworld.4.5.0/manifest.json), captured instance inventories.

The older Rot Choir faction history was consulted but is subordinate to the
Bible: its patient/maternal emphasis does not override Selen's current joy key.
No Qud mechanical-parity claim; the initial atmospheric inspiration develops
into the game's own material, ecological and narrative vocabulary.

## Review and deliverables

Completed 2026-09-10 using the builtin `image_gen.imagegen` tool:

- [Approved starting style](References/LivingWoodcut/living-woodcut-approved-v1.png).
- [Choir TendrilFen study](References/LivingWoodcut/tendril-fen-lore-v2.png).
- [Stump Buttress Ridge study, corrected](References/LivingWoodcut/stump-buttress-lore-v2.png).
- [Exact prompts, input paths and SHA-256 provenance](References/LivingWoodcut/prompts.json).

Visual review: both delivered images use full-bleed panoramic overhead framing
and retain recognizable regional structure. The fen replaces mustard ground
with dark loam, separates crimson growth from violet/ochre fungi, and preserves
the water braid and small pipe bars. Its three pale tendrils are oversized for
ordinary creature models: reduce their footprint and height during modeling,
using native entity scale. Their branching anatomy is a design proposal, not a
new canon assertion. Subtle mycelial color and pipe material still need material
tests at gameplay resolution; the reference does not prove light behavior.

The first ridge generation incorrectly made tepuibone into upright bone fences.
A focused ImageGen correction replaced those with flush pale mineral seams and
reduced fine ground striations. The final study keeps pink-grey fossil woodstone,
large grain bands, open gaps, tar seeps and pipe/vent remnants. Local grain curls
remain artistic: actual modular materials must maintain the canonical common
east-west direction. Creature likenesses, counts, footprints and all collision
boundaries remain governed by native content, not the illustration.

Cold-eye/adversarial review: do not infer new passages, manufactured Choir
architecture, permanent manifestations, hidden lore answers or extra hazards
from decorative marks. No live Unity look-pass, performance result, test-suite
result or implemented asset replacement is claimed by this concept work.
