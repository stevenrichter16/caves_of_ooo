# Boundary source visual review

Current status, 2026-10-24 UTC: the exact opening masonry family is integrated
and accepted through native tests and bounded ordinary keyboard Play. The
2026-10-23 offline source review below is retained as historical evidence; its
contact/context images are not gameplay verification.

The input was the actual opening capture plus image-generated paintover and
parts references listed in the manifest. Their scale, soft shading and raised
RoadStone paths were rejected. Native geometry was authored from explicit
integer cells, using existing restrained masonry colors.

## Historical offline pass 1 — 2026-10-23

The native256×72 render matched every authored pixel. However, dark and lit
walls had an8px center joining6px arms. In the source-layout study the center
expanded once per cell, making straight runs look like beads or fence posts.
That failed the intended visual improvement even though all numerical checks
passed. The first atlas/contact/receipt remain beside the final files.

## Historical offline pass 2 — 2026-10-23

Dark and lit courses now keep a constant8px width, while low walls keep6px.
Each material connects seamlessly to itself; cross-material joints have a
shared opaque6px center and a deliberate width/color change. Mortar is opaque,
so the joints do not suggest open walking gaps. The three-value palette and
flat upper edge avoid painted shadows or a false lighting claim.

Reviewed all16 masks per material and the source-layout illustration. Straight
runs now read continuously, and corners/tees remain continuous. Actual ruin
passages stay empty. The beam and open wicket remain distinct existing props
within their real gaps, and the two quartz veins remain pointed resource art
without a connector. The low isolated crossing remains a capped obstacle.
The subdued dark row is intentionally quieter than the low cap; its readability
against the lit game scene still requires the native Play gate.

Final offline checks:48 frames; exact256×72; zero pixel mismatches; binary alpha;
three colors/frame; connected opaque mass for each mask; matching cardinal
alpha and color ports; exact directly rendered4× contact. Source receipt and
manifest record the final hash. The4× contact replaces the initial8× suggestion
without changing native geometry or sampling.

## Historical runtime-trial handoff — 2026-10-23

At this handoff, root reviewed the final contact and context and approved a runtime trial only:
continuous courses read as connected barriers, with quartz, beam and gaps
remaining separate. This was not final art or gameplay acceptance and did not
authorize import before G1 was ready. At that point no world renderer selected the atlas.
Runtime mask and ownership tests, actual
destruction/FOV repaint, material/camera comparison in normal gameplay,
keyboard/Deck targeting, collision and frame-cost checks remained open. The
source-layout study was deliberately not evidence for those gates.


## Runtime visual review — 2026-10-24 UTC

The later runtime integration passed 60 focused native cases and a 552-case
bounded native regression, all GREEN with zero skipped; the 552 includes the
60. Receipts are `g2-boundaries-green.xml` and `g2-boundaries-regression.xml` in
`Docs/Verification/Live16x24/`. These are separate from the offline source
validator and do not turn its illustrations into gameplay evidence.

Reviewed actual 1280×800 normal-launch captures from seed 64, with isolated
saves and no presentation override: `g2-normal-start`, `g2-wall-examine`,
`g2-wall-destroyed`, `g2-ruin-passage`, `g2-western-ruin` and
`g2-low-crossing-confirmed` (PNG plus JSON). The parent performed the ordinary
keyboard actions; this independent review inspected the resulting pixels and
receipts without altering the game.

1. **Continuous barriers and real passages:** the matched G1/G2 opening shows
   the northern low wall and longer vertical runs as connected masonry rather
   than detached posts. At the western ruin, visible dark courses remain
   distinguishable from the floor and the y16 gap reads as a clear passage.
   The intentionally dim remembered caps do not claim hidden live connections.
2. **Distinct owners and state change:** the wicket remains a separate wooden
   interruption; the beam remains brown timber in its own lower opening;
   visible quartz retains its pointed resource silhouette. After ordinary
   destruction of (44,6), factory Rubble replaces the wall and surviving
   (43,6)/(45,6) caps leave an unmistakable break with no stale connecting arm.
3. **Identity and obstacle readability:** examine shows the matching isolated
   stone cap and readable Stone/39-of-40 structure. The low crossing remains
   a small capped obstacle; actual input at player (28,16) strikes the worn
   crossing at (28,17) instead of walking through it. This confirms collision,
   while the artwork remains intentionally lower than the ruin courses.

No significant visual defect was found in this bounded review; no extra art
revision was justified. The final atlas hash remains
`7ad89c28fa6d9dcc3be83ed9935aa257f5d10b1d3ba41100f0dc18051a89dff2`.
All approved actors and earlier source atlases remain unchanged.

## Current limits

**Verified:** this family's native routing/ownership/repaint assertions and the
recorded keyboard launch, movement, examine, wall destruction and low-crossing
checks, with actual captured readability.

**Not verified by this pass:** Vault targeting/landing (the player was not
granted Vault), physical Steam Deck controls or performance, every lighting
condition or region, beam hauling/wicket state changes through ordinary input,
and final packaged delivery. Their source/native counters are not substitutes
for unperformed live actions. The G2 scene and graphics release remain
incomplete; acceptance here applies only to the exact opening masonry family.
