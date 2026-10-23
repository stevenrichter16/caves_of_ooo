# Opening reinterpretation — capture-first prompt

Tool: built-in imagegen. Executed2026-10-23 UTC. Image1: `Docs/Verification/Live16x24/greybox-game-20261023-043026.png`; image2: `Docs/Verification/Clearing16x24/clearing-game-final.png`. Output: `opening-paintover-v1.png`. Reference only; see REVIEW.md for rejected generated changes.

Use case: style-transfer / stylized-concept.
Asset type: art-direction paintover of actual playable RPG opening, reference only (not a production backdrop).

Primary request: transform the rough silhouettes of the first supplied Unity gameplay capture into a beautiful, restrained flat 2D pixel-art opening for Caves of Ooo. Preserve the gameplay composition, cells, traversable negative space, pond footprint, field rows, low walls, ruined passages, actors and visible fog boundary. Treat this as a 16×24-cell game with square pixels, not an isometric or perspective scene.

Inputs: first image is the actual gameplay composition and edit target. Second image is the existing approved native-art study, style reference ONLY: limited olive, deep green, straw ochre and cream palette; clean silhouettes; deliberate clusters of pixels. Do not import its characters or layout into the target. Individual approved character references, if supplied, likewise establish pixel vocabulary only.

Art direction: make the three real anchors easy to read. The north pond has dark still teal water, sparse small pale ripple marks and grouped straw reeds. The eastern cultivated area uses dark earth, readable furrows and ripe heads contrasted with spent stalks; its small hearth and practical water/repair service must be distinct. The southwestern ruin uses broken charcoal-green masonry, occasional pale stone faces and a restrained quartz seam. Keep openings, doorways and actor silhouettes unobstructed. The player gets the clearest warm accent. Warden, villagers, dog and marlbacks should have original distinct silhouettes matching their actual occupants, not extra story characters. Marlbacks are short-faced, hunched burrowers with overlapping shale back plates and grasping foreclaws, not ordinary humanoids. Quiet near-solid dark green ground connects these anchors; avoid sprinkling texture into every cell.

Constraints: retain top-down flat projection, rectangular cell proportions, native-pixel aesthetic, image framing, HUD positions and existing text. No 3D extrusion, lighting gradients, bloom, depth of field, high-resolution painterly texture, fantasy castles, new buildings, extra service props or invented passages. Do not copy Qud assets. Do not add markers, labels or titles. Preserve fog; do not reveal hidden terrain. The useful result should be decomposable into terrain tiles, wall variants, individual props and actors, never an opaque scene painting.

Additional owner constraints: if an anchor is outside the visible capture, do not force it into frame. Keep the low crossing lower than standing walls, and the movable beam inside its one-cell footprint. Make the shallow finite basin distinct from the cracked well, and grain heads distinct from vessel-shaped Drawgourd.

HUD/text is protected reference content: do not redesign it; any generated lettering change will be rejected. Evaluate only the world art.

Quality gate: inspect whether the resulting silhouette/material choices can survive native 16×24 reconstruction. Record changes the generator made to topology or fog; reject those changes for integration. Use the parts mapping to adapt only designs for real owners and supported states.
