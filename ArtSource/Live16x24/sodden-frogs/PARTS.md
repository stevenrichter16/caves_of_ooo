# Parts, reference critique and integration limits

## Inputs and corrections

- Actual composition input:
  `Docs/Verification/Live16x24/sodden-frogs-before-01-open-mire.png`.
- Built-in imagegen concept and exact prompt are preserved as
  `scene-reference-01.png` and `scene-prompt-01.txt`.
- Useful concept traits: low squat bodies, broad folded haunches, raised paired
  eyes, separated planted toes, green Reedfrog and scarlet-on-black Bandfrog.
- Reject the generated **“GIN FROG (Reedfrog)”** label: these are different real
  owners. Reject its extra directional poses, smoothed shading and enlarged
  proportions as production instructions. No generated pixels were traced,
  shrunk or extracted into the native atlas.
- Existing GinFrog is a protected16×16 pale-olive/cream PNG. The concept and old
  3D visitor palette cannot authorize overwriting or recoloring it.
- `ArtSource/SpreadVisitorCreatures/build_visitors.py` and `visitors.json` provide
  older original frog anatomy references, not a native2D source to import.

## Named parts

| Owner | Readable native parts | Meaning |
|---|---|---|
| Reedfrog | Broad green back; pale round throat; paired eye rims/pupils; two dark folded haunches; four toe clusters | Fat passive wildlife. No permanent attack posture, lamp-oil bottle or harvest-ready icon. |
| Bandfrog | Oblique coal body; three scarlet back bands; scarlet thigh/ankle markings; paired eyes; tucked haunch and four toe clusters | Anatomical warning bands. Neither a damage flash nor an active poison effect. |
| GinFrog comparison only | Exact existing native pixels | Separate unchanged owner; no new export or mechanics. |

Real identities are in `Objects.json:34511/34565/34672`. Reedfrog is passive and
can leave a corpse yielding FrogOil. Bandfrog has DefaultBite and CausticSkin;
its skin retaliates against adjacent physical contact, not remote or elemental
damage. GinFrog is passive. Do not encode current hostility, inventory, HP,
wetness or thermal state in the base-body selector.

## Actual-play watch items

1. Inspect the exact currently disclosed owners in the preserved ordinary
   Reed Maze/Open Mire checkpoints, not only this contact sheet. Verify world
   body, selected-owner reader and bounds at actual1× and1280×800.
2. Check Bandfrog's dark body against both grass and peat. Scarlet must read as
   separate transverse bands rather than an unbroken red damage silhouette.
3. Check the lower new16×24 body baselines beside the unchanged square GinFrog;
   source comparison exposes this difference rather than stretching old art.
4. Preserve real wet/damage cues. A red Bandfrog glyph can collide with a red
   damage-color override; root is testing that renderer path separately.
5. After movement, death/removal and fresh Continue, a corpse, remembered glyph,
   undisclosed owner or different frog must not receive a living new body.
6. These two bodies improve ordinary wetland encounters. They do not accept
   empty-area composition, all Sodden flora/props, optional animation, physical
   Deck readability or whole-game polish.
