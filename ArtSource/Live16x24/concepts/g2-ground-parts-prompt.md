# G2 ground parts extraction — exact prompt

Built-in imagegen. Input: `g2-ground-paintover-v1.png`, motif reference only.
Output is a concept parts reference, not an imported native atlas.

Use case: precise-object-edit
Asset type: extracted ground-material references, NOT production tiles or gameplay evidence.
Input is our concept paintover. Extract and simplify only three ground treatments into a clearly separated 3-column by 2-row reference sheet on transparent background, without labels:
Top row: quiet teal shallow-water interior with one short ripple; a straight water-side muddy bank edge; an inside corner bank with an open teal interior.
Bottom row: flat worn stone and earth path; dry dark brown soil with two short shallow furrow marks; the same furrows in darker moist soil.
Each is a simple flat top-down rectangular16x24-equivalent tile with square pixels, hard edges, three flat colors maximum, deliberately very sparse detail. A bank is only a narrow muted olive line inside the water-side edge, not a raised wall or stone parapet. Path and soil are flat low-contrast surfaces, no rocks standing up, no shadows, no height shading. Ground must not compete with the approved yellow player or crops.
Keep the reference's restrained deep teal, dark olive-green and brown palette. Reject its gradients and soft shading. No characters, vegetation, yellow stalks, tools, UI, text, frames, new props or scenic backdrop. Preserve transparent gutters between the six isolated material references. These motifs will be rebuilt directly on the native integer pixel grid, never downsampled.
