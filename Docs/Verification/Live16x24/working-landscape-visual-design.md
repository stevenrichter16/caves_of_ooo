# Working landscape: restrained scene-composition design

2026-10-24. Independent visual design, grounded in actual ordinary Unity captures and current generation source. No asset/runtime edits or Unity actions. Parent owns imagegen, implementation and gameplay evidence.

## Decision

Make the existing kitchen and forge read as places within a recovered working landscape. The current native objects are accepted; the problem is their setting. Orven’s small service group is outweighed by repeated rectangular hedge rooms, while the forge sits beside long parallel flower rows that look mechanically planted despite the flowers’ wild description.

The next deliverable is a visibly better pair of complete playable views, then their fit within the six-stop opening. It is not a new prop packet. Use the existing dark-green ground and native object colors. Direct the eye through asymmetric boundary masses and open approaches, not through brighter characters, new quest markers, decorative roads or more clutter.

Primary baselines:

- `Docs/Verification/Live16x24/east-kitchen-integrated-kitchen-revisited.png`: kitchen composition, after actual harvest/return.
- `Docs/Verification/Live16x24/east-kitchen-integrated-east-return.png`: east landscape, flowers and forge setting.
- `Docs/Verification/Live16x24/east-kitchen-integrated-forge-reader.png`: true forge identity and approved source silhouette; do not use its modal-obscured view as the main scene paintover.
- `/tmp/coo-east-kitchen-integration-visual-review.md`: acceptance and pan-text closure.

## Priority order

1. Make the inhabited kitchen court and forge approach recognizable at the current world scale.
2. Break the repeated large geometric rhythms: several similar enclosure rectangles; three parallel flower bands.
3. Keep the journey visually related through the already-accepted masonry, quiet ground, cultivated beds and native sprites.
4. Add a connected hedge derivative only if arrangement alone leaves a named repetition/readability defect. Do not use detailing to postpone layout critique.

## Kitchen design: a working edge, not a furniture island in a room

**Large shape:** one sheltered working edge opening onto a broad dark-green approach. Background boundaries survive as incomplete crofts of unequal apparent size. The player should read the court’s open side before reading a series of similar field boxes.

**Keep the useful small arrangement.** The current relative service stamp already has a clear functional organization: two shelter stones and pan/input/pickup along its back; oven to one side; Cook in the middle; chair and cot to the other side; one real claspbean bed forward of the oven. Do not scatter these owners to make a picturesque arrangement. The connected chair is already offset from the cot. Preserve the ordinary access and the pan/tray/board order unless a new greybox demonstrates a better reachable layout.

**Change the surroundings first.** Give this group a short, jointed or L-shaped surviving hedge edge behind/to one side, with unequal arms. It should support the two real shelter stones rather than form another complete four-sided room. Leave the court’s working side broadly open, with at least two distinct physical ways to reach the actual services. Do not cap the approach with a decorative gate, add a roof, or put foliage in the service-adjacent cells. The clear area is normal grass; it gains meaning from the hedge ending and the arrangement of real objects, not a colored plaza decal.

**Break only enough background geometry to change the hierarchy.** Start with the two most dominant nonfunctional rectangular enclosure shapes in the kitchen baseline. Turn one into a long surviving run with an offset short return and a broad missing side; turn another into two unequal remnants separated by a deliberate gap. Retain the wooded lower-left mass as the heavier wilderness counterweight, keeping its real trees and bushes distinguishable. Avoid replacing five equal rooms with five equal L shapes. Keep functional pens, encounter-cover constraints, exits and any passage/haul obstacle intact. Do not wholesale clear the zone for the court.

**Visual balance target:** a compact inhabited foreground anchor, one larger distant boundary remnant, and one wooded edge. The other ground remains quiet. Empty space is useful if it lets the player read the entrance and encounter spacing. Additional crates, lanterns, mats, flags, furniture or food would be invented scope and are rejected.

**Seed-64 specificity:** the accepted image has the kitchen at `(18,8)` with pan/trays on row6 and the harvested bed at `(16,10)`. Those coordinates identify this baseline only. The design is a relative footprint/relationship, not a rule to force all seeds into that corner. Seed65 already places the same service near `(54,16)`; the composition must turn/mirror sensibly around the actual validated stamp.

