# Grovelands materials — original native static source

Status: **source/contact approved by root for an actual gameplay trial**. Fourteen
original native16×24 frames are directly rendered from editable integer-cell
Blender geometry. The224×24 PNG and source metadata have been copied to their
owned Assets paths. This copy check is not Unity import/test or gameplay
acceptance. Root owns those gates. No optional animation was added.

## Real inputs and extraction decisions

The art author viewed the actual Fruiting Wall, Grove, Compost and Tendril Fen
captures named in references.json, the root's generated four-panel concept and
the approved Sill, North vegetation and opening base contacts. The exact root
imagegen prompt is `Docs/Verification/Live16x24/grovelands-concept-prompt.txt`;
concepts/ remains root-owned. references.json pins every input and records the
independent reviewer's two NPS form references and credits. No image, game sprite
or photograph was sampled, traced, cropped, resized or painted over by this
production builder.

Accepted concept choices: pale tapered living roots; restrained violet folded
shelves; low wine rosettes; teal seep contrast. Rejected: bright grass carpeting,
huge mushroom caps, fabricated structures at fog borders, new skeletons,
compost-as-mushrooms and excessive water waves. The game generator's existing
four formations and open approaches remain the composition; the source contact
is a material study, not a fabricated gameplay screenshot.

Important corrected premise: GroveSeep is a finite water stock, but ordinary
LiquidPool projects permanent water coating independently of Volume
(`Assets/Scripts/Gameplay/World/Map/Zone.cs:796–807`). Well drinking decrements
stock without removing the coating. Both source states therefore remain wet.
The empty state exposes filaments through darker wet film; it does not imply
remaining draw stock, a refill mechanic, dry ground or harmless footing. Frozen,
mixed-liquid and structural availability remain root's live semantic guards.

## Frozen frame map

|Frame|Owner/state|Visual and gameplay boundary|
|---:|---|---|
|0–1|MycelialColumn static variants|Narrow pale living stems, root buttresses and broken muted-violet veins; solid, real light remains separate.|
|2|GroveSeep Volume>0|Low filament rim and quiet teal water; available stock only under the real service guards.|
|3|GroveSeep Volume0|Same outline, wet dark film and exposed filaments; finite draw unavailable.|
|4|GroveSign|Grown wood board/support and raised grain; no arrow, nailed building or baked text.|
|5|GroveRedGrowth unharvested|Heavy low raw red lobes on living support; not normal berries or a carried item.|
|6–7|FruitingBody static variants|Solid compact folded violet-over-ochre lobes; not edible forage or BloomingFruitingBody.|
|8|CompostRow quiet base|Low continuous organic substrate; walkable.|
|9–10|CompostRow minority variants|Sparse cloth or chitin remnant on the same low bed.|
|11|CompostCache unharvested|Sagged pocket in the row; no visible/guaranteed coins. Harvest removal exposes the actual separate row.|
|12|ChoirIronVein unharvested|Dark grounded angular ore with restrained metal fracture, larger than the portable fragment.|
|13|WineLeafSundew|Low pointed wine-dark rosette with three sticky beads; repeatable step snare, not food or an animal.|

All foreground cells have binary alpha and a row22 ground baseline with row23
clear. Compost uses full cell width for low horizontal continuity; all art stays
inside its owner's16×24 bounds. Palette is explicit in source-cells.json and the
builder: shared subdued olive/wood/pale/ore values plus limited new muted violet,
wine and teal families,3–7 colors per frame. No glow, gradients, perspective,
alpha feathering or invented shadow layer.

The actual four base owners borrow **22 unchanged cells across three existing
atlases**: Sill-envelope0–15 VineWall and20–23 Grass, North-vegetation0 Tree,
opening-base5 Bush. The previous harvest packet's nine reuses are unrelated to
this count. New source contains none of those borrowed production frames.
Runtime coordinate variation should favor quiet base cells; source contacts
show repeated shelf/row material only, not the final regional selection weights.

## Reproduce and validate

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 \
  --python Tools/Live16x24/build_grovelands_materials.py
python3 Tools/Live16x24/verify_grovelands_materials.py
# Only after an approved byte-for-byte Assets copy/import:
python3 Tools/Live16x24/verify_grovelands_materials.py --check-unity
```

The builder always writes **packet-only**, even without a defer flag. It uses
existing camera/model/collection helpers; no new rendering framework or runtime
process is required. Each named part/color is editable integer-quad geometry in
`grovelands-materials.blend`; source-cells.json is its exact native cell recipe.
The atlas and1×/8× contacts are direct Blender renders of the same geometry.
Only the author/root may copy the approved PNG/meta to Assets; this builder does
not import, export there, invoke Unity or change accepted source packets.

`source-red.txt` retains the required missing-output failure before production.
The first complete source run and final run both pass. The verifier independently
decodes each native pixel against source cells, checks every contact pixel,
frame/state order, silhouette/alpha bounds, required semantic parts, persistent
wet-film structure, exact palette/import settings and407 preserved inputs.
Fourteen deliberately changed source/metadata countercases are rejected,
including dry exhausted seep, false stock, missing dew, wrong fungal owner,
duplicate visible variant, palette/grid/order/baseline errors and bilinear import.
Metadata checking compares the authored structure and GUID semantically, so
Unity-added defaults and whitespace do not create false failures.

One clean rebuild proved source-cells/meta bytes and decoded atlas/contact
pixels identical. Encoded PNG hashes changed with Blender render metadata;
`reproducibility.json` records both separately. There is no .blend1 backup.
The final PNG hash and metadata hash are in frozen-handoff.json. These are the
bytes copied to Assets; Unity may serialize additional metadata defaults later.
The copied-file source check was successful, without calling any Unity tool.

## Acceptance boundary and next step

Can verify here: original editable source, exact native dimensions/pixels,
source order/states, direct contact fidelity, deterministic decoded rebuild,
all407 prior inputs unchanged and the owned PNG/meta copy. Root visually
approved this contact for an actual trial.

Cannot verify here: runtime admission in all37 candidate addresses, actual
light/water overlays, selected-owner or fog behavior, gameplay interaction and
resource availability, cache-removal/row layering, real four-formation fullness,
Unity native tests, save/revisit behavior or device performance. Remaining
actors, optional hazards, crop stages and special sites are still coverage gaps.
Root must compare the same four actual gameplay scenes, especially repetition
of solid shelves and low compost, readability of the wet seep and contrast under
actual column light. Source completion does not close those gates.

Pixels and geometry are frozen after handoff. Any later named gameplay defect
gets a preserved revision and bounded correction; do not keep polishing optional
motion or silently edit accepted borrowed art.
