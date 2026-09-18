# World miniature design language

Status: authored design direction; conversion ledger and candidates in progress.
This is a CoO-original presentation expansion. UI remains UI, all world models
are mesh volumes, and established town/pilot scenery keeps authored precedence.

## Common language

Carved, weathered miniature forms with low-gloss surfaces, deliberate silhouettes
and small pools of saturated biological color. Bone ivory, old copper, pink
tepui grain, olive lichen and ink-dark recesses connect regions. Avoid making
all creatures mushroom people or all objects rounded stone. Material, anatomy
and faction must remain readable at the fixed 56-degree game camera.

A cell is the mechanical footprint. Small fauna should occupy approximately
0.5–0.9 cells; a tall silhouette can be visible without adding false collision.
Large models need a verified native multi-cell footprint. The renderer never
makes an owner invulnerable by baking it into scenery. Decorative component
geometry follows its owner's destruction, pickup, transfer and visibility.

## Actor families

- Sill townspeople: layered hoods, worn hems, broad hands and faces readable
  beneath hood rims. Profession details are tools, aprons, satchels and wear,
  while actual weapon/armor attachments follow inventory, not decorative fakes.
- Concord: measured, tidy closures and warmer wax/gold details. Recension:
  indigo cloth, nested paper tabs, ink and attribution tools. A blueprint named
  PalimpsestEcho is currently a **Recension field scribe**, not a ghost; use its
  shipped display identity and sprite.
- Snapjaws: protruding animal jaw, compact upright stance, differing scavenger,
  hunter, chieftain and warlord equipment/silhouettes. Preserve canonical reskins.
- Mammals: separate bear, canid, feline and ape proportions. Visible shoulder
  masses and jointed limbs, rather than four sticks beneath one shared sphere.
- Arthropods: spider abdomen/cephalothorax and eight legs; scorpion pincers,
  segmented abdomen and curved stinger. Glass forms retain chitin structure
  through opaque pale facets, with selective highlights.
- Serpents/wurms: continuous tapering skins, coherent scales, distinct head and
  tail. Heavy Sari coils contrast with the long branch-draped Wardline.
- Birds/bats/moths: separate feather, membrane and dusted-wing construction;
  anatomy-specific flapping rather than humanoid skeletons.
- Frogs/lizards/tortoises: folded legs, eye bulges, throat volumes, scale/casque
  plates and toe shapes. Existing frog/tortoise/snake rigs are reusable anatomy
  infrastructure, not permission to erase species markings.
- Fungal/stone/undead actors: weight-bearing roots or articulated stone plates,
  fungal shelf growth and species-specific skull/shell details. Preserve the
  silhouette of the original person where the lore says it once was someone.

## First endemic batch

| Identity | Actual shape and marking | Behavior/presentation constraint |
| --- | --- | --- |
| Summit Singer | Small brown frog, raised throat, folded hind legs, dark W following the curved back, fine granules | Passive atmospheric anchor. Do not invent an aggressive attack, perpetual call or glowing alarm effect. |
| Brocchinia Sentinel | Low wedge-headed lizard, long tapering tail, olive/lichen mottles, yellow eyes, splayed toes | Cryptic endemic. Do not add a permanent bromeliad prop that would imply another occupant or prevent escape. |
| Sima Pricklebrow | Indigo gecko, amber throat, conspicuous fine brow spines, elbows, adhesive toe pads, lace scales | Communal fauna remains separate native owners; no decorative flock attached to a single health pool. |

Source: shipped Objects.json descriptions, Environment sprites viewed directly,
and sarisarinama_bestiary_design.md species entries. Their first FBX/Blender
studies are candidates; integration and animation review remain gates.

## Items, fixtures and terrain

Weapons need actual blade/haft/edge profiles matching their class; enchanted
variants carry the relevant ember, frost, acid or choir material without hiding
weapon type. Tonics use silhouette, closures, liquid body and sigils as well as
color. Grimoires are bound volumes with appropriate cover motifs; components,
food, reagents, ore and keys retain distinct tangible forms.

Container open/closed states must reveal the correct cavity; a destroyed shell
must not survive as baked decoration. Doors, traps, crops, lights, quest markers,
liquid depth and maintenance stages require state mappings before claiming full
coverage. Four variants for repeated natural props; connected geology remains
cell-owned or uses existing multi-cell ownership, never a replacement collider.

Floors keep broad, quiet material fields. Walls show strata, breaks and readable
crowns. Library memory marble, choir iron and tepuibone require distinct carved
materials. The Rooted is a specific human pose with a deliberately empty reach
toward the eastern wall; a generic stump would lose the lore.