## East design: a wild meadow interrupted by use

**Large shape:** unequal flower drifts framing a legible stretch of open ground, with a small masonry work edge at the forge. The open route should read as continuous navigable grass. The scene should not read as planted rows or a horizontal texture repeated across the screen.

Replace the three nearly parallel `flower avenues` sine bands with a small number of unequal actual placement masses:

- One broad, broken crescent or hooked drift toward one outer edge, with a thicker middle and tapering ends.
- One shorter, oblique or offset drift on the opposite side, ending before it visually meets the first.
- A small detached echo near an existing tree/bush mass if the current owner budget and safe approaches allow it.

The concept can use stepped tile contours and holes of ordinary empty grass. Avoid continuous straight flower lines crossing much of the viewport, identical repeated ovals, evenly spaced confetti, or a flower border around every object. Do not just rotate all three bands together: their lengths, width and terminations should differ. Keep the accepted two-bell flower sprite and its restrained pink; arrangement supplies the change.

The forge remains the existing neutral hearth, its real stoneburr forage and three broken-wall owners. Its surrounding open space should feel cut out of the meadow by use: flowers stop short of its two reachable approaches, and the masonry fragments give it a small back/side edge. Do not invent active coals, smoke, anvils, a blacksmith, fuel, tools on a table, roofs or inventory goods. The existing copper-pipe/vent cluster, millstone and functional pen remain their own actual owner groups; they are not spare decoration to move near the forge.

The broader drift arrangement may require truthful landscape-context wording. Current `LandscapeContext` says wind-combed blooms form long avenues. If the final shape no longer supports that wording, update that geometric sentence with the actual new drift/open-lane relationship. Do not add lore claims or services to justify the art.

## Ground and boundaries: truthful limits

- The baseline court is grass. Reserved `IsApproach` cells are not road owners. Keep them dark green; do not paint a tan road over them to make navigation obvious.
- Existing real RoadStone, cultivated beds, water/coatings and walls keep their corresponding native treatment. The empty harvested claspbean bed stays an empty prepared-soil cell. No artificial wet patch, furrow or ripe plant is permitted.
- A new short packed approach is optional, not required by this visual design. If the implementation chooses it after the grass-only greybox, it must be actual validated RoadStone generation linking a real route to the service. It cannot be a renderer-only patch, a sprinkling under furniture or an implied road leading nowhere. The current glade-only ground-art admission must not accidentally widen with foreground scope.
- Hedge openings must be actual openings. If connected hedge run/end/corner derivatives become necessary, keep the accepted palette and native cell footprint; derive them from real visible neighbors. No links through unknown fog or over missing/destroyed hedges. Their geometry must never disguise a solid cell as passable or grow across an open cell.

## Stable and changeable scope

| Stable | May change in this pass |
| --- | --- |
| Approved Seed Keeper, Cook, bird, grazer and jar bytes; accepted Player and all 18 east/kitchen frames | Cold-generation placement of scoped, noncritical Hedge/CharmFlowers runs and their relationship to the service approach |
| Existing inventories, crop stage/moisture, harvest/repair state, finite goods, service contracts and neutral tray state | Validated fresh service-stamp placement/orientation if necessary for the working court; preserve functional owners and safe access |
| Current/saved entity graphs; no live rearrangement for a screenshot | New-world geometry only, tested on at least two known layouts; optional real ground owners only after source/behavior checks |
| Existing glade pond/garden/ruin and cellar functional geometry | Explicit opening-only connected-boundary derivatives or ground-art admission if required by this named scene defect |
| Real HUD, camera/projection, cell proportions and selection semantics | Composition within the exact admitted opening surface sites, never a global Spread repaint |

This intentionally leaves accepted glade/cellar art alone. Their existing material language is the continuity anchor. North works and Nella’s plot are route comparisons, not permission to redesign everything at once. Broader Spread layouts remain outside this bounded pass.

## Screenshot and imagegen brief

