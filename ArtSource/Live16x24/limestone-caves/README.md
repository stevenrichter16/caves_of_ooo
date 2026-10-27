# Original limestone floor and wall source — ready for actual integration trial

2026-10-27. Six original, opaque16×24 cells form `limestone-caves.png`,96×24.
Frames0–3 are quiet LimestoneFloor;4–5 are continuous LimestoneWall. Source
verification and editable geometry pass. This is an ungraded source handoff,
not actual limestone gameplay, full-area polish or package acceptance.

## References and rejected premises

Root retained actual ordinary depth3/depth4 entries, passage/chamber and wall
reader under `Docs/Verification/Live16x24/deeper-strata-before-*`. The existing
scene/parts images and exact prompt files here remain unchanged. The separate
`REFERENCE-CRITIQUE.md` rejects the scene's brightened memory/expanded fog and
raised edge suggestions, and the parts sheet's gradients, irregular pixels and
excess detail. Only the approved palette and quiet flat material distinction
informed the original integer-cell designs. No reference image is sampled,
traced, cropped, downsampled or edited by the builder.

## Parts, owners and essential states

| Frame | Atlas rectangle | Actual meaning | Base/detail pixels |
| --- | --- | --- | --- |
| 0 |0,0,16,24|Neutral real LimestoneFloor, sparsest dust chip|381/3|
| 1 |16,0,16,24|Same floor, offset small dust chips|379/5|
| 2 |32,0,16,24|Same floor, upper flake and tiny pits|378/6|
| 3 |48,0,16,24|Same floor, lower paired flake|378/6|
| 4 |64,0,16,24|Surviving LimestoneWall, internal short fractures|369/15|
| 5 |80,0,16,24|Same wall, alternate internal fractures|368/16|

All outermost pixels equal their family's base. Floors are98.44–99.22%quiet,
walls95.83–96.09%; required minima95%/94%pass. The source has seven flat colors:
floor`707064`,fleck`78786A`,dust`68685C`,wallmass`929282`,fissure`7E7E6F`,seam
`88887A`,worn`9E9E8C`. There are no edge masks,outlines,masonry,bevels,extrusion,
cast shadows,partial alpha or nuanced animation. Wall mass is lighter than
floor in all three RGBchannels; actual Cave grading still needs review.

LimestoneFloor is passable Terrain with its real render/part semantics; no
Material/Thermal/Destructible parts are invented. LimestoneWall inherits solid
Stone/Mineral/Thermal meaning and has structuralHP35/max35/hardness3,not a
creature health stat. SurvivingHP1..35 keeps an intact mass; actual destruction
removes that owner and creates existing Rubble. These cells do not reinterpret
genericWall,VineWall,Pillar,BrokenColumn,creatures,hazards,loot or hidden contents.
The approved floor baseRGB112,112,100 may separately support the runtime's
conservative owner-free ambient backing; this does not create a floor owner,
collision or reader. Runtime policies remain root's separately tested work.

## Editable authoring and reproducibility

`limestone-caves.blend` contains six named source collections with2,304 integer
unit quads in26 named mesh clusters. Geometry lies atz0 with exact flat emission
materials. Native instances are atx0,16,32,48,64,80 with unit scale/no rotation.
`source-cells.json` records each original part/color cell. The native atlas and
1×/8× contacts are rendered directly from the same collections at final scale;
there is no raster enlargement or editing. Contact layouts are material studies,
not actual cave layouts or gameplay views.

Run from the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_limestone_caves.py -- --build
python3 Tools/Live16x24/build_limestone_caves.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/limestone-caves/limestone-caves.blend --threads 4 --python Tools/Live16x24/build_limestone_caves.py -- --inspect-blend
```

The builder adapts the established shallow-cave geometry/render checker and
uses its existing helper chain. No general tooling framework was added. Only
volatile `cycles.*_time` PNG text chunks are removed after render; all image
chunks remain byte-identical. Pillow only decodes/checks images. Two fresh
Blender builds produced identical nativePNG,both contacts and source-cell bytes.
Saved blend session bytes differ, so both saved sources were reopened and every
part,color,unit face and native instance was verified instead of claiming binary
blend reproducibility. The known Blender6 `use_nodes` deprecation warning from
the read-only inspection is retained in logs; current Blender5.2.1 passes.

## Preserved inputs and retained checks

The finite guard checks27 exact files and9 metadata files: approved resource
PNGs,editable sources,helper dependencies,the four root references plus critique,
and two actual baseline images. Only trailing horizontal whitespace is ignored
in protected Unity metadata; GUID,structure,settings and values stay protected.
No input is auto-repinned. Raw inputs are never rewritten. The current evolving
phase/coverage records are deliberately not source dependencies.

- `source-red.txt`: verification failed for absent native atlas before outputs.
  This source RED is separate from root's genuine36-case runtime RED.
- `source-green.txt` / `source-verification.json`: all2,304 native pixels and
  748,800 direct contact pixels agree with the original source. Opacity,quiet
  fractions,uniform family edges,palette and protected inputs pass.
- Ten corruption controls reject wrong canvas,transparent/partialalpha holes,
  altered floor/wall edges,altered chip,swapped variant pixels,duplicate source,
  named-part drift and changed filter despite whitespace tolerance. A positive
  whitespace-only metadata control passes.
- `blend-check-first.txt` / `blend-check-final.txt` plus the geometry receipt
  verify reopened source parts,actual emission colors,faces and frame order.
- `rebuild-before.json`, `rebuild.log` and `reproducibility.json` retain the two
  fresh-build comparison. `frozen-handoff.json` pins final delivery files.

The exact new sourcePNG is copied to
`Assets/Resources/Sprites/Live16x24/limestone-caves.png`, with a new unique metadata
GUID and Point/Clamp/no-mipmap/uncompressed/16PPU/FullRect settings. No prior PNG,
meta or editable source is changed. Writing the resource is not a claim of
Unity native GREEN or actual integration. Root owns imports,tests,gameplay and Git.

## Visual self-review and limits

The native and8× contact were opened and inspected. The restrained dust flecks
keep broad floor quiet; the brighter wall field remains distinct without raised
faces. Neighboring cells share clean base edges; no false shadow or pit hole is
authored. Tiled wall fractures visibly repeat in the study, and contrast may
shift under actual Cave grading: judge these in the normal camera before any
further pixel change. Do not add texture or generic props merely to fill space.

Accepted prior sprites,editable sources and working animations remain protected.
No new actual view,reader,damage/destruction,travel,restart,wholechunk/fullness,
package,Linux/Deck or performance acceptance is certified by this source packet.
