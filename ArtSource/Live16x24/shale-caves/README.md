# Original shale material source — revised walls ready for actual trial

Six original opaque16×24 cells form `shale-caves.png`,96×24: four quiet floors
and two dark layered walls. The revised source contact was approved by root
for an actual gameplay trial. Source approval is not actual cave contrast,
whole-area polish or package acceptance. Original scene/parts references and
exact prompts remain unchanged; `REFERENCE-CRITIQUE.md` records rejected
visibility,geometry,UI,uniform gravel,gradient anddense-stripe suggestions.

## Named revision and protected approved floor

Root rejected the first contact's three isolated bright wall clusters because
they read as raised rock piles on smooth floor. The complete generated first
draft,builder and source checks are retained under `draft-v1/` with a hash
manifest. `WALL-REVISION-01.md` records the precise correction. The four floor
cells are unchanged: all1,536 approved native pixels and their named source
cells/palette match the original draft.

Only wall4/5 changed. The new field uses many short, staggered single-pixel
horizontal or stepped laminae, distributed through a continuous dark body.
Bright raised tips are removed; layering occupies29.17/29.95% of the two cells,
within the root-directed20–35% range. Low contrast and broken run lengths avoid
full-width stripes. All outer pixels remain the family's base for continuity.
No beveled edge,cast shadow,perimeter mask,masonry,extrusion or animation exists.
The repeated horizontal rhythm still needs actual normal-zoom chunk review.

## Parts, palette and real owner meanings

| Frame | Owner | Base / detail pixels |
| --- | --- | ---: |
| 0 | ShaleFloor | 379 / 5 |
| 1 | ShaleFloor | 378 / 6 |
| 2 | ShaleFloor | 377 / 7 |
| 3 | ShaleFloor | 377 / 7 |
| 4 | ShaleWall | 272 / 112 |
| 5 | ShaleWall | 269 / 115 |

Palette: floor`54585C`(84,88,92),floorflake`5C6064`,floordust`4A4E54`;
wallmass`3C434C`(60,67,76),darkinterlayer`343D46`,cutlamina`47525F`,secondary
body`424C58`. Seven flat colors,binary opaque alpha,square pixels. Floor
quietness is98.18–98.70%; wallbase is70.05–70.83%,with subtle layers instead of
separate pale rubble piles. The wall base is at least15RGB levels darker than
the floor in each channel. This ungraded contrast is not a Cave-grade promise.

ShaleFloor is the exact neutral passable Terrain owner with`.` and authored
variants/foreground`&K`; no Material,Thermal orDestructible parts are invented.
ShaleWall is the exact solid Stone/Mineral/Thermal owner with structuralHP28,
max28,hardness2 and existing Rubble wreckage. SurvivingHP1..28 retains its
material body. Real destruction removes it and creates the simulation's Rubble;
no depleted/broken sprite or gameplay owner is manufactured. GenericWall40/3,
LimestoneWall35/3,VineWall,props,liquids,actors,hidden owners andspecialsites do
not inherit this art by name,depth or glyph resemblance.

The floor's RGB84,88,92 may support root's separately tested owner-free ambient
backing on eligible neutral disclosed cells. That canvas does not create a
floor,collision,reader or native hazard claim. Runtime authority/state/reader
selection is separate root-owned code, not supplied by this source builder.

## Editable native geometry and commands

`shale-caves.blend` contains2,304 integer unit quads grouped into24 named
editable material/part meshes. All faces are atz0 and use exact emission colors.
Six native instances have unit scale/no rotation atx0,16,32,48,64,80.
`source-cells.json` records each authored part/color cell. The native atlas and
1×/8× contacts render directly from the same geometry at output resolution;
there is no raster scale,trace,crop or sample from concept images. Pillow only
reads pixels for verification. Contact arrangements are source studies,not
actual cave geometry or gameplay screenshots.

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_shale_caves.py -- --build
python3 Tools/Live16x24/build_shale_caves.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/shale-caves/shale-caves.blend --threads 4 --python Tools/Live16x24/build_shale_caves.py -- --inspect-blend
```

The established limestone/opening geometry helpers are reused without edits.
The nativePNG,both contacts and source-cell JSON are byte-identical between
two fresh Blender builds. Only volatile `cycles.*_time` PNG text chunks are
removed after rendering; all image chunks remain unchanged. Blend session
metadata bytes vary,so both saved blends were reopened to verify every unit
face,named part,actual emission material and native instance. No binary-blend
reproducibility is claimed. Blender5.2.1's future `use_nodes` deprecation warning
is retained in the inspection logs.

## Retained verification and preservation evidence

- `source-red.txt`: absent native atlas before first outputs; this is art-source
  RED,not root's runtime test count.
- `wall-revision-source-red.txt`: changed authored wall palette/source refuses
  the old render before revision rebuilding.
- `source-green.txt` / `source-verification.json`: all2,304 native pixels and
  748,800 direct contact pixels exactly match source; uniform family edges,
  opacity,palette,quietness,source-part andapproved-floor assertions pass.
- Ten corruption controls refuse incorrect canvas,transparent/partial alpha,
  changed floor/wall edges or original chip,swapped variants,duplicate source,
  named-part drift and changed importer filtering. Metadata-only trailing
  whitespace has a separate positive control.
- `baseline-protected-inputs.json` pins42 exact and23 metadata inputs before
  authoring. All16 old shale sources/metas plus approved limestone/shallow
  sources,shared rubble/stalagmite/stairs/cast and helper files remain protected.
- The unchanged `protected-inputs.json` extends that baseline to48 exact files
  with original concept/prompt/critique pins. Initial raw metadata hashes remain
  recorded; only trailing horizontal whitespace is ignored for metadata checks.
  No input is rewritten or automatically repinned.
- Separate `revision-protected-inputs.json` protects21 exact files: the rejected
  draft,its manifest,the named revision rationale andoriginal guard. It does not
  overwrite historical source protection.
- `blend-check-first.txt`,`blend-check-final.txt` and the geometry receipt prove
  reopened native geometry. `rebuild-before.json`,`rebuild.log` and
  `reproducibility.json` retain the final two-build comparison.
- The exact sourcePNG is copied to the new runtime resource with a unique meta
  GUID and Point/Clamp/noMip/uncompressed/16PPU settings. Source verifier confirms
  the copy and import fields. No prior PNG/meta/source changed.

## Visual self-review and handoff limits

The initial and revised contacts were actually opened. The named raised-pile
source defect is fixed: wall cells now read as a distributed dark layered mass,
with quiet floor visible alongside. The final floor remains unchanged. Repeated
short lamina patterns and grading may still require a named actual-view
correction; inspect existing rubble,stairs,wall silhouette andhazard marks at
normal zoom. No claim of finished chunk fullness,all-biome coverage or polish.
Root owns Unity imports,native tests,actual play and Git. This task changes no
runtime,tests,world generation or living coverage docs.

Final source/runtime PNG SHA256: `e749ed4470297915ae9542e07dfea589ad096155f080adf0cefcddf15ef033ae`.
Final editable blend SHA256: `438ccbcaa88c497ab6dd65174d8133906836010990cb7702c320bc6297fd285b`.
`frozen-handoff.json` pins the final source files,including the retained draft.