Use the two baseline scene captures as separate related design studies; a single collage risks inventing a world that combines different chunks. Show the current native contact and approved Cook only as style/scale references. The forge reader is identity reference only. The parent should preserve exact input paths, prompt and output alongside the selected decisions.

### First study: kitchen composition

> Reinterpret only the world composition in the attached actual Caves of Ooo kitchen screenshot. Keep the flat top-down 16×24-cell pixel style, square native pixels, current camera scale, quiet solid dark-green ground, black unrevealed area, and the real HUD as reference. Preserve the existing Cook, player, oven, cot, chair, pan, input tray, pickup board, shelter masonry and harvested-soil artwork exactly; do not enlarge, redesign or recolor them. Make the inhabited service cluster read as one small working court open to the landscape. Replace the overwhelming rhythm of similar rectangular hedge rooms with a short asymmetric sheltering edge around the court and a few unequal, visibly broken croft remnants farther away. Keep two clear grass approaches to the services and existing working spaces. Retain the wooded counterweight and useful open ground. The only proposed additions/removals are hedge-placement changes to be implemented as real generation later. No new props, actors, doors, fences, roofs, food, signs, lamps, road paint, floor decals, gradients, cast shadows, 3D lighting, glow or decorative UI. Do not reveal anything behind black fog. This is a constrained scene-design concept, not a new sprite sheet or a finished game screenshot.

### Companion study: east meadow/forge setting

> Reinterpret only the vegetation arrangement and open-space hierarchy in the attached actual east-zone screenshot. Preserve the flat native 16×24 visual scale, dark-green grass, existing player and object artwork, black unrevealed regions and HUD reference. Keep the neutral forge, its actual masonry and stoneburr group, the copper pipes/vents, millstone and functional pen in their real groups. Replace three mechanically parallel flower rows with unequal broken drifts: one longer hooked mass, one shorter differently directed mass, and at most one small echo, with tapered tile contours and quiet grass between. Keep flowers away from the forge’s actual two accessible approaches; the service is a small worked opening within a wild meadow. Use the accepted tiny pale-pink flower shape without enlargement or added detail. No roads painted on grass, invented equipment, fire, coals, smoke, blacksmith, roofs, large plant models, new terrain types, fake actions, texture carpet, glow, gradients or shadows. Do not alter collision by painting a fake gap or reveal black fog. This is a layout proposal to map back to real cold-generated owners.

Imagegen cannot establish cell-accurate placement, preserve source bytes reliably, or prove navigation. Reject any attractive change outside the constraints. Extract the accepted broad shapes and spacing; implement those through actual generation and existing sprites. Do not ship the generated pixels as the scene.

## Evaluate before adding detail

1. **Same-view comparison:** capture actual gameplay before/after at 1280×800, same world scale and comparable player position/visibility. Compare unselected world views, not just the examine portraits. Include a second supported seed/layout such as65; do not claim one seed is the biome.
2. **Hierarchy:** without selection outlines, can a reviewer locate the kitchen work group/forge opening and identify how to approach it within a few seconds? Does a dominant large hedge rectangle still get noticed before the service? Are the flower groups visibly different in direction/extent, or merely three wavy stripes?
3. **Truth:** every visible flower/hedge/ground boundary has an actual owner/state; the harvested bed stays empty; the repaired pan stays repaired; neutral trays stay neutral. No scenic object implies a new use, supply or working state. Fog does not disclose hidden neighbors.
4. **Ordinary access:** walk each approach, use the existing service, interact with the bed/crop area as applicable, depart/return, save/Continue. Preserve border ports, collision/LOS, safe withdrawal, service adjacency and any coupled cover/haul/passage constraints. Generation changes require RED→GREEN and counter-checks; appearance alone is not enough.
5. **Journey coherence:** compare the revised two scenes to the glade, cellar, north works and Nella’s plot on the actual route. The palette/material/scale should belong together while the purposes remain different. A copy of the kitchen’s enclosure around every stop fails.
6. **Freeze when better:** accept once the largest arrangement defect is visibly reduced and ordinary action/state checks pass. Do not reopen the approved actors or add a packet of unrelated props. If two concepts only produce prettier clutter or invented roads, return to the real cell greybox and solve the broad masses there.

