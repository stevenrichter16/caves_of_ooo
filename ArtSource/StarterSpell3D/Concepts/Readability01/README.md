# Starter spell readability — five imagegen mockups

**Status: complete.** Five generated mockups are present and visually reviewed. Each PNG is **1672×941**, generated from the 1920×1080 reference. The files were copied without pixel edits; dimensions, hashes and original output paths are recorded in [manifest.json](manifest.json).

This package explores five still-image VFX directions for the existing zoomed-out Caves of Ooo view. It is a concept pass only. The game, camera, Unity assets and Blender sources remain unchanged.

The reference is the actual 1920×1080 [town GameView](../../../../Docs/Verification/StarterSpell3D/Integration/NativeMedia/SSN-0d7a74963b314b6f9c9270a00d13846d/town-full-gameview.png). Every prompt requests the same camera angle, zoom, framing, town layout, actor positions and sizes, HUD, hotbar, fog boundaries and background exposure. The complete viewport remains visible in the results. Visual review finds the town scale and overall framing consistent with the reference, with much stronger connected effect volumes and clearer colors at the full-view size. These are AI-edited concepts, not pixel-identical captures: recipient placement varies, particularly in Fire, Water and Ice, and actor/HUD details are not runtime, targeting or hitbox specifications.

## Visual direction

Retain the game's carved, faceted, stylized 3D forms. Make each spell conspicuous at the unchanged camera scale through a roughly 150–200-pixel primary shape, bright milky cores, saturated darker edges and local soft glow.

The user's latest refinement is **less space between particles**. The prompts therefore request dense, overlapping particles and short streaks that form a connected effect volume: a bright center, closely layered motes and a gradually thinner outer edge. The 100–180-particle direction describes the desired visual density, not a verified count or runtime budget. Keep deliberate openings around faces instead of making the entire effect sparse.

Airborne ribbons, spray and sparks may overhang cell edges decoratively. Ground contact stays attached to the actual caster, path and recipient, without suggesting a larger damaging area or changing town objects.

| Mockup | Direction |
| --- | --- |
| [01-ember-spit.png](01-ember-spit.png) | **Ember Spit — molten charcoal:** an ivory-yellow core, amber/coral carved flame ribbons, a thick tapering trail and an upward impact crown packed with sparks, flakes and edged coal chips. |
| [02-jet-blast.png](02-jet-blast.png) | **Jet Blast — pearl torrent:** a sculptural turquoise waterfall fold, milky mint crests, deep teal sides and a dense mantle of overlapping pearl droplets and water shards. |
| [03-ground-surge.png](03-ground-surge.png) | **Ground Surge — golden root lightning:** four connected ground stitches with ivory-gold knots, ochre ridges, plum-violet sides and tightly layered raised arcs and mineral sparks. |
| [04-rime-grip.png](04-rime-grip.png) | **Rime Grip — luminous frost bloom:** blunt pale-ice clamps, glacier-blue bevels, indigo seams and a bright open frost crown with dense faceted chips behind the visible recipient. |
| [05-calm.png](05-calm.png) | **Calm — flowering quiet:** a broad open ivory/violet loop with three gaps, luminous curved ribbons and closely packed upward spirals of leaf-shaped pearl and lavender motes. |

The fire-family palette and carved flame language can guide Flaming Hands later; its adjacent-cell palm fans remain a distinct shape from Ember Spit's projectile. Rain has no separate image in this five-mockup set. This package does not claim seven-spell image coverage.

## Reproduction and review

The complete generation requests are preserved in [prompts.json](prompts.json). Its `reference` field contains the absolute input-image path, and each `mockups` entry provides the output `id`, title and full prompt, including the tighter particle-spacing refinement.

For each entry, use the built-in `image_gen` tool with that entry's `prompt` and `referenced_image_paths: [reference]`. Do not use a recent-conversation-image count when the reference file is available. Preserve the resulting image as `<id>.png`, then record the actual output and provenance. Image generation is stochastic; the saved requests support repeating the direction, not reproducing identical pixels.

All five were inspected at full framing for apparent camera scale, effect density and legibility. The bright connected shapes read without a detail crop, and the caster and recipient faces generally remain visible. Decorative particle spread and generated character positions should be interpreted as art direction, not changes to game mechanics. Particle counts and animation performance were not measured from these stills. [generation-sources.json](generation-sources.json) preserves the generator output locations; [prompts.json](prompts.json) preserves the exact requests.