A successful outcome is a clearly visible improvement to two whole places and their route continuity, with no new gameplay system and no loss of truthful state. It is not yet proof of motion polish, packaged launch, complete regional graphics or physical Steam Deck performance.

## Built-in concept critique and deterministic cell-mask proposal

Reviewed `ArtSource/Live16x24/concepts/working-landscape-v1.png` and `working-landscape-east.png` against their exact prompts and the actual captures. Both are concepts, not native outputs or navigation evidence.

### What to take from the concepts

- **Kitchen v1:** reducing the area occupied by large hedge rectangles makes the service group easier to locate. The asymmetric open area around the court is useful. Retain that hierarchy and the separation between the work group and distant vegetation.
- **Reject v1’s repeated elbows.** It substitutes several similarly sized L-shaped remnants for similarly sized rectangles. The original repetition defect survives in a new symbol. Its extra/rearranged peripheral trees, more textured/soft ground and rewritten HUD/skill names are also outside acceptance. The image cannot authorize moving or duplicating actual owners. Keep source sprites/UI and real fog exactly; do not derive pixels by resizing this concept.
- **East:** the larger quiet gaps and distinct existing object groups remain useful reference. **Reject the flower solution.** It is still three diagonal ribbons made of nearly parallel mini-rows, with a small echo. Rotating/waving the rows does not achieve unequal broad drifts. Do not reproduce these ribbons with trigonometry. Keep the accepted flower sprite; solve the occupied-cell masks.

No further concept iteration is needed merely to obtain prettier pixels. A compact native cell greybox can now test the named mass/spacing problem more precisely, with imagegen used again only if the actual result leaves a clear unresolved design question.

### Scope and deterministic input

Apply the new masks only through the explicitly admitted fresh opening layout, not globally to every Hedgerow/FlowerMeadow and not to restored graphs. Use the existing parcel list, world/seed `Roll`, `mirrored`, and `IsApproach`/`IsLawn`/`IsWater` exclusions. Keep the plan pure: evaluating cells in a different order must give the same answer. Give this pass dedicated hash salts, without consuming the population or service RNG streams.

The exact values below are starting greybox proportions, not new release pins. Geometry/access evidence chooses the final values. Keep all authored source sprites unchanged while evaluating them.

### Hedge mask: unequal perimeter fragments

Use the existing Hedgerow candidate as the outer limit: only a cell that the old parcel boundary/gate/approach logic would admit may survive. This makes the first experiment subtractive. It cannot add a new blocking cell or close an old opening, although opening sightlines and changing encounter cover still need review.

1. Identify the current parcel and local integer coordinates `u=x-p.X`, `v=y-p.Y`. Derive a stable parcel rank from seed/world plus the parcel index/center. Assign **different fragment roles** by ranked order rather than rolling the same template independently five times.
2. Use four shape roles across five parcels, with no more than two jointed/elbow roles. Mirror/rotate the roles deterministically where parcel dimensions permit. Preserve a coherent preferred side; do not randomly drop individual hedge cells as salt-and-pepper damage.
3. Suggested roles, expressed as intervals on actual perimeter edges:

| Role | Kept shape | Main visual difference |
| --- | --- | --- |
| Shelter elbow | One 60–80% edge segment plus an adjoining 30–55% end return; unequal arms | One clear sheltered corner, deliberately the only strongly jointed mass near a service |
| Broken long run | Two unequal intervals on one long edge with a 3–5-cell gap; no return | Horizontal/vertical remnant, not another room corner |
| Opposed scraps | A 25–40% interval on one long edge and a much shorter interval on the opposite edge, offset so endpoints do not align; no connecting side | Two traces of a former parcel with a broad diagonal visual opening |
| Short upright | One 40–65% short-edge run and, only if useful, one isolated 2–3-cell piece elsewhere on the perimeter | A narrow mass, not a smaller copy of the long run |

For short parcels, clamp retained intervals to meaningful whole-cell lengths and keep the intended gap rather than allowing rounding to reconnect them. A spare fifth parcel can reuse the broken-long-run role at distinctly different length/orientation; do not give all five a two-edge join.

The service builder’s successful origin is not known when the terrain plan is made. A rank-derived resident anchor is only a placement preference, not proof of the final Cook cell. Therefore **do not claim the closest computed parcel is the actual shelter** or clear service space after publication. Keep the existing safe-fit proof, inspect the realized result, and if deliberate service framing needs a reserved court, coordinate that reservation explicitly during cold generation. Keep this as the same bounded scene pass, with rollback/fallback on an invalid footprint. Do not move a finished live kitchen to fit the picture.

This mask should reduce dominant geometric mass while retaining visibly different remnants. If the selected seed still reads as five L shapes, fail the visual review even if every individual cell passes.

### Flower mask: two unequal bodies, optional small echo

Do not use three world-width centerlines or three thin rotated ellipses. Use the existing parcels as seed-relative bounding/anchor regions; choose two distinct primary roles through a stable ordering. They need not emit exactly one drift per parcel. Small seed jitter of a few cells, bounded by the real exclusions, prevents fixed screenshot coordinates from becoming the layout.

- **Primary body: broad hooked/lobed crescent.** Make a union of two overlapping ellipses with unequal radii, then subtract one offset elliptical bite. Start with the larger lobe using roughly 35–45% of the selected parcel’s width and 55–70% of its height as radii, the smaller lobe about half its area and shifted toward one end. The bite opens toward a real approach rather than enclosing it. Keep a body several cells thick; this must not collapse to a one-cell ring. Mirror/flip the lobe arrangement by stable seed rank.
- **Secondary body: short compact patch.** In a different selected region, use one compact ellipse or two strongly overlapping small lobes. Keep its long-to-short axis ratio around 1.5–2.2 and its area substantially below the primary body. Its direction/termination differs from the crescent; it is neither a second long crescent nor another ribbon. Existing tree/bush islands and approach masks remain higher-priority exclusions.
- **Optional echo:** a small detached patch near an already-used outer mass, no more than about a fifth of the primary area. It may be absent for a seed. Do not turn every remaining parcel into a third equal band.

Use the existing cell `Roll` for restrained density inside those geometric bodies, not to create their shape. A starting core occupancy near the existing 70% with a lower-density outer fringe is enough. Taper the geometric body first; random holes alone do not stop a thin band from looking like rows. Avoid a perfectly filled carpet, uniformly scattered lone blooms or a fixed one-in-N row cadence. Keep distinct bodies separated by a real quiet gap after exclusions; if clipping fragments a crescent into three long stripes, reject that anchor and use the next deterministic candidate.

A compact evaluation order is:

```
if outside supported fresh-opening scope: use previous plan
if outside bounds or approach/lawn/water: no landscape object
if existing higher-priority tree/bush island: retain that owner
if candidate Hedge: return Hedge only when parcel fragment mask keeps it
if flower role body contains cell and density roll admits it: CharmFlowers
otherwise retain the existing bounded peripheral shelter/bush policy
```

This is a sketch of policy, not code to paste over `ObjectAt`: existing source precedence and supported formation branches must be retained. In particular, never run the hedge fragment filter on a functional pen stamped by another builder, or treat a live returned entity graph as fresh plan data.

### Minimum checks before accepting the mask

- Deterministic regeneration/cell evaluation order; distinct role distribution; no five identical elbows; no reintroduced closed rectangle caused by interval rounding.
- Every retained hedge cell is within the original eligible boundary mask, with approach/gate exclusions intact. Examine reachable cover/passage/haul outcomes because fewer blockers can still alter tactics.
- Flowers are actual placements, and the output visibly has unequal bodies rather than three rotated ribbons. Preserve higher-priority owner groups, readable approach gaps and truthful landscape-context text.
- Real fresh seed64 and65 captures at the existing 1280×800 scale; find the service before the boundary motif without changing sprite brightness/size. Keep the accepted source hashes and HUD untouched.
- Ordinary interaction, departure/return and save/Continue; old saved geometry remains unchanged. If service fitting moves the site in the new layout, compare its realized relative court and access rather than forcing the baseline coordinate.

The mask is successful when the native game improves on the concepts: less repetitive large shapes, a usable inhabited edge, and two distinct wild flower masses, while all current art and mechanics remain honest.
